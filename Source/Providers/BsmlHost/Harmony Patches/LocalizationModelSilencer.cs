#if !LEGATO_EARLY_VR_UI
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    /// <summary>
    /// Skips native warnings about missing localization keys.
    /// </summary>
    [HarmonyPatch(typeof(LocalizationModel))]
    internal static class LocalizationModelSilencer
    {
        private static readonly MethodInfo UnityDebugLogWarning = AccessTools.DeclaredMethod(typeof(Debug), nameof(Debug.LogWarning), new Type[] { typeof(object) });

#if BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
        [HarmonyPatch(nameof(LocalizationModel.Get), new[] { typeof(string), typeof(Polyglot.Language) })]
#else
        [HarmonyPatch(nameof(LocalizationModel.Get))]
#endif
#if !BEAT_SABER_1_29_0 && !LEGATO_NATIVE_POLYGLOT
        [HarmonyPatch(nameof(LocalizationModel.TryGet))]
#endif
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(UnityDebugLogWarning))
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
#endif
