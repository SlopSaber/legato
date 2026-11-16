using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SongCore.Utilities;
using UnityEngine;

namespace MappingExtensions.HarmonyPatches
{
    internal static class LegacyObstacleTypeEncoding
    {
        private const int PrecisionUnit = 1000;
        private const int PrecisionHeightMarker = 1000;
        private const int StartHeightMarker = 4001;
        private const int MaxStartHeight = 999;
        private const int MaxWallHeight = 4000;
        internal const int MaxEncodedType = StartHeightMarker + MaxWallHeight * PrecisionUnit + MaxStartHeight;

        private const float WallHeightToGameHeightMultiplier = 5f;
        private const int EncodedLayerGroundOffset = 1000;
        private const float StartHeightToLayerDivisor = 750f;

        internal enum Mode
        {
            PrecisionHeight,
            PrecisionHeightWithStart
        }

        internal readonly struct DecodedType
        {
            internal readonly Mode mode;
            internal readonly int wallHeight;
            internal readonly int startHeight;

            internal DecodedType(Mode mode, int wallHeight, int startHeight)
            {
                this.mode = mode;
                this.wallHeight = wallHeight;
                this.startHeight = startHeight;
            }
        }

        internal static bool TryDecode(int encodedType, out DecodedType decodedType)
        {
            decodedType = default;
            if (encodedType is < PrecisionHeightMarker or > MaxEncodedType)
            {
                return false;
            }

            if (encodedType >= StartHeightMarker)
            {
                var encodedHeightAndStart = encodedType - StartHeightMarker;
                decodedType = new DecodedType(
                    Mode.PrecisionHeightWithStart,
                    encodedHeightAndStart / PrecisionUnit,
                    encodedHeightAndStart % PrecisionUnit);
                return true;
            }

            decodedType = new DecodedType(Mode.PrecisionHeight, encodedType - PrecisionHeightMarker, 0);
            return true;
        }

        internal static int EncodeHeight(DecodedType decodedType)
        {
            return (int)(decodedType.wallHeight / (float)PrecisionUnit * WallHeightToGameHeightMultiplier * PrecisionUnit + PrecisionHeightMarker);
        }

        internal static int EncodeLayer(DecodedType decodedType)
        {
            if (decodedType.mode != Mode.PrecisionHeightWithStart)
            {
                return 0;
            }

            // Legacy ME v2 expands authored start height into the precision layer space used by wall art maps.
            // An offset of 1000 keeps converted walls aligned to Beat Saber's 0.6m wall grid.
            return (int)(decodedType.startHeight / StartHeightToLayerDivisor * WallHeightToGameHeightMultiplier * PrecisionUnit + EncodedLayerGroundOffset);
        }
    }

#if BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(BeatmapSaveDataVersion3.BeatmapSaveData), nameof(BeatmapSaveDataVersion3.BeatmapSaveData.GetHeightForObstacleType))]
#else
    [HarmonyPatch(typeof(BeatmapDataLoaderVersion2_6_0AndEarlier.BeatmapDataLoader.ObstacleConverter), nameof(BeatmapDataLoaderVersion2_6_0AndEarlier.BeatmapDataLoader.ObstacleConverter.GetHeightForObstacleType))]
#endif
    internal static class BeatmapDataLoaderObstacleConverterGetHeightForObstacleTypePatch
    {
        private static void Postfix(ref int __result, MappingObstacleType __0)
        {
            if (!LegacyObstacleTypeEncoding.TryDecode((int)__0, out var decodedType))
            {
                return;
            }

            __result = LegacyObstacleTypeEncoding.EncodeHeight(decodedType);
        }
    }

#if BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(BeatmapSaveDataVersion3.BeatmapSaveData), nameof(BeatmapSaveDataVersion3.BeatmapSaveData.GetLayerForObstacleType))]
#else
    [HarmonyPatch(typeof(BeatmapDataLoaderVersion2_6_0AndEarlier.BeatmapDataLoader.ObstacleConverter), nameof(BeatmapDataLoaderVersion2_6_0AndEarlier.BeatmapDataLoader.ObstacleConverter.GetLayerForObstacleType))]
#endif
    internal static class BeatmapDataLoaderObstacleConverterGetLayerForObstacleTypePatch
    {
        private static void Postfix(ref int __result, MappingObstacleType __0)
        {
            if (!LegacyObstacleTypeEncoding.TryDecode((int)__0, out var decodedType))
            {
                return;
            }

            __result = LegacyObstacleTypeEncoding.EncodeLayer(decodedType);
        }
    }

    [HarmonyPatch(typeof(BeatmapObjectSpawnMovementData), nameof(BeatmapObjectSpawnMovementData.GetObstacleSpawnData))]
    internal static class BeatmapObjectSpawnMovementDataGetObstacleSpawnDataPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Replaces the obstacle width.
            return new CodeMatcher(instructions)
                .MatchEndForward(
                    new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodBase)i.operand).Name == $"get_{nameof(ObstacleController.width)}"),
                    new CodeMatch(OpCodes.Conv_R4),
                    new CodeMatch())
                .ThrowIfInvalid("Could not find obstacle width access in BeatmapObjectSpawnMovementData.GetObstacleSpawnData")
                .Insert(Transpilers.EmitDelegate<Func<float, float>>(obstacleWidth =>
                {
                    if (!MappingExtensionsData.TryDecodePrecisionWidth(obstacleWidth, out var width))
                    {
                        return obstacleWidth;
                    }

                    return width;
                }))
                .InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(ObstacleController), nameof(ObstacleController.Init))]
    internal static class ObstacleControllerInitPatch
    {
#if BEAT_SABER_1_29_0
        private static void Prefix(ObstacleData obstacleData, ref float height)
#else
        private static void Prefix(ObstacleData obstacleData, ref ObstacleSpawnData obstacleSpawnData)
#endif
        {
            if (!MappingExtensionsData.TryDecodePrecisionHeight(obstacleData.height, out var obstacleHeight))
            {
                if (!Plugin.active || obstacleData.height <= 2)
                {
                    return;
                }

                obstacleHeight = obstacleData.height;
            }

            obstacleHeight *= StaticBeatmapObjectSpawnMovementData.kNoteLinesDistance;

#if BEAT_SABER_1_29_0
            height = obstacleHeight;
#else
            obstacleSpawnData = new ObstacleSpawnData(obstacleSpawnData.moveOffset, obstacleSpawnData.obstacleWidth, obstacleHeight);
#endif
        }

        private static void Postfix(ObstacleController __instance, ObstacleData obstacleData)
        {
            if (!ShouldNormalizeVisualLength(obstacleData, __instance._length))
            {
                return;
            }

            bool wasNegative = __instance._length < 0f;
            if (obstacleData.duration < 0f && wasNegative)
            {
                var lengthPerBeat = __instance._length / obstacleData.duration;
                if (lengthPerBeat <= Mathf.Epsilon)
                {
                    return;
                }

                var negativeWallLength = StretchableObstacleNegativeLengthPatch.GetNormalizedVisualLength(__instance._length);
                __instance._length = negativeWallLength;
                __instance._obstacleDuration = negativeWallLength / lengthPerBeat;
                Resize(__instance, negativeWallLength, wasNegative);
                return;
            }

            var precisionWallLength = StretchableObstacleNegativeLengthPatch.GetNormalizedVisualLength(__instance._length);
            __instance._length = precisionWallLength;
            Resize(__instance, precisionWallLength, wasNegative);
        }

        private static void Resize(ObstacleController controller, float length, bool wasNegative) {
#if BEAT_SABER_1_29_0
            controller._stretchableObstacle.SetSizeAndColor(controller._width, controller._height, length, controller.color);
            if (wasNegative) StretchableObstacleNegativeLengthPatch.NormalizeLegacy(controller._stretchableObstacle, controller._width, controller._height, length);
#else
            controller._stretchableObstacle.SetSizeAndOffset(controller._width, controller._height, length, controller.manualUvOffset);
#endif
        }

        private static bool ShouldNormalizeVisualLength(ObstacleData obstacleData, float length)
        {
            if (length < 0f)
            {
                return true;
            }

            return obstacleData.duration <= 0.25f
                   && (MappingExtensionsData.IsPrecisionValue(obstacleData.lineIndex)
                       || MappingExtensionsData.IsPrecisionValue(obstacleData.width)
                       || MappingExtensionsData.IsPrecisionValue((int)obstacleData.lineLayer)
                       || MappingExtensionsData.IsPrecisionValue(obstacleData.height));
        }
    }

#if BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(StretchableObstacle), nameof(StretchableObstacle.SetSizeAndColor))]
#else
    [HarmonyPatch(typeof(StretchableObstacle), "CalculateObstacleTransformProperties")]
#endif
    internal static class StretchableObstacleNegativeLengthPatch
    {
        internal const float TinyPrecisionWallVisualLength = 0.05f;
        private const float NegativeWallMinimumVisualLength = 35f;

#if BEAT_SABER_1_29_0
        private static void Postfix(StretchableObstacle __instance, float width, float height, float length) {
            if (length < 0f) NormalizeLegacy(__instance, width, height, GetNormalizedVisualLength(length));
        }

        internal static void NormalizeLegacy(StretchableObstacle obstacle, float width, float height, float length) {
            var position = new Vector3(0f, height * 0.5f, length * -0.5f);
            var size = new Vector3(width, height, length);
            obstacle._bounds = new Bounds(position, size);
            if (obstacle._obstacleCore != null) {
                obstacle._obstacleCore.localPosition = position;
                obstacle._obstacleCore.localScale = size - Vector3.one * obstacle._coreOffset;
            }
            obstacle._obstacleFrame.length = length;
            obstacle._obstacleFrame.localPosition = position;
            obstacle._obstacleFrame.Refresh();
            if (obstacle._obstacleFakeGlow != null) {
                obstacle._obstacleFakeGlow.length = length + obstacle._fakeGlowOffset.z;
                obstacle._obstacleFakeGlow.localPosition = position;
                obstacle._obstacleFakeGlow.Refresh();
            }
        }
#else
        private static void Postfix(
            StretchableObstacle __instance,
            float width,
            float height,
            float length,
            ref Vector3 localPosition,
            ref Vector3 size,
            ref Vector3 scale)
        {
            if (length >= 0f)
            {
                return;
            }

            var positiveLength = GetNormalizedVisualLength(length);
            size = new Vector3(width, height, positiveLength);
            localPosition = new Vector3(0f, height * 0.5f, positiveLength * -0.5f);
            scale = size - __instance._coreOffset;
        }

#endif
        internal static float GetNormalizedVisualLength(float length)
        {
            if (length < 0f)
            {
                return Mathf.Max(-length, NegativeWallMinimumVisualLength);
            }

            return length > TinyPrecisionWallVisualLength ? TinyPrecisionWallVisualLength : length;
        }
    }

#if !BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(StretchableObstacle))]
    internal static class StretchableObstacleNegativeLengthRendererPatch
    {
        [HarmonyPatch(nameof(StretchableObstacle.SetAllProperties))]
        [HarmonyPostfix]
        private static void SetAllPropertiesPostfix(StretchableObstacle __instance, float height, float length)
        {
            NormalizeRendererLength(__instance, height, length);
        }

        [HarmonyPatch(nameof(StretchableObstacle.SetSizeAndOffset))]
        [HarmonyPostfix]
        private static void SetSizeAndOffsetPostfix(StretchableObstacle __instance, float height, float length)
        {
            NormalizeRendererLength(__instance, height, length);
        }

        private static void NormalizeRendererLength(StretchableObstacle stretchableObstacle, float height, float length)
        {
            if (length >= 0f)
            {
                return;
            }

            var positiveLength = StretchableObstacleNegativeLengthPatch.GetNormalizedVisualLength(length);
            var localPosition = new Vector3(0f, height * 0.5f, positiveLength * -0.5f);

            stretchableObstacle._obstacleFrame.length = positiveLength;
            stretchableObstacle._obstacleFrame.localPosition = localPosition;
            stretchableObstacle._obstacleFrame.Refresh();

            if (stretchableObstacle._obstacleFakeGlow != null)
            {
                stretchableObstacle._obstacleFakeGlow.length = positiveLength + stretchableObstacle._fakeGlowOffset.z;
                stretchableObstacle._obstacleFakeGlow.localPosition = localPosition;
                stretchableObstacle._obstacleFakeGlow.Refresh();
            }
        }
    }
#endif
}
