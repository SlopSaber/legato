#if LEGATO_EARLY_STANDARD_MAPS || LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
#define LEGATO_BUILTIN_GPU_TIMING
#endif

#if LEGATO_NATIVE_GPU_TIMING
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Legato.Performance;

internal sealed class NativeGpuTiming : IDisposable
{
    private readonly Action<string> _debug;
    private readonly Action<string> _warn;

    internal NativeGpuTiming(Action<string> debug, Action<string> warn)
    {
        _debug = debug;
        _warn = warn;
    }

    private const string Library = "NalulunaFps.NativeGpuTiming";
    private const int BeginFrame = 1, BeginScope = 2, EndScope = 3, EndFrame = 4, Stop = 5;
    private static NativeGpuTiming _active;
#if !LEGATO_BUILTIN_GPU_TIMING
    private static readonly MethodInfo SubmitMethod = AccessTools.Method(typeof(ScriptableRenderContext), "Submit");
    private static readonly MethodInfo EndScopeMethod = AccessTools.Method(typeof(NativeGpuTiming), nameof(BeforeSubmit));
#else
    private int _cameraDepth;
#endif
    private MonoBehaviour _owner;
    private Harmony _harmony;
    private Coroutine _finishCoroutine;
    private Texture2D _probe;
    private CommandBuffer _markers;
    private IntPtr _eventCallback;
#if !LEGATO_BUILTIN_GPU_TIMING
    private ScriptableRenderContext _scopeContext;
#endif
    private bool _started, _failed, _nativeLoaded, _frameOpen, _scopeOpen;
    private int _lastFrame = -1, _lastStatus = -1;
    private bool _reportedSample;
    private float _nextRecovery, _nextDiagnostics;
    private ulong _lastSkipped, _lastInvalid;

    public float Read(MonoBehaviour owner, float realtime)
    {
        if (_failed) return float.NaN;
        if (!_started) Start(owner);
        if (!_started || _failed) return float.NaN;
        try
        {
            int available = GetTimingSample(out double milliseconds, out ulong sequence, out long timestamp,
                out uint scopes, out ulong skipped, out ulong invalid, out int status, out int result);
            if (status != _lastStatus)
            {
                _lastStatus = status;
                string detail = status == 0 ? "waiting for render-thread initialization" : status == 1 ? "collecting GPU rendering scopes" :
                    status == 2 ? "D3D11 device removed; rebuilding queries" : "D3D11 query failure; rebuilding queries";
                _debug($"Native GPU timing: {detail}; HRESULT=0x{result:X8}");
            }
            if (status >= 2 && realtime >= _nextRecovery)
            {
                _nextRecovery = realtime + 5f;
                CreateProbe();
            }
            if ((skipped != _lastSkipped || invalid != _lastInvalid) && realtime >= _nextDiagnostics)
            {
                _nextDiagnostics = realtime + 5f;
                _lastSkipped = skipped;
                _lastInvalid = invalid;
                _debug($"Native GPU timing omitted frames: ring full={skipped}, invalid/disjoint/empty={invalid}");
            }
            long age = GpuTimingClock.Timestamp - timestamp;
            if (available == 0 || status != 1 || sequence == 0 || scopes == 0 ||
                double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds < 0 ||
                age < 0 || age > GpuTimingClock.Frequency / 2)
                return float.NaN;
            if (!_reportedSample)
            {
                _reportedSample = true;
                _debug($"GPU source: native D3D11 rendering scopes; first={milliseconds:F3} ms, scopes={scopes}, sequence={sequence}. Elapsed rendering time, not an idle-excluded busy counter.");
            }
            return (float)milliseconds;
        }
        catch (Exception exception)
        {
            Fail(exception);
            return float.NaN;
        }
    }

    private void Start(MonoBehaviour owner)
    {
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11)
        {
            _failed = true;
            _debug("Native GPU timing requires Direct3D11; current backend=" + SystemInfo.graphicsDeviceType);
            return;
        }
        try
        {
            if (GetTimingApiVersion() != 1) throw new InvalidOperationException("Native GPU timing library version mismatch");
            _nativeLoaded = true;
            _eventCallback = GetRenderEventFunc();
            if (_eventCallback == IntPtr.Zero) throw new InvalidOperationException("Native render callback unavailable");
            _owner = owner;
            CreateProbe();
#if LEGATO_BUILTIN_GPU_TIMING
            Camera.onPreRender += BeforeLegacyCamera;
            Camera.onPostRender += AfterLegacyCamera;
#else
            _markers = new CommandBuffer { name = "NalulunaFps GPU timestamp" };
            Type pipeline = AccessTools.TypeByName("UnityEngine.Rendering.Universal.UniversalRenderPipeline");
            if (pipeline == null) throw new InvalidOperationException("URP rendering pipeline type unavailable");
            MethodInfo graph = AccessTools.GetDeclaredMethods(pipeline).Single(method => method.Name == "RecordAndExecuteRenderGraph");
            MethodInfo camera = AccessTools.GetDeclaredMethods(pipeline).Single(method => method.Name == "RenderSingleCamera" &&
                method.GetParameters().Length == 2 && method.GetParameters()[1].ParameterType.Name == "UniversalCameraData");
            _harmony = new Harmony(owner.GetType().Assembly.GetName().Name + ".NativeD3D11Timing");
            _harmony.Patch(graph, prefix: new HarmonyMethod(typeof(NativeGpuTiming), nameof(BeforeRenderGraph)));
            // Submit's small managed wrapper can already be inlined. Insert
            // our end marker at its actual call site in URP instead.
            _harmony.Patch(camera, transpiler: new HarmonyMethod(typeof(NativeGpuTiming), nameof(InsertEndScope)));
#endif
            _active = this;
            _started = true;
#if !LEGATO_BUILTIN_GPU_TIMING
            RenderPipelineManager.beginContextRendering += OnBeginContext;
#endif
            _finishCoroutine = owner.StartCoroutine(FinishFrames());
#if LEGATO_BUILTIN_GPU_TIMING
            _debug("Native D3D11 GPU timing attached to built-in camera rendering; nested cameras share their outer scope, asynchronous query reads.");
#else
            _debug("Native D3D11 GPU timing attached to URP RenderGraph and camera submission; eight pending frames, 32 camera scopes per frame, asynchronous query reads.");
#endif
        }
        catch (Exception exception)
        {
            Fail(exception);
            Dispose();
        }
    }

    private void CreateProbe()
    {
        if (_probe != null) UnityEngine.Object.Destroy(_probe);
        _probe = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
        {
            name = "NalulunaFps device probe",
            hideFlags = HideFlags.HideAndDontSave
        };
        _probe.Apply(false, true);
        IntPtr resource = _probe.GetNativeTexturePtr();
        if (resource == IntPtr.Zero) throw new InvalidOperationException("Unity D3D11 resource pointer unavailable");
        SetSourceTexture(resource);
    }

#if LEGATO_BUILTIN_GPU_TIMING
    private void BeforeLegacyCamera(Camera camera)
    {
        if (!_started || _failed) return;
        BeginRenderingFrame();
        if (_failed || _cameraDepth++ != 0) return;
        try
        {
            GL.IssuePluginEvent(_eventCallback, BeginScope);
            _scopeOpen = true;
        }
        catch (Exception exception) { Fail(exception); }
    }

    private void AfterLegacyCamera(Camera camera)
    {
        if (!_started || _failed || !_scopeOpen || _cameraDepth <= 0 || --_cameraDepth != 0) return;
        try
        {
            GL.IssuePluginEvent(_eventCallback, EndScope);
            _scopeOpen = false;
        }
        catch (Exception exception) { Fail(exception); }
    }
#else
    private void OnBeginContext(ScriptableRenderContext context, List<Camera> cameras) => BeginRenderingFrame();
#endif

    private void BeginRenderingFrame()
    {
        if (!_started || _failed || _lastFrame == Time.frameCount) return;
        try
        {
            _lastFrame = Time.frameCount;
            _frameOpen = true;
#if LEGATO_BUILTIN_GPU_TIMING
            _cameraDepth = 0;
#endif
            GL.IssuePluginEvent(_eventCallback, BeginFrame);
        }
        catch (Exception exception) { Fail(exception); }
    }

    private IEnumerator FinishFrames()
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (_started)
        {
            yield return endOfFrame;
            if (!_frameOpen || _failed) continue;
            _frameOpen = false;
            _scopeOpen = false;
            try { GL.IssuePluginEvent(_eventCallback, EndFrame); }
            catch (Exception exception) { Fail(exception); }
        }
    }

#if !LEGATO_BUILTIN_GPU_TIMING
    private static void BeforeRenderGraph(ScriptableRenderContext __1) => _active?.StartScope(__1);

    private void StartScope(ScriptableRenderContext context)
    {
        if (!_started || _failed || !_frameOpen || _scopeOpen) return;
        try
        {
            _scopeContext = context;
            IssueMarker(context, BeginScope);
            _scopeOpen = true;
        }
        catch (Exception exception) { Fail(exception); }
    }

    private static void BeforeSubmit(ref ScriptableRenderContext context)
    {
        NativeGpuTiming active = _active;
        if (active == null || !active._started || active._failed || !active._scopeOpen || !active._scopeContext.Equals(context)) return;
        try
        {
            active.IssueMarker(context, EndScope);
            active._scopeOpen = false;
        }
        catch (Exception exception) { active.Fail(exception); }
    }

    private void IssueMarker(ScriptableRenderContext context, int marker)
    {
        _markers.Clear();
        _markers.IssuePluginEvent(_eventCallback, marker);
        context.ExecuteCommandBuffer(_markers);
    }

    private static IEnumerable<CodeInstruction> InsertEndScope(IEnumerable<CodeInstruction> instructions)
    {
        int inserted = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(SubmitMethod))
            {
                // Stack holds ScriptableRenderContext& for Submit. Duplicate
                // it for our hook, preserving the original call's operand.
                var duplicate = new CodeInstruction(OpCodes.Dup);
                duplicate.labels.AddRange(instruction.labels);
                duplicate.blocks.AddRange(instruction.blocks);
                instruction.labels.Clear();
                instruction.blocks.Clear();
                yield return duplicate;
                yield return new CodeInstruction(OpCodes.Call, EndScopeMethod);
                ++inserted;
            }
            yield return instruction;
        }
        if (inserted != 1) throw new InvalidOperationException("Expected one URP camera submission; found " + inserted);
    }
#endif

    private void Fail(Exception exception)
    {
        if (_failed) return;
        _failed = true;
        _warn("Native GPU timing unavailable: " + exception.Message);
        // Stop query collection even if a hook fails in the middle of a
        // camera render. Later queued scope events safely become no-ops.
        if (_nativeLoaded && _eventCallback != IntPtr.Zero)
        {
            try
            {
                SetSourceTexture(IntPtr.Zero);
                GL.IssuePluginEvent(_eventCallback, Stop);
            }
            catch (Exception cleanup) { _debug("Native GPU timing cleanup: " + cleanup.Message); }
        }
    }

    public void Dispose()
    {
        _started = false;
#if LEGATO_BUILTIN_GPU_TIMING
        Camera.onPreRender -= BeforeLegacyCamera;
        Camera.onPostRender -= AfterLegacyCamera;
        _cameraDepth = 0;
#else
        RenderPipelineManager.beginContextRendering -= OnBeginContext;
#endif
        if (ReferenceEquals(_active, this)) _active = null;
        if (_finishCoroutine != null && _owner != null) _owner.StopCoroutine(_finishCoroutine);
        _finishCoroutine = null;
        if (_harmony != null)
        {
            try { _harmony.UnpatchSelf(); }
            catch (Exception exception) { _warn("Native GPU timing hook removal: " + exception.Message); }
            _harmony = null;
        }
        if (_nativeLoaded)
        {
            try
            {
                SetSourceTexture(IntPtr.Zero);
                GL.IssuePluginEvent(_eventCallback, Stop);
            }
            catch (Exception exception) { _debug("Native GPU timing shutdown: " + exception.Message); }
        }
        _nativeLoaded = false;
        _markers?.Dispose();
        _markers = null;
        if (_probe != null) UnityEngine.Object.Destroy(_probe);
        _probe = null;
        _owner = null;
        _frameOpen = false;
        _scopeOpen = false;
        _lastFrame = -1;
        _lastStatus = -1;
        _reportedSample = false;
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetTimingApiVersion();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetRenderEventFunc();
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetSourceTexture(IntPtr resource);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetTimingSample(out double milliseconds, out ulong sequence, out long timestamp,
        out uint scopes, out ulong skippedFrames, out ulong invalidFrames, out int status, out int result);
}
#endif
