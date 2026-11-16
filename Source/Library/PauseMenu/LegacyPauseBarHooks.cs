#if BEAT_SABER_1_29_0
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Legato.Presentation
{
    internal static class LegacyPauseBarHooks
    {
        private static readonly Harmony Patcher = new("Legato.PauseBar.NativeSetup");
        private static Action<LevelBar, NativePauseLevel>? Completed;

        internal static IDisposable Subscribe(Action<LevelBar, NativePauseLevel> callback) => new Subscription(callback);

        private static void Remember(LevelBar __instance, IPreviewBeatmapLevel previewBeatmapLevel) =>
            PauseMenuLayout.Remember(__instance, previewBeatmapLevel);

        private static void Finish(LevelBar bar, IPreviewBeatmapLevel level)
        {
            if (bar != null && ReferenceEquals(PauseMenuLayout.CurrentLevel(bar), level))
            {
                Completed?.Invoke(bar, level);
            }
        }

        private static IEnumerable<CodeInstruction> AfterSuccessfulSetup(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            Type stateMachine = __originalMethod.DeclaringType!;
            FieldInfo bar = AccessTools.Field(stateMachine, "<>4__this");
            FieldInfo level = AccessTools.Field(stateMachine, "previewBeatmapLevel");
            MethodInfo finish = AccessTools.Method(typeof(LegacyPauseBarHooks), nameof(Finish));
            bool inserted = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.operand is MethodInfo method && method.Name == "SetResult" &&
                    method.DeclaringType == typeof(System.Runtime.CompilerServices.AsyncVoidMethodBuilder))
                {
                    CodeInstruction load = new(OpCodes.Ldarg_0);
                    load.labels.AddRange(instruction.labels);
                    instruction.labels.Clear();
                    yield return load;
                    yield return new CodeInstruction(OpCodes.Ldfld, bar);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, level);
                    yield return new CodeInstruction(OpCodes.Call, finish);
                    inserted = true;
                }
                yield return instruction;
            }
            if (!inserted) throw new InvalidOperationException("Native pause metadata completion was not found.");
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action<LevelBar, NativePauseLevel> _callback;
            private bool _disposed;

            internal Subscription(Action<LevelBar, NativePauseLevel> callback)
            {
                _callback = callback;
                if (Completed == null)
                {
                    try
                    {
                        MethodInfo setup = PauseMenuLayout.SetupMethod;
                        Patcher.Patch(setup, prefix: new HarmonyMethod(typeof(LegacyPauseBarHooks), nameof(Remember)));
                        Patcher.Patch(AccessTools.AsyncMoveNext(setup), transpiler: new HarmonyMethod(typeof(LegacyPauseBarHooks), nameof(AfterSuccessfulSetup)));
                    }
                    catch
                    {
                        Patcher.UnpatchSelf();
                        throw;
                    }
                }
                Completed += callback;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Completed -= _callback;
                if (Completed == null) Patcher.UnpatchSelf();
            }
        }
    }
}
#endif
