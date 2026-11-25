using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using IPA.Utilities;
namespace Legato.ChatPlex {
    internal static class NativeAccess {
        internal static BeatmapCharacteristic Characteristic(BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            key.parentDifficultyBeatmapSet.beatmapCharacteristic;
#elif LEGATO_SCRIPTABLE_CHARACTERISTICS
            key.beatmapCharacteristic;
#else
            key.characteristic;
#endif

        internal static IEnumerable<string> Mappers(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            new[] { level.levelAuthorName };
#else
            level.allMappers;
#endif
        internal static IEnumerable<BeatmapDifficulty> Difficulties(BeatmapLevel level, BeatmapCharacteristic characteristic) =>
#if BEAT_SABER_1_29_0
            level.previewDifficultyBeatmapSets.Where(set => set.beatmapCharacteristic == characteristic).SelectMany(set => set.beatmapDifficulties);
#else
            level.GetBeatmapKeys().Where(key => Characteristic(key) == characteristic).Select(key => key.difficulty);
#endif
        internal static string PreviewAudioPath(BeatmapLevel level) =>
#if BEAT_SABER_1_29_0
            level is CustomPreviewBeatmapLevel preview ? preview.songPreviewAudioClipPath : null;
#else
            level.previewMediaData is FileSystemPreviewMediaData media ? media._previewAudioClipPath : null;
#endif
        internal static VRController SelectedController(VRUIControls.VRPointer pointer) =>
#if BEAT_SABER_1_29_0
            pointer?.vrController;
#else
            pointer?.lastSelectedVrController;
#endif
        internal static Transform ViewAnchor(VRController controller) =>
#if BEAT_SABER_1_29_0
            controller.transform;
#else
            controller.viewAnchorTransform;
#endif
        internal static float VerticalAxis(VRController controller) =>
#if BEAT_SABER_1_29_0
            controller.verticalAxisValue;
#else
            controller.thumbstick.y;
#endif
        internal static void SetFaceInfo(TMP_FontAsset font, UnityEngine.TextCore.FaceInfo info) {
#if BEAT_SABER_1_29_0
            font.SetField("m_FaceInfo", info);
#else
            font.faceInfo = info;
#endif
        }
#if BEAT_SABER_1_29_0
        internal static IEnumerable<BeatmapKey> GetBeatmapKeys(this BeatmapLevel level) =>
            level is IBeatmapLevel full ? full.beatmapLevelData.difficultyBeatmapSets.SelectMany(set => set.difficultyBeatmaps) : Enumerable.Empty<BeatmapKey>();
#endif
#if LEGATO_CHARACTERISTIC_NAME_PROPERTY
        internal static string SerializedName(this BeatmapCharacteristicSO characteristic) => characteristic.serializedName;
#endif
#if LEGATO_SCRIPTABLE_CHARACTERISTICS
        internal static string HintLocalizationKey(this BeatmapCharacteristicSO characteristic) => characteristic.descriptionLocalizationKey;
#endif
#if BEAT_SABER_1_37_1 && !LEGATO_ADDITIONAL_PACK_LEVELS
        internal static BeatmapLevel[] AllBeatmapLevels(this BeatmapLevelPack pack) => pack.beatmapLevels;
#endif
    }
}
