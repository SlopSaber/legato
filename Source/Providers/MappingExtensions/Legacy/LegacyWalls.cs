using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace MappingExtensions.HarmonyPatches
{
    [HarmonyPatch(typeof(ObstacleController), nameof(ObstacleController.Init))]
    internal static class LegacyWallDimensions
    {
        private static void Prefix(ObstacleData obstacleData, ref Vector3 startPos, ref Vector3 midPos, ref Vector3 endPos, ref float height)
        {
            int type = (int)obstacleData.obstacleType;
            if (type is >= 1000 and <= 4000)
            {
                height *= (type - 1000) / 1000f;
            }
            else if (type is >= 4001 and <= 4005000)
            {
                int encodedHeight = type - 4001;
                height *= (encodedHeight / 1000) / 1000f;
                var offset = Vector3.up * ((encodedHeight % 1000) * 0.004f);
                startPos += offset;
                midPos += offset;
                endPos += offset;
            }
        }

        private static float DecodeWidth(float width)
        {
            MappingExtensionsData.TryDecodePrecisionWidth(width, out float decoded);
            return decoded;
        }

        private static float EffectiveDuration(ObstacleData obstacle, Vector3 midPos, Vector3 endPos, float move2Duration)
        {
            float duration = obstacle.duration;
            if (duration >= 0f && (duration > 0.25f ||
                !(MappingExtensionsData.IsPrecisionValue(obstacle.lineIndex) ||
                  MappingExtensionsData.IsPrecisionValue(obstacle.width) || (int)obstacle.obstacleType >= 1000)))
            {
                return duration;
            }
            float distance = (endPos - midPos).magnitude;
            if (distance <= Mathf.Epsilon || move2Duration <= 0f) return duration;

            float length = distance / move2Duration * duration;
            if (length < 0f) return Mathf.Max(-length, 35f) / distance * move2Duration;
            return length < 0.05f ? 0.05f / distance * move2Duration : duration;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var width = AccessTools.PropertyGetter(typeof(ObstacleData), nameof(ObstacleData.width));
            var duration = AccessTools.PropertyGetter(typeof(ObstacleData), nameof(ObstacleData.duration));
            var decodeWidth = AccessTools.Method(typeof(LegacyWallDimensions), nameof(DecodeWidth));
            var effectiveDuration = AccessTools.Method(typeof(LegacyWallDimensions), nameof(EffectiveDuration));
            var codes = instructions.ToList();
            int widthReads = 0;
            int durationReads = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(width) && i + 1 < codes.Count && codes[i + 1].opcode == OpCodes.Conv_R4)
                {
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, decodeWidth));
                    widthReads++;
                }
                else if (codes[i].Calls(duration))
                {
                    var mid = new CodeInstruction(OpCodes.Ldarg_S, (byte)4);
                    mid.labels.AddRange(codes[i].labels);
                    codes[i].labels.Clear();
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = effectiveDuration;
                    codes.InsertRange(i, new[] { mid, new CodeInstruction(OpCodes.Ldarg_S, (byte)5), new CodeInstruction(OpCodes.Ldarg_S, (byte)7) });
                    i += 3;
                    durationReads++;
                }
            }
            if (widthReads != 1 || durationReads != 2)
                throw new System.InvalidOperationException($"Unexpected obstacle dimensions in native Init ({widthReads} width, {durationReads} duration).");
            return codes;
        }
    }

    [HarmonyPatch(typeof(StretchableObstacle), nameof(StretchableObstacle.SetSizeAndColor))]
    internal static class LegacyWallVisualSize
    {
        private static void Prefix(ref float width, ref float height)
        {
            if (Plugin.active)
            {
                width = Mathf.Abs(width);
                height = Mathf.Abs(height);
            }
        }
    }
}
