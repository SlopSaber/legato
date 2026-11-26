#if LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Legato.Beatmaps {
    internal static class LegacyBeatmapLineClamp {
        private static readonly MethodInfo Clamp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp),
            new[] { typeof(int), typeof(int), typeof(int) });
        private static readonly MethodInfo AddObject = AccessTools.Method(typeof(List<BeatmapObjectData>),
            nameof(List<BeatmapObjectData>.Add));

        internal static IEnumerable<CodeInstruction> Apply(IEnumerable<CodeInstruction> instructions, Action<string> warn) {
            var code = new List<CodeInstruction>(instructions);
            var destinations = new List<int>(2);
            for (int index = 1; index < code.Count - 2; index++) {
                if (code[index].opcode == OpCodes.Ldelem_Ref && code[index + 2].Calls(AddObject))
                    destinations.Add(index);
            }

            if (destinations.Count != 2) {
                warn("Could not identify both beatmap line insertion points.");
                return code;
            }

            // Clamp the storage lane for notes and obstacles. Their authored coordinates stay intact.
            for (int index = destinations.Count - 1; index >= 0; index--) {
                int position = destinations[index];
                if (code[position - 1].Calls(Clamp)) continue;
                code.InsertRange(position, new[] {
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Ldc_I4_3),
                    new CodeInstruction(OpCodes.Call, Clamp)
                });
            }
            return code;
        }
    }
}
#endif
