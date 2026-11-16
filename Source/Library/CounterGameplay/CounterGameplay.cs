using IPA.Utilities;
using System.Collections.Generic;

namespace Legato.Gameplay {
    internal static class GameplaySceneAccess {
        internal static GameplayCoreSceneSetupData FromTransition(LevelScenesTransitionSetupData data) =>
            FieldAccessor<LevelScenesTransitionSetupData, GameplayCoreSceneSetupData>.Get(data, "<gameplayCoreSceneSetupData>k__BackingField");

        internal static BeatmapKey Key(GameplayCoreSceneSetupData data) =>
#if BEAT_SABER_1_29_0
            data.difficultyBeatmap;
#else
            data.beatmapKey;
#endif
        internal static BeatmapLevel Level(GameplayCoreSceneSetupData data) =>
#if BEAT_SABER_1_29_0
            data.difficultyBeatmap.level;
#else
            data.beatmapLevel;
#endif
        internal static float NoteJumpBeatOffset(GameplayCoreSceneSetupData data) =>
#if BEAT_SABER_1_29_0
            data.difficultyBeatmap.noteJumpStartBeatOffset;
#else
            data.beatmapBasicData.noteJumpStartBeatOffset;
#endif

        internal static PlayerLevelStatsData Stats(PlayerData player, BeatmapKey key) =>
#if BEAT_SABER_1_29_0
            player.GetPlayerLevelStatsData(key);
#else
            player.GetOrCreatePlayerLevelStatsData(key);
#endif
        internal static bool Rotating(GameplayCoreSceneSetupData data) {
            string name = BeatmapKeyExtensions.CharacteristicSerializedName(Key(data));
            return name == "90Degree" || name == "360Degree";
        }
    }

    internal enum NoteScoringKind { Other, Normal, ArcHead, ArcTail, ChainHead, ChainLink }

    internal static class NoteScoring {
        internal static NoteScoringKind Kind(NoteData note) => note.scoringType switch {
            NoteData.ScoringType.Normal => NoteScoringKind.Normal,
#if BEAT_SABER_1_29_0
            NoteData.ScoringType.SliderHead => NoteScoringKind.ArcHead,
            NoteData.ScoringType.SliderTail => NoteScoringKind.ArcTail,
            NoteData.ScoringType.BurstSliderHead => NoteScoringKind.ChainHead,
            NoteData.ScoringType.BurstSliderElement => NoteScoringKind.ChainLink,
#else
            NoteData.ScoringType.ArcHead => NoteScoringKind.ArcHead,
            NoteData.ScoringType.ArcTail => NoteScoringKind.ArcTail,
            NoteData.ScoringType.ChainHead => NoteScoringKind.ChainHead,
            NoteData.ScoringType.ChainLink => NoteScoringKind.ChainLink,
#endif
            _ => NoteScoringKind.Other
        };
    }
}

namespace Legato.Scenes {
    internal static class TutorialPreview {
        internal static void Initialize(TutorialScenesTransitionSetupData setup, PlayerSpecificSettings settings) {
#if BEAT_SABER_1_29_0
            setup.Init(settings);
#else
            setup.Init(settings, new GameplayAdditionalInformation());
#endif
        }
        internal static void KeepScene(GameScenesManager manager, string scene, bool keep) {
            HashSet<string> scenes = FieldAccessor<GameScenesManager, HashSet<string>>.Get(manager, "_neverUnloadScenes");
            if (keep) scenes.Add(scene);
            else scenes.Remove(scene);
        }
    }
}
