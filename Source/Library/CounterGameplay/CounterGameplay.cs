using IPA.Utilities;
using System.Collections.Generic;

namespace Legato.Gameplay {
    internal static class GameplaySceneAccess {
        internal static GameplayCoreSceneSetupData FromTransition(LevelScenesTransitionSetupData data) =>
#if LEGATO_BEFORE_MULTIPLAYER
            data.Get<GameplayCoreSceneSetupData>();
#else
            FieldAccessor<LevelScenesTransitionSetupData, GameplayCoreSceneSetupData>.Get(data, "<gameplayCoreSceneSetupData>k__BackingField");
#endif

        internal static BeatmapKey Key(GameplayCoreSceneSetupData data) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            data.difficultyBeatmap;
#else
            data.beatmapKey;
#endif
        internal static BeatmapLevel Level(GameplayCoreSceneSetupData data) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            data.difficultyBeatmap.level;
#else
            data.beatmapLevel;
#endif
        internal static float NoteJumpBeatOffset(GameplayCoreSceneSetupData data) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            data.difficultyBeatmap.noteJumpStartBeatOffset;
#else
            data.beatmapBasicData.noteJumpStartBeatOffset;
#endif

        internal static PlayerLevelStatsData Stats(PlayerData player, BeatmapKey key) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            player.GetPlayerLevelStatsData(key);
#else
            player.GetOrCreatePlayerLevelStatsData(key);
#endif
        internal static bool Rotating(GameplayCoreSceneSetupData data) {
#if LEGATO_BEFORE_MULTIPLAYER
            string name = data.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
#else
            string name = BeatmapKeyExtensions.CharacteristicSerializedName(Key(data));
#endif
            return name == "90Degree" || name == "360Degree";
        }
    }

    internal enum NoteScoringKind { Other, Normal, ArcHead, ArcTail, ChainHead, ChainLink }

    internal static class NoteScoring {
#if LEGATO_BEFORE_MULTIPLAYER
        internal static NoteScoringKind Kind(NoteData note) =>
            note.noteType is NoteType.NoteA or NoteType.NoteB ? NoteScoringKind.Normal : NoteScoringKind.Other;
#else
        internal static NoteScoringKind Kind(NoteData note) => note.scoringType switch {
            NoteData.ScoringType.Normal => NoteScoringKind.Normal,
#if LEGATO_SLIDER_SCORING_TYPES
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
#endif
    }
}

namespace Legato.Scenes {
    internal static class TutorialPreview {
        internal static void Initialize(TutorialScenesTransitionSetupData setup, PlayerSpecificSettings settings) {
#if LEGATO_BEFORE_MULTIPLAYER
            setup.Init();
#elif LEGATO_GAMEPLAY_WITHOUT_ADDITIONAL_INFORMATION
            setup.Init(settings);
#elif LEGATO_SCRIPTABLE_SCENE_TRANSITIONS
            var starters = UnityEngine.Resources.FindObjectsOfTypeAll<SimpleLevelStarter>();
            if (starters.Length == 0) throw new System.InvalidOperationException("The menu level starter is unavailable.");
            var environments = FieldAccessor<SimpleLevelStarter, EnvironmentsListModel>.Get(starters[0], "_environmentsListModel");
#if LEGATO_TUTORIAL_WITHOUT_ADDITIONAL_INFORMATION
            setup.Init(settings, environments);
#else
            setup.Init(settings, environments, new GameplayAdditionalInformation());
#endif
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
