using System;
using IPA.Utilities;
using Utils = SongCore.Utilities.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HMUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using SongCore.Data;
using SongCore.UI;
using SongCore.Utilities;
using TMPro;
using UnityEngine;
using Zenject;
#if BEAT_SABER_1_29_0
using Legato;
#endif

namespace SongCore.Hooks
{
    internal class SongDataMenuHooks : IInitializable, IDisposable
    {
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private readonly CustomLevelLoader _customLevelLoader;
        private readonly RequirementsUI _requirementsUI;
        private readonly PluginConfig _config;
        private readonly Dictionary<string, Dictionary<BeatmapDifficulty, string>> _characteristicDifficultyLabels = new();
        private readonly Dictionary<string, Sprite> _characteristicDetailsSprites = new();

        private Hook _getSongDataHook = null!;
        private Hook _textSizeLimitHook = null!;
        private Hook _customDifficultyLabelsHook = null!;
        private Hook _customDifficultyTextHook = null!;
        private Hook _defaultCharacteristicHook = null!;
        private Hook _cosmeticCharacteristicHook = null!;
        private Hook _requirementsHook = null!;
        private Hook _buttonsStateHook = null!;
#if LEGATO_SPLIT_SCENE_COLOR_SETUP
        private Hook _overrideColorSchemeHook = null!;
        private Hook _overrideMultiplayerColorSchemeHook = null!;
#else
        private ILHook _overrideColorSchemeHook = null!;
        private ILHook _overrideMultiplayerColorSchemeHook = null!;
#endif
        private SongData? _songData;

        private SongDataMenuHooks(StandardLevelDetailViewController standardLevelDetailViewController, CustomLevelLoader customLevelLoader, RequirementsUI requirementsUI, PluginConfig config)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
            _customLevelLoader = customLevelLoader;
            _requirementsUI = requirementsUI;
            _config = config;
        }

        public void Initialize()
        {
            _getSongDataHook = new Hook(typeof(LevelCollectionViewController).GetMethod(nameof(LevelCollectionViewController.HandleLevelCollectionTableViewDidSelectLevel), BindingFlags.Instance | BindingFlags.NonPublic)!, HandleDidSelectLevel, true);
            _textSizeLimitHook = new Hook(typeof(BeatmapDifficultySegmentedControlController).GetMethod(nameof(BeatmapDifficultySegmentedControlController.SetData))!, LimitTextSize, true);
            _customDifficultyLabelsHook = new Hook(typeof(BeatmapDifficultySegmentedControlController).GetMethod(nameof(BeatmapDifficultySegmentedControlController.SetData))!, SetData, true);
#if BEAT_SABER_1_29_0
            _customDifficultyTextHook = new Hook(typeof(LevelBar).GetMethod(nameof(LevelBar.Setup), new[] { typeof(IPreviewBeatmapLevel), typeof(BeatmapCharacteristicSO), typeof(BeatmapDifficulty) })!, SetupData, true);
#else
            _customDifficultyTextHook = new Hook(typeof(LevelBar).GetMethod(nameof(LevelBar.SetupData), BindingFlags.Instance | BindingFlags.NonPublic)!, SetupData, true);
#endif
            _defaultCharacteristicHook = new Hook(typeof(BeatmapCharacteristicSegmentedControlController).GetMethod(nameof(BeatmapCharacteristicSegmentedControlController.SetData))!, SelectDefaultCharacteristic, true);
            _cosmeticCharacteristicHook = new Hook(typeof(BeatmapCharacteristicSegmentedControlController).GetMethod(nameof(BeatmapCharacteristicSegmentedControlController.SetData))!, SetCosmeticCharacteristic, true);
            _requirementsHook = new Hook(typeof(StandardLevelDetailView).GetMethod(nameof(StandardLevelDetailView.RefreshContent))!, ProcessBeatmapRequirements, true);
#if !BEAT_SABER_1_29_0
            _buttonsStateHook = new Hook(typeof(StandardLevelDetailView).GetMethod(nameof(StandardLevelDetailView.CheckIfBeatmapLevelDataExists), BindingFlags.Instance | BindingFlags.NonPublic)!, SaveAndRestoreButtonsState, true);
#endif
#if LEGATO_SPLIT_SCENE_COLOR_SETUP
            _overrideColorSchemeHook = new Hook(typeof(StandardLevelScenesTransitionSetupData).GetMethod(nameof(StandardLevelScenesTransitionSetupData.InitColorInfo), BindingFlags.Instance | BindingFlags.NonPublic)!, OverrideSoloColorScheme, true);
            _overrideMultiplayerColorSchemeHook = new Hook(typeof(MultiplayerLevelScenesTransitionSetupData).GetMethod(nameof(MultiplayerLevelScenesTransitionSetupData.InitColorInfo), BindingFlags.Instance | BindingFlags.NonPublic)!, OverrideMultiplayerColorScheme, true);
#else
            _overrideColorSchemeHook = new ILHook(typeof(StandardLevelScenesTransitionSetupData).GetMethod(nameof(StandardLevelScenesTransitionSetupData.Init), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!, OverrideColorSchemeManipulator, true);
            _overrideMultiplayerColorSchemeHook = new ILHook(typeof(MultiplayerLevelScenesTransitionSetupData).GetMethod(nameof(MultiplayerLevelScenesTransitionSetupData.Init), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!, OverrideColorSchemeManipulator, true);
#endif
        }

        public void Dispose()
        {
            _getSongDataHook.Dispose();
            _textSizeLimitHook.Dispose();
            _customDifficultyLabelsHook.Dispose();
            _customDifficultyTextHook.Dispose();
            _defaultCharacteristicHook.Dispose();
            _cosmeticCharacteristicHook.Dispose();
            _requirementsHook.Dispose();
            _buttonsStateHook?.Dispose();
            _overrideColorSchemeHook.Dispose();
            _overrideMultiplayerColorSchemeHook.Dispose();

            foreach (var sprite in _characteristicDetailsSprites.Values)
            {
#if LEGATO_INLINE_SPRITE_DISPOSAL
                UnityEngine.Object.Destroy(sprite.texture);
                UnityEngine.Object.Destroy(sprite);
#else
                SpriteAsyncLoader.DestroySprite(sprite);
#endif
            }
        }

        private void HandleDidSelectLevel(Action<LevelCollectionViewController, LevelCollectionTableView, BeatmapLevel> original, LevelCollectionViewController instance, LevelCollectionTableView tableView, BeatmapLevel level)
        {
            _songData = Collections.GetCustomLevelSongData(level.levelID);

            if (_songData == null)
            {
                original(instance, tableView, level);
                return;
            }

            _characteristicDifficultyLabels.Clear();
            foreach (var difficultyData in _songData._difficulties)
            {
                _characteristicDifficultyLabels.TryAdd(difficultyData._beatmapCharacteristicName, new Dictionary<BeatmapDifficulty, string>());
                if (!string.IsNullOrWhiteSpace(difficultyData._difficultyLabel))
                {
                    _characteristicDifficultyLabels[difficultyData._beatmapCharacteristicName].TryAdd(difficultyData._difficulty, difficultyData._difficultyLabel);
                }
            }

            original(instance, tableView, level);
        }

        // TODO: Add Hover Hints that shows full text when text is too large?
#if BEAT_SABER_1_29_0
        private void LimitTextSize(Action<BeatmapDifficultySegmentedControlController, IReadOnlyList<IDifficultyBeatmap>, BeatmapDifficulty> original, BeatmapDifficultySegmentedControlController instance, IReadOnlyList<IDifficultyBeatmap> difficultyBeatmaps, BeatmapDifficulty selectedDifficulty)
        {
            original(instance, difficultyBeatmaps, selectedDifficulty);
#else
        private void LimitTextSize(Action<BeatmapDifficultySegmentedControlController, IEnumerable<BeatmapDifficulty>, BeatmapDifficulty, BeatmapDifficultyMask> original, BeatmapDifficultySegmentedControlController instance, IEnumerable<BeatmapDifficulty> difficultyBeatmaps, BeatmapDifficulty selectedDifficulty, BeatmapDifficultyMask allowedBeatmapDifficultyMask)
        {
            original(instance, difficultyBeatmaps, selectedDifficulty, allowedBeatmapDifficultyMask);
#endif

            if (_songData == null || !_config.DisplayDiffLabels)
            {
                return;
            }

            for (var i = 0; i < instance._difficultySegmentedControl._texts.Count; i++)
            {
                var cell = (TextSegmentedControlCell)instance._difficultySegmentedControl.cells[i];

                if (cell._text.alignment == TextAlignmentOptions.Midline)
                {
                    continue;
                }

                cell._text.enableAutoSizing = true;
                cell._text.fontSizeMin = 2.125f;
                cell._text.fontSizeMax = 3f;
                cell._text.alignment = TextAlignmentOptions.Midline;
#if !BEAT_SABER_1_29_0
                cell._text.m_textAlignment = TextAlignmentOptions.Converted;
#endif // needed to stop Awake() from resetting alignment
                cell._text.rectTransform.sizeDelta = new Vector2(0f, 1.2f);
                cell._text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

#if BEAT_SABER_1_29_0
        private void SetData(Action<BeatmapDifficultySegmentedControlController, IReadOnlyList<IDifficultyBeatmap>, BeatmapDifficulty> original, BeatmapDifficultySegmentedControlController instance, IReadOnlyList<IDifficultyBeatmap> difficultyBeatmaps, BeatmapDifficulty selectedDifficulty)
        {
            original(instance, difficultyBeatmaps, selectedDifficulty);
#else
        private void SetData(Action<BeatmapDifficultySegmentedControlController, IEnumerable<BeatmapDifficulty>, BeatmapDifficulty, BeatmapDifficultyMask> original, BeatmapDifficultySegmentedControlController instance, IEnumerable<BeatmapDifficulty> difficultyBeatmaps, BeatmapDifficulty selectedDifficulty, BeatmapDifficultyMask allowedBeatmapDifficultyMask)
        {
            original(instance, difficultyBeatmaps, selectedDifficulty, allowedBeatmapDifficultyMask);
#endif

            if (_songData == null || !_config.DisplayDiffLabels)
            {
                return;
            }

            var selectedBeatmapCharacteristic = _standardLevelDetailViewController._standardLevelDetailView._beatmapCharacteristicSegmentedControlController.selectedBeatmapCharacteristic.SerializedName();
            instance._difficultySegmentedControl._texts = instance._difficulties
                .Select((diff, i) => _characteristicDifficultyLabels.TryGetValue(selectedBeatmapCharacteristic, out var difficultyLabels) && difficultyLabels.TryGetValue(diff, out var difficultyLabel)
                    ? GetDifficultyLabel(difficultyLabel) ?? instance._difficultySegmentedControl._texts[i]
                    : instance._difficultySegmentedControl._texts[i])
                .ToArray();

            for (var i = 0; i < instance._difficultySegmentedControl._texts.Count; i++)
            {
                var cell = (TextSegmentedControlCell)instance._difficultySegmentedControl.cells[i];
                cell.text = instance._difficultySegmentedControl._texts[i];
            }
        }

#if BEAT_SABER_1_29_0
        private void SetupData(Action<LevelBar, IPreviewBeatmapLevel, BeatmapCharacteristicSO, BeatmapDifficulty> original, LevelBar instance, IPreviewBeatmapLevel beatmapLevel, BeatmapCharacteristicSO beatmapCharacteristic, BeatmapDifficulty beatmapDifficulty)
        {
            original(instance, beatmapLevel, beatmapCharacteristic, beatmapDifficulty);
            ApplyLevelBar(instance, beatmapDifficulty, beatmapCharacteristic);
        }
#else
        private Task SetupData(Func<LevelBar, BeatmapLevel, BeatmapDifficulty, BeatmapCharacteristic, Task> original, LevelBar instance, BeatmapLevel beatmapLevel, BeatmapDifficulty beatmapDifficulty, BeatmapCharacteristic beatmapCharacteristic)
        {
            var result = original(instance, beatmapLevel, beatmapDifficulty, beatmapCharacteristic);
            ApplyLevelBar(instance, beatmapDifficulty, beatmapCharacteristic);
            return result;
        }
#endif

        private void ApplyLevelBar(LevelBar instance, BeatmapDifficulty beatmapDifficulty, BeatmapCharacteristic beatmapCharacteristic)
        {

            if (_songData == null || !_config.DisplayDiffLabels || !instance._showDifficultyAndCharacteristic)
            {
                return;
            }

            if (_characteristicDifficultyLabels.TryGetValue(beatmapCharacteristic.SerializedName(), out var difficultyLabels) && difficultyLabels.TryGetValue(beatmapDifficulty, out var difficultyLabel))
            {
#if LEGATO_LEGACY_TEXTMESH_PRO
                instance._difficultyText.enableWordWrapping = false;
#else
                instance._difficultyText.textWrappingMode = TextWrappingModes.NoWrap;
#endif
                instance._difficultyText.overflowMode = TextOverflowModes.Ellipsis;
                instance._difficultyText.text = GetDifficultyLabel(difficultyLabel) ?? instance._difficultyText.text;
            }

            var characteristicDetails = _songData._characteristicDetails?.FirstOrDefault(d => d._beatmapCharacteristicName == beatmapCharacteristic.SerializedName());
            if (characteristicDetails != null)
            {
                var sprite = GetCharacteristicIcon(characteristicDetails._characteristicIconFilePath);
                if (sprite != null)
                {
                    instance._characteristicIconImageView.sprite = sprite;
                }
            }

        }

        private string? GetDifficultyLabel(string difficultyLabel)
        {
            return string.IsNullOrWhiteSpace(difficultyLabel) ? null : difficultyLabel.Replace("<", "<\u200B").Replace(">", ">\u200B");
        }

#if BEAT_SABER_1_29_0
        private void SelectDefaultCharacteristic(Action<BeatmapCharacteristicSegmentedControlController, IReadOnlyList<IDifficultyBeatmapSet>, BeatmapCharacteristic> original, BeatmapCharacteristicSegmentedControlController instance, IReadOnlyList<IDifficultyBeatmapSet> beatmapCharacteristics, BeatmapCharacteristic selectedBeatmapCharacteristic)
        {
            original(instance, beatmapCharacteristics, selectedBeatmapCharacteristic);
#else
        private void SelectDefaultCharacteristic(Action<BeatmapCharacteristicSegmentedControlController, IEnumerable<BeatmapCharacteristic>, BeatmapCharacteristic, HashSet<BeatmapCharacteristic>> original, BeatmapCharacteristicSegmentedControlController instance, IEnumerable<BeatmapCharacteristic> beatmapCharacteristics, BeatmapCharacteristic selectedBeatmapCharacteristic, HashSet<BeatmapCharacteristic> notAllowedCharacteristics)
        {
            original(instance, beatmapCharacteristics, selectedBeatmapCharacteristic, notAllowedCharacteristics);
#endif

            if (_songData == null || _songData._defaultCharacteristic == null || _songData._defaultCharacteristic == instance.selectedBeatmapCharacteristic.SerializedName())
            {
                return;
            }

            var index = SongCoreNative.AvailableCharacteristics(instance).FindIndex(c => c.SerializedName() == _songData._defaultCharacteristic);
            if (index != -1)
            {
                instance._segmentedControl.SelectCellWithNumber(index);
                instance._selectedBeatmapCharacteristic = SongCoreNative.AvailableCharacteristics(instance)[index];
            }
        }

#if BEAT_SABER_1_29_0
        private void SetCosmeticCharacteristic(Action<BeatmapCharacteristicSegmentedControlController, IReadOnlyList<IDifficultyBeatmapSet>, BeatmapCharacteristic> original, BeatmapCharacteristicSegmentedControlController instance, IReadOnlyList<IDifficultyBeatmapSet> beatmapCharacteristics, BeatmapCharacteristic selectedBeatmapCharacteristic)
        {
            original(instance, beatmapCharacteristics, selectedBeatmapCharacteristic);
#else
        private void SetCosmeticCharacteristic(Action<BeatmapCharacteristicSegmentedControlController, IEnumerable<BeatmapCharacteristic>, BeatmapCharacteristic, HashSet<BeatmapCharacteristic>> original, BeatmapCharacteristicSegmentedControlController instance, IEnumerable<BeatmapCharacteristic> beatmapCharacteristics, BeatmapCharacteristic selectedBeatmapCharacteristic, HashSet<BeatmapCharacteristic> notAllowedCharacteristics)
        {
            original(instance, beatmapCharacteristics, selectedBeatmapCharacteristic, notAllowedCharacteristics);
#endif

            if (_songData == null || _songData._characteristicDetails == null || !_config.DisplayCustomCharacteristics)
            {
                return;
            }

            foreach (var characteristicDetails in _songData._characteristicDetails)
            {
                var index = SongCoreNative.AvailableCharacteristics(instance).FindIndex(c => c.SerializedName() == characteristicDetails._beatmapCharacteristicName);

                if (index == -1)
                {
                    continue;
                }

                var dataItem = instance._segmentedControl._dataItems[index];
                var cell = (IconSegmentedControlCell)instance._segmentedControl.cells[index];

                if (!string.IsNullOrWhiteSpace(characteristicDetails._characteristicLabel))
                {
                    dataItem.hintText = characteristicDetails._characteristicLabel;
                    cell.hintText = characteristicDetails._characteristicLabel;
                }

                var icon = GetCharacteristicIcon(characteristicDetails._characteristicIconFilePath);
                if (icon != null)
                {
                    dataItem.icon = icon;
                    cell.sprite = icon;
                }
            }
        }

        private Sprite? GetCharacteristicIcon(string? characteristicIconFilePath)
        {
            if (string.IsNullOrWhiteSpace(characteristicIconFilePath))
            {
                return null;
            }

            var spritePath = Path.Combine(SongCoreNative.LevelDirectory(_standardLevelDetailViewController.beatmapLevel.levelID), characteristicIconFilePath);
            if (!_characteristicDetailsSprites.TryGetValue(spritePath, out var icon))
            {
                if ((icon = Utils.LoadSpriteFromFile(spritePath)) != null)
                {
                    _characteristicDetailsSprites.Add(spritePath, icon);
                }
            }

            return icon;
        }

        private void ProcessBeatmapRequirements(Action<StandardLevelDetailView> original, StandardLevelDetailView instance)
        {
            original(instance);

            var beatmapLevel = _standardLevelDetailViewController.beatmapLevel;
            var beatmapKey = SongCoreNative.SelectedKey(instance);
#if BEAT_SABER_1_29_0
            if (beatmapLevel == null || !SongCoreNative.KeyIsValid(beatmapKey)) return;
#endif
            var actionButton = instance.actionButton;
            var practiceButton = instance.practiceButton;

            actionButton.interactable = true;
            practiceButton.interactable = true;

            _requirementsUI.ButtonGlowColor = false;
            _requirementsUI.ButtonInteractable = false;

            if (_songData == null)
            {
                return;
            }

            var wipFolderSong = false;
            var difficultyData = Collections.GetCustomLevelSongDifficultyData(beatmapKey);
            if (difficultyData != null)
            {
                //If no additional information is present
                if (!difficultyData.additionalDifficultyData._requirements.Any() &&
                    !difficultyData.additionalDifficultyData._suggestions.Any() &&
                    !difficultyData.additionalDifficultyData._warnings.Any() &&
                    !difficultyData.additionalDifficultyData._information.Any() &&
                    !_songData!.contributors.Any() && !Utils.DiffHasColors(difficultyData))
                {
                    _requirementsUI.ButtonGlowColor = false;
                    _requirementsUI.ButtonInteractable = false;
                }
                else if (!difficultyData.additionalDifficultyData._warnings.Any())
                {
                    _requirementsUI.ButtonGlowColor = true;
                    _requirementsUI.ButtonInteractable = true;
                    _requirementsUI.SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
                else if (difficultyData.additionalDifficultyData._warnings.Any())
                {
                    _requirementsUI.ButtonGlowColor = true;
                    _requirementsUI.ButtonInteractable = true;
                    if (difficultyData.additionalDifficultyData._warnings.Contains("WIP"))
                    {
                        actionButton.interactable = false;
                    }

                    _requirementsUI.SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
            }

            if (beatmapLevel.levelID.EndsWith(" WIP", StringComparison.Ordinal))
            {
                _requirementsUI.ButtonGlowColor = true;
                _requirementsUI.ButtonInteractable = true;
                actionButton.interactable = false;
                wipFolderSong = true;

                if (difficultyData != null)
                {
                    _requirementsUI.SetRainbowColors(Utils.DiffHasColors(difficultyData));
                }
            }

            if (difficultyData != null)
            {
                foreach (var requirement in difficultyData.additionalDifficultyData._requirements)
                {
                    if (!Collections.capabilities.Contains(requirement))
                    {
                        actionButton.interactable = false;
                        practiceButton.interactable = false;
                        _requirementsUI.ButtonGlowColor = true;
                        _requirementsUI.ButtonInteractable = true;
                    }
                }
            }

            if (Legato.BeatmapKeyExtensions.CharacteristicSerializedName(beatmapKey) == "MissingCharacteristic")
            {
                actionButton.interactable = false;
                practiceButton.interactable = false;
                _requirementsUI.ButtonGlowColor = true;
                _requirementsUI.ButtonInteractable = true;
            }

            _requirementsUI.beatmapLevel = beatmapLevel;
            _requirementsUI.beatmapKey = beatmapKey;
            _requirementsUI.songData = _songData;
            _requirementsUI.diffData = difficultyData;
            _requirementsUI.wipFolder = wipFolderSong;
        }

        private void SaveAndRestoreButtonsState(Action<StandardLevelDetailView> original, StandardLevelDetailView instance)
        {
            var actionButtonInteractable = instance.actionButton.interactable;
            var practiceButtonInteractable = instance.practiceButton.interactable;
            original(instance);
            instance.actionButton.interactable = actionButtonInteractable;
            instance.practiceButton.interactable = practiceButtonInteractable;
        }

#if LEGATO_SPLIT_SCENE_COLOR_SETUP
        private void OverrideSoloColorScheme(
#if LEGATO_SINGLE_COLOR_OVERRIDE
            Action<StandardLevelScenesTransitionSetupData, ColorScheme, ColorScheme> original,
            StandardLevelScenesTransitionSetupData instance, ColorScheme playerOverride, ColorScheme beatmapOverride)
        {
            original(instance, playerOverride, beatmapOverride);
#else
            Action<StandardLevelScenesTransitionSetupData, ColorScheme, bool, ColorScheme> original,
            StandardLevelScenesTransitionSetupData instance, ColorScheme playerOverride, bool overrideLightshow, ColorScheme beatmapOverride)
        {
            original(instance, playerOverride, overrideLightshow, beatmapOverride);
#endif
            instance.colorScheme = SetOverrideColorScheme(instance.colorScheme, instance, instance.beatmapKey);
        }

        private void OverrideMultiplayerColorScheme(Action<MultiplayerLevelScenesTransitionSetupData, ColorScheme> original,
            MultiplayerLevelScenesTransitionSetupData instance, ColorScheme playerOverride)
        {
            original(instance, playerOverride);
            instance.colorScheme = SetOverrideColorScheme(instance.colorScheme, instance, instance.beatmapKey);
        }
#else
        private void OverrideColorSchemeManipulator(ILContext context)
        {
            var cursor = new ILCursor(context);
            cursor.GotoNext(MoveType.Before, i => i.MatchCall(out var method) && method.Name == "set_colorScheme");
            cursor.Emit(OpCodes.Ldarg_0);
#if BEAT_SABER_1_29_0
            if (context.Method.DeclaringType.Name.StartsWith("Multiplayer", StringComparison.Ordinal)) cursor.Emit(OpCodes.Ldarg_S, context.Method.Parameters[4]);
            else cursor.Emit(OpCodes.Ldarg_2);
#else
            cursor.Emit(OpCodes.Ldarg_2);
#endif
#if BEAT_SABER_1_29_0
            cursor.EmitDelegate<Func<ColorScheme, LevelScenesTransitionSetupData, IDifficultyBeatmap, ColorScheme>>((color, transition, key) => SetOverrideColorScheme(color, transition, in key));
#else
            cursor.EmitDelegate(SetOverrideColorScheme);
#endif
        }
#endif

        private ColorScheme SetOverrideColorScheme(ColorScheme colorScheme, LevelScenesTransitionSetupData levelScenesTransitionSetupData, in BeatmapKey beatmapKey)
        {
#if BEAT_SABER_1_29_0
            var nativeColors = V4NativeColors.Apply(colorScheme, beatmapKey);
            var overrideColorScheme = GetOverrideColorScheme(nativeColors ?? colorScheme, beatmapKey) ?? nativeColors;
#else
            var overrideColorScheme = GetOverrideColorScheme(colorScheme, beatmapKey);
#endif

            if (overrideColorScheme == null)
            {
                return colorScheme;
            }

            if (levelScenesTransitionSetupData is StandardLevelScenesTransitionSetupData standardLevelScenesTransitionSetupData)
            {
                standardLevelScenesTransitionSetupData.usingOverrideColorScheme = true;
            }
            else if (levelScenesTransitionSetupData is MultiplayerLevelScenesTransitionSetupData multiplayerLevelScenesTransitionSetupData)
            {
                multiplayerLevelScenesTransitionSetupData.usingOverrideColorScheme = true;
            }

            return overrideColorScheme;
        }

        private ColorScheme? GetOverrideColorScheme(ColorScheme currentColorScheme, BeatmapKey beatmapKey)
        {
            if (_config is { CustomSongNoteColors: false, CustomSongEnvironmentColors: false, CustomSongObstacleColors: false })
            {
                return null;
            }

            var songDifficultyData = Collections.GetCustomLevelSongDifficultyData(beatmapKey);

            if (songDifficultyData is null || (songDifficultyData._colorLeft == null && songDifficultyData._colorRight == null && songDifficultyData._envColorLeft == null && songDifficultyData._envColorRight == null &&
                                               songDifficultyData._envColorWhite == null && songDifficultyData._obstacleColor == null && songDifficultyData._envColorLeftBoost == null && songDifficultyData._envColorRightBoost == null &&
                                               songDifficultyData._envColorWhiteBoost == null))
            {
                return null;
            }

            if (_config.CustomSongNoteColors)
            {
                Plugin.Log.Debug("Custom song note colors On");
            }

            if (_config.CustomSongEnvironmentColors)
            {
                Plugin.Log.Debug("Custom song environment colors On");
            }

            if (_config.CustomSongObstacleColors)
            {
                Plugin.Log.Debug("Custom song obstacle colors On");
            }

            var saberLeft = songDifficultyData._colorLeft == null || !_config.CustomSongNoteColors
                ? currentColorScheme.saberAColor
                : Utils.ColorFromMapColor(songDifficultyData._colorLeft);
            var saberRight = songDifficultyData._colorRight == null || !_config.CustomSongNoteColors
                ? currentColorScheme.saberBColor
                : Utils.ColorFromMapColor(songDifficultyData._colorRight);
            var envLeft = songDifficultyData._envColorLeft == null || !_config.CustomSongEnvironmentColors
                ? songDifficultyData._colorLeft == null ? currentColorScheme.environmentColor0 : Utils.ColorFromMapColor(songDifficultyData._colorLeft)
                : Utils.ColorFromMapColor(songDifficultyData._envColorLeft);
            var envRight = songDifficultyData._envColorRight == null || !_config.CustomSongEnvironmentColors
                ? songDifficultyData._colorRight == null ? currentColorScheme.environmentColor1 : Utils.ColorFromMapColor(songDifficultyData._colorRight)
                : Utils.ColorFromMapColor(songDifficultyData._envColorRight);
            var envWhite = songDifficultyData._envColorWhite == null || !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColorW
                : Utils.ColorFromMapColor(songDifficultyData._envColorWhite);
            var envLeftBoost = songDifficultyData._envColorLeftBoost == null || !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColor0Boost
                : Utils.ColorFromMapColor(songDifficultyData._envColorLeftBoost);
            var envRightBoost = songDifficultyData._envColorRightBoost == null|| !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColor1Boost
                : Utils.ColorFromMapColor(songDifficultyData._envColorRightBoost);
            var envWhiteBoost = songDifficultyData._envColorWhiteBoost == null  || !_config.CustomSongEnvironmentColors
                ? currentColorScheme.environmentColorWBoost
                : Utils.ColorFromMapColor(songDifficultyData._envColorWhiteBoost);
            var obstacle = songDifficultyData._obstacleColor == null || !_config.CustomSongObstacleColors
                ? currentColorScheme.obstaclesColor
                : Utils.ColorFromMapColor(songDifficultyData._obstacleColor);

#if BEAT_SABER_1_29_0
            var scheme = new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false,
                saberLeft, saberRight, envLeft, envRight, envLeftBoost != default && envRightBoost != default, envLeftBoost, envRightBoost, obstacle);
            scheme.SetField("_environmentColorW", envWhite);
            scheme.SetField("_environmentColorWBoost", envWhiteBoost);
            return scheme;
#elif LEGATO_SINGLE_COLOR_OVERRIDE
            return new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false,
                saberLeft, saberRight, envLeft, envRight, envWhite, envLeftBoost != default && envRightBoost != default, envLeftBoost, envRightBoost, envWhiteBoost, obstacle);
#else
            return new ColorScheme("SongCoreMapColorScheme", "SongCore Map Color Scheme", true, "SongCore Map Color Scheme", false, true, saberLeft, saberRight, true,
                envLeft, envRight, envWhite, envLeftBoost != default && envRightBoost != default, envLeftBoost, envRightBoost, envWhiteBoost, obstacle);
#endif
        }
    }
}
