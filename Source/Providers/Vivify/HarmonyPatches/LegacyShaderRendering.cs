#if BEAT_SABER_1453
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using Vivify.Managers;
using Vivify.PostProcessing;

namespace Vivify.HarmonyPatches;

internal sealed class LegacyShaderRendering : IAffinity, IDisposable
{
    private readonly LegacyLightingPass? _lightingPass;
    private readonly LegacyGrabTexturePass? _grabPass;
    private readonly LegacyExtraPassRenderPass? _extraPass;
    private readonly AreaLitLightMeshPass? _lightMeshPass;
    private readonly AreaLitProjectorPass? _projectorPass;
    private readonly SiraLog _log;
    private readonly bool _hasNamedForwardGrabs;

    private bool _reportedFallback;
    private volatile bool _grabPassRequested;
    private bool _reportedGrabPass;

    [UsedImplicitly]
    private LegacyShaderRendering(AssetBundleManager assetBundleManager, SiraLog log)
    {
        _log = log;
        if (!Legato.Rendering.LegacyShaderSupport.IsUniversalPipeline())
        {
            return;
        }

        LegacyExtraPasses.ClearCache();
        if (LegacyGrabPasses.Count > 0)
        {
            log.Info($"Emulating {LegacyGrabPasses.Count} GrabPass pass(es) from bundled shaders.");
        }

        if (LegacyGrabPasses.NativeGrabCount > 0)
        {
            log.Info($"Using native renderer lists for {LegacyGrabPasses.NativeGrabCount} legacy GrabPass shader(s).");
        }

        // Unity logs native GrabPass failures from a render job thread.
        Application.logMessageReceivedThreaded += OnLogMessage;

        HashSet<Shader> shaders = [];
        HashSet<string> namedForwardGrabs = new(StringComparer.Ordinal);
        HashSet<RenderTexture> cameraTargets = [];
        HashSet<RenderTexture> lightMeshTargets = [];
        Material? lightMeshClear = null;
        Material? lightMesh = null;
        List<Material> materials = [];
        foreach (UnityEngine.Object asset in assetBundleManager.Assets)
        {
            if (asset is Material material)
            {
                AddMaterial(material);
            }
            else if (asset is GameObject prefab)
            {
                foreach (Camera camera in prefab.GetComponentsInChildren<Camera>(true))
                {
                    if (camera.targetTexture != null)
                    {
                        cameraTargets.Add(camera.targetTexture);
                    }
                }

                foreach (Projector projector in prefab.GetComponentsInChildren<Projector>(true))
                {
                    if (AreaLitProjector.IsSupported(projector) && !projector.TryGetComponent(out AreaLitProjector _))
                    {
                        projector.gameObject.AddComponent<AreaLitProjector>();
                        _projectorPass ??= new AreaLitProjectorPass();
                    }
                }

                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.GetSharedMaterials(materials);
                    bool extraPasses = false;
                    foreach (Material sharedMaterial in materials)
                    {
                        if (sharedMaterial != null)
                        {
                            AddMaterial(sharedMaterial);
                            extraPasses |= LegacyExtraPasses.HasExtraPasses(sharedMaterial.shader);
                        }
                    }

                    if (extraPasses && !renderer.TryGetComponent(out LegacyExtraPasses _))
                    {
                        if (renderer is ParticleSystemRenderer { enableGPUInstancing: true, renderMode: ParticleSystemRenderMode.Mesh } &&
                            materials.Count == 1 && materials[0] != null && LegacyExtraPasses.NeedsParticleLayer(materials[0].shader))
                        {
                            LegacyExtraPasses.PrepareParticleLayers();
                        }

                        renderer.gameObject.AddComponent<LegacyExtraPasses>();
                        _extraPass ??= new LegacyExtraPassRenderPass();
                    }
                }
            }
        }

        int repairedTargets = 0;
        foreach (RenderTexture target in cameraTargets)
        {
            if (target.depthStencilFormat != GraphicsFormat.None)
            {
                continue;
            }

            target.Release();
            target.depthStencilFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            repairedTargets++;
        }

        if (repairedTargets > 0)
        {
            log.Info($"Added depth attachments to {repairedTargets} bundled camera target(s).");
        }

        lightMeshTargets.IntersectWith(cameraTargets);
        if (lightMeshClear != null && lightMesh != null && lightMeshTargets.Count > 0)
        {
            _lightMeshPass = new AreaLitLightMeshPass(lightMeshClear, lightMesh, lightMeshTargets);
            log.Info("Enabled AreaLit vertex lighting passes for bundled light-mesh cameras.");
        }

        if (_projectorPass != null)
        {
            log.Info("Enabled AreaLit projector rendering for bundled projectors.");
        }

        if (_extraPass != null)
        {
            log.Info("Enabled additional untagged shader passes for bundled renderers.");
        }

        _grabPass = new LegacyGrabTexturePass(namedForwardGrabs);
        _hasNamedForwardGrabs = namedForwardGrabs.Count > 0;
        if (_hasNamedForwardGrabs)
        {
            log.Info($"Providing {namedForwardGrabs.Count} named GrabPass texture(s) for transparent legacy ForwardBase shaders.");
        }

        foreach (Shader shader in shaders)
        {
            if (shader == null || !Legato.Rendering.LegacyShaderSupport.HasForwardBase(shader))
            {
                continue;
            }

            _lightingPass = new LegacyLightingPass();
            log.Info("Enabled legacy ForwardBase rendering for bundled shaders.");
            return;
        }

        void AddMaterial(Material material)
        {
            Shader shader = material.shader;
            if (shader == null)
            {
                return;
            }

            shaders.Add(shader);
            if (shader.name == "AreaLit/LightMeshClear")
            {
                lightMeshClear ??= material;
            }
            else if (shader.name == "AreaLit/LightMesh")
            {
                lightMesh ??= material;
            }

            if (shader.name.StartsWith("AreaLit/", StringComparison.Ordinal) && material.HasProperty(AreaLitLightMeshPass.LightMeshProperty) &&
                material.GetTexture(AreaLitLightMeshPass.LightMeshProperty) is RenderTexture target)
            {
                lightMeshTargets.Add(target);
            }

            if (material.renderQueue > 2500)
            {
                LegacyGrabPasses.CollectForwardGrabTextures(shader, namedForwardGrabs);
            }
        }
    }

    public void Dispose()
    {
        Application.logMessageReceivedThreaded -= OnLogMessage;
        _lightingPass?.Dispose();
    }

    private void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!_grabPassRequested && type == LogType.Error && condition.StartsWith("ShaderLab::GrabPasses", StringComparison.Ordinal))
        {
            _grabPassRequested = true;
        }
    }

    [AffinityPrefix]
    [AffinityPatch(
        typeof(UniversalRenderPipeline),
        "RenderSingleCamera",
        AffinityMethodType.Normal,
        new[] { typeof(ScriptableRenderContext), typeof(UniversalCameraData) })]
    private void CullProjectors(ScriptableRenderContext context, UniversalCameraData cameraData)
    {
        _projectorPass?.Cull(context, cameraData.camera, _lightMeshPass?.Matches(cameraData.camera) == true);
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(ScriptableRenderer), "AddRenderPasses")]
    private void AddLightingPass(ScriptableRenderer __instance)
    {
        if (_lightMeshPass != null)
        {
            __instance.EnqueuePass(_lightMeshPass);
        }

        if (_projectorPass?.HasReceivers == true)
        {
            __instance.EnqueuePass(_projectorPass);
        }

        if (_lightingPass != null)
        {
            __instance.EnqueuePass(_lightingPass);
        }

        if (_extraPass != null)
        {
            __instance.EnqueuePass(_extraPass);
            __instance.EnqueuePass(_extraPass.TransparentPass);
        }

        if (_grabPass != null && (_grabPassRequested || _hasNamedForwardGrabs))
        {
            __instance.EnqueuePass(_grabPass);
            if (_grabPassRequested && !_reportedGrabPass)
            {
                _reportedGrabPass = true;
                _log.Info("Bundled shaders use GrabPass; providing the camera opaque texture as _GrabTexture.");
            }
        }
    }

    [AffinityPostfix]
    [AffinityPatch(
        typeof(RenderingUtils),
        nameof(RenderingUtils.CreateDrawingSettings),
        AffinityMethodType.Normal,
        new[] { typeof(List<ShaderTagId>), typeof(UniversalRenderingData), typeof(UniversalCameraData), typeof(UniversalLightData), typeof(SortingCriteria) })]
    private void AddLegacyFallback(List<ShaderTagId> shaderTagIdList, UniversalCameraData cameraData, ref DrawingSettings __result)
    {
        if (_lightMeshPass?.Matches(cameraData.camera) == true && shaderTagIdList.Contains(AreaLitLightMeshPass.UniversalForward))
        {
            // The light-mesh target stores data from the Vertex passes. ForwardBase draws would overwrite it.
            __result = new DrawingSettings(AreaLitLightMeshPass.SkipForward, __result.sortingSettings);
            return;
        }

        if (_lightingPass == null ||
            !Legato.Rendering.LegacyShaderSupport.AppendForwardBase(shaderTagIdList, ref __result))
        {
            return;
        }

        if (!_reportedFallback)
        {
            _reportedFallback = true;
            _log.Debug($"Added legacy ForwardBase fallback for [{cameraData.camera.name}].");
        }
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(DrawObjectsPass), "CanDisableZWrite")]
    private void KeepLegacyDepthWrites(ref bool __result)
    {
        if (_lightingPass != null)
        {
            // Legacy materials can lack the depth prepass required by depth priming's Equal test.
            __result = false;
        }
    }
}
#endif
