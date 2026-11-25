#if LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Linq;
using HMUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using SongCore.Data;
using SongCore.UI;
using SongCore.Utilities;
using UnityEngine;
using Zenject;

namespace SongCore.Hooks
{
    internal sealed class SongDataMenuHooks : IInitializable, IDisposable
    {
        private readonly RequirementsUI _requirements;
        private readonly PluginConfig _config;
        private Hook? _refreshContent;
        private Hook? _mapColorScheme;
        private ILHook? _difficultyLabels;

        private SongDataMenuHooks(RequirementsUI requirements, PluginConfig config)
        {
            _requirements = requirements;
            _config = config;
        }

        public void Initialize()
        {
            _refreshContent = new Hook(typeof(StandardLevelDetailView).GetMethod(nameof(StandardLevelDetailView.RefreshContent))!, RefreshContent, true);
            _mapColorScheme = new Hook(typeof(StandardLevelScenesTransitionSetupDataSO).GetMethod(nameof(StandardLevelScenesTransitionSetupDataSO.Init))!, ApplyMapColors, true);
            _difficultyLabels = new ILHook(typeof(BeatmapDifficultySegmentedControlController).GetMethod(nameof(BeatmapDifficultySegmentedControlController.SetData))!, context =>
            {
                var cursor = new ILCursor(context);
                cursor.GotoNext(MoveType.Before, instruction => instruction.MatchCallvirt<TextSegmentedControl>(nameof(TextSegmentedControl.SetTexts)));
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<string[], IDifficultyBeatmap[], string[]>>(ApplyDifficultyLabels);
            }, true);
        }

        public void Dispose()
        {
            _difficultyLabels?.Dispose();
            _mapColorScheme?.Dispose();
            _refreshContent?.Dispose();
        }

        private string[] ApplyDifficultyLabels(string[] labels, IDifficultyBeatmap[] difficulties)
        {
            if (!_config.DisplayDiffLabels)
                return labels;

            for (int index = 0; index < labels.Length && index < difficulties.Length; index++)
            {
                string? label = Collections.GetCustomLevelSongDifficultyData(difficulties[index])?._difficultyLabel;
                if (!string.IsNullOrWhiteSpace(label))
                    labels[index] = label!.Replace("<", "<\u200B").Replace(">", ">\u200B");
            }
            return labels;
        }

        private void ApplyMapColors(Action<StandardLevelScenesTransitionSetupDataSO, IDifficultyBeatmap, OverrideEnvironmentSettings?, ColorScheme?, GameplayModifiers, PlayerSpecificSettings, PracticeSettings?, string, bool> original,
            StandardLevelScenesTransitionSetupDataSO transition, IDifficultyBeatmap difficulty, OverrideEnvironmentSettings? environments, ColorScheme? colors,
            GameplayModifiers modifiers, PlayerSpecificSettings settings, PracticeSettings? practice, string backButtonText, bool testSounds)
        {
            var data = Collections.GetCustomLevelSongDifficultyData(difficulty);
            if (data != null && Utils.DiffHasColors(data) && (_config.CustomSongNoteColors || _config.CustomSongEnvironmentColors || _config.CustomSongObstacleColors))
            {
                EnvironmentInfoSO environment = difficulty.GetEnvironmentInfo();
                if (environments?.overrideEnvironments == true)
                    environment = environments.GetOverrideEnvironmentInfoForType(environment.environmentType);
                ColorScheme basis = colors ?? new ColorScheme(environment.colorScheme);
                Color left = ChooseColor(_config.CustomSongNoteColors, data._colorLeft, basis.saberAColor);
                Color right = ChooseColor(_config.CustomSongNoteColors, data._colorRight, basis.saberBColor);
                Color envLeft = ChooseColor(_config.CustomSongEnvironmentColors, data._envColorLeft ?? data._colorLeft, basis.environmentColor0);
                Color envRight = ChooseColor(_config.CustomSongEnvironmentColors, data._envColorRight ?? data._colorRight, basis.environmentColor1);
                Color boostLeft = ChooseColor(_config.CustomSongEnvironmentColors, data._envColorLeftBoost, basis.environmentColor0Boost);
                Color boostRight = ChooseColor(_config.CustomSongEnvironmentColors, data._envColorRightBoost, basis.environmentColor1Boost);
                Color obstacle = ChooseColor(_config.CustomSongObstacleColors, data._obstacleColor, basis.obstaclesColor);
                bool boost = basis.supportsEnvironmentColorBoost || _config.CustomSongEnvironmentColors && (data._envColorLeftBoost != null || data._envColorRightBoost != null);
                colors = new ColorScheme(basis, left, right, envLeft, envRight, boost, boostLeft, boostRight, obstacle);
            }

            original(transition, difficulty, environments, colors, modifiers, settings, practice, backButtonText, testSounds);
        }

        private static Color ChooseColor(bool enabled, SongData.MapColor? value, Color fallback) =>
            enabled && value != null ? Utils.ColorFromMapColor(value) : fallback;

        private void RefreshContent(Action<StandardLevelDetailView> original, StandardLevelDetailView view)
        {
            original(view);
            BeatmapKey? difficulty = view.selectedDifficultyBeatmap;
            BeatmapLevel? level = difficulty?.level;
            var data = level != null ? Collections.GetCustomLevelSongData(level.levelID) : null;
            var metadata = difficulty != null ? Collections.GetCustomLevelSongDifficultyData(difficulty) : null;
            var details = metadata?.additionalDifficultyData;
            bool wip = level?.levelID.EndsWith(" WIP", StringComparison.Ordinal) == true;
            bool missing = difficulty?.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName == "MissingCharacteristic"
                || details?._requirements.Any(requirement => !Collections.capabilities.Contains(requirement)) == true;
            bool colors = metadata != null && Utils.DiffHasColors(metadata);
            bool show = data != null && (missing || wip || colors || data.contributors.Any() ||
                details != null && (details._requirements.Length > 0 || details._suggestions.Length > 0 || details._warnings.Length > 0 || details._information.Length > 0));

            if (missing || wip || details?._warnings.Contains("WIP") == true)
                view.playButton.interactable = false;
            if (missing)
                view.practiceButton.interactable = false;

            _requirements.beatmapLevel = level;
            _requirements.beatmapKey = difficulty;
            _requirements.songData = data;
            _requirements.diffData = metadata;
            _requirements.wipFolder = wip;
            _requirements.ButtonGlowColor = show;
            _requirements.ButtonInteractable = show;
            _requirements.SetRainbowColors(show && colors);
        }
    }
}
#endif
