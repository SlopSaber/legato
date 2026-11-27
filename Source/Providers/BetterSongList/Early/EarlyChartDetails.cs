using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using BetterSongList.SortModels;
using BetterSongList.Util;
using HarmonyLib;
using SongCore;
using SongDetailsCache.Structs;
using UnityEngine;

namespace BetterSongList.UI
{
    [HarmonyPatch(typeof(StandardLevelDetailViewController), nameof(StandardLevelDetailViewController.RefreshContent))]
    internal static class EarlyChartDetailsPatch
    {
        private static void Postfix(StandardLevelDetailViewController __instance)
        {
            try
            {
                var details = __instance.GetComponent<EarlyChartDetails>() ?? __instance.gameObject.AddComponent<EarlyChartDetails>();
                details.Attach(__instance);
            }
            catch (Exception error)
            {
                Plugin.Log.Warn($"Could not attach chart controls: {error}");
            }
        }
    }

    internal sealed class EarlyChartDetails : MonoBehaviour, INotifyPropertyChanged
    {
        private StandardLevelDetailViewController _view;
        private GameObject _controls;
        private IStandardLevel _pendingRemoval;
        private bool _removing;
        private string _performance = string.Empty;
        private string _chart = string.Empty;
        private string _metric = string.Empty;
        private string _removalMessage = string.Empty;

        [UIParams]
        private BSMLParserParams _parser = null;

        [UIObject("remove-chart")]
        private GameObject _remove = null;

        public event PropertyChangedEventHandler PropertyChanged;

        internal void Attach(StandardLevelDetailViewController view)
        {
            _view = view;
            if (_controls == null)
            {
                var controls = new GameObject("BetterSongList Chart Controls", typeof(RectTransform))
                {
                    layer = view.gameObject.layer
                };
                var rect = (RectTransform)controls.transform;
                rect.SetParent(view.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                try
                {
                    BSMLParser.Instance.Parse(Utilities.GetResourceContent(GetType().Assembly,
                        "BetterSongList.UI.BSML.EarlyChartDetails.bsml"), controls, this);
                    _controls = controls;
                }
                catch
                {
                    controls.SetActive(false);
                    Destroy(controls);
                    throw;
                }
            }

            _remove.SetActive(TryGetRemovalPath(view.difficultyLevel?.level, out _));
        }

        [UIValue("chart-name")]
        private string Chart => _chart;

        [UIValue("performance")]
        private string Performance => _performance;

        [UIValue("movement-metric")]
        private string Metric => _metric;

        [UIValue("removal-message")]
        private string RemovalMessage => _removalMessage;

        [UIAction("open-chart")]
        private async void OpenChart()
        {
            var difficulty = _view.difficultyLevel;
            if (difficulty?.level == null)
                return;

            var level = difficulty.level;
            SetText(ref _chart, $"{level.songName} · {difficulty.difficulty.Name()}", nameof(Chart));
            float speed = difficulty.noteJumpMovementSpeed;
            if (speed <= 0)
                speed = difficulty.difficulty.NoteJumpMovementSpeed();
            string metric = Config.Instance.PreferredMiscSetting;
            UpdateMovement(level.beatsPerMinute, speed, metric, null);
            string leaderboard = Config.Instance.PreferredLeaderboard;
            string hash = BeatmapsUtil.GetHashOfLevel(level);
            SetText(ref _performance, hash != null && SongDetailsUtil.isAvailable
                ? $"Loading {leaderboard} rating" : $"{leaderboard} rating unavailable", nameof(Performance));
            _parser.EmitEvent("show-chart");
            if (hash == null || !SongDetailsUtil.isAvailable)
                return;

            try
            {
                var cache = await SongDetailsUtil.TryGet();
                if (!IsCurrent(difficulty)) return;
                if (cache == null || !cache.instance.songs.FindByHash(hash, out var song))
                {
                    SetText(ref _performance, $"{leaderboard} rating unavailable", nameof(Performance));
                    return;
                }
                UpdateMovement(level.beatsPerMinute, speed, metric, (int)song.uploadTimeUnix);
                var mode = _view.gameplayMode;
                string characteristic = mode == GameplayMode.SoloOneSaber ? "OneSaber" :
                    mode == GameplayMode.SoloNoArrows ? "NoArrows" : "Standard";
                if (!Enum.TryParse(difficulty.difficulty.ToString(), true, out MapDifficulty scoreDifficulty) ||
                    !Enum.TryParse(characteristic, true, out MapCharacteristic scoreCharacteristic) ||
                    !song.GetDifficulty(out var score, scoreDifficulty, scoreCharacteristic))
                {
                    SetText(ref _performance, $"{leaderboard} rating unavailable", nameof(Performance));
                    return;
                }

                bool scoreSaber = leaderboard != "BeatLeader";
                float stars = scoreSaber ? score.stars : score.starsBeatleader;
                string result = stars > 0f ? $"{leaderboard}  {stars:0.0#} ★" : $"{leaderboard}  Unranked";
                if (scoreSaber && stars > 0f)
                {
                    float multiplier = Mathf.Max(0.01f, Config.Instance.AccuracyMultiplier);
                    float accuracy = Mathf.Clamp01(.984f - Mathf.Max(0f, (stars - 1.5f) / (11f * multiplier)) * .027f);
                    result += $"  ·  {PPUtil.PPPercentage(accuracy) * stars * 42.1f:0} PP at {accuracy:P1}";
                }
                SetText(ref _performance, result, nameof(Performance));
            }
            catch (Exception error)
            {
                if (IsCurrent(difficulty))
                    SetText(ref _performance, $"{leaderboard} rating unavailable", nameof(Performance));
                Plugin.Log.Warn($"Could not load chart rating: {error}");
            }
        }

        private bool IsCurrent(IStandardLevelDifficultyBeatmap difficulty) =>
            _view != null && _view.isInViewControllerHierarchy && ReferenceEquals(_view.difficultyLevel, difficulty);

        private void UpdateMovement(float bpm, float speed, string choice, int? uploadDate)
        {
            string extra = "Jump timing unavailable";
            if (choice == "Map Age")
                extra = "BeatSaver age  " + (uploadDate.HasValue ? FolderDateSorter.GetMapAgeMonths(uploadDate.Value) : "Unavailable");
            else if (bpm > 0f && speed > 0f && !float.IsInfinity(bpm) && !float.IsInfinity(speed))
                extra = choice == "Jump Distance" ? $"Jump distance  {JumpDistanceCalculator.GetJd(bpm, speed, 0f):0.0} m"
                    : $"Reaction time  {JumpDistanceCalculator.GetRt(bpm, speed, 0f):0} ms";
            SetText(ref _metric, $"Note jump speed  {speed:0.0#}\n{extra}", nameof(Metric));
        }

        [UIAction("request-removal")]
        private void RequestRemoval()
        {
            if (_removing) return;
            var level = _view.difficultyLevel?.level;
            if (!TryGetRemovalPath(level, out _)) return;
            _pendingRemoval = level;
            SetText(ref _removalMessage, $"Remove {level.songName} and its map folder?", nameof(RemovalMessage));
            _parser.EmitEvent("show-removal");
        }

        [UIAction("confirm-removal")]
        private async void ConfirmRemoval()
        {
            var level = _pendingRemoval;
            if (_removing || level == null || !ReferenceEquals(level, _view.difficultyLevel?.level) ||
                !TryGetRemovalPath(level, out var path))
                return;

            _pendingRemoval = null;
            _removing = true;
            try
            {
                await Task.Run(() =>
                {
                    if (!Directory.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 ||
                        (File.GetAttributes(Path.GetDirectoryName(path)) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("The map folder changed before removal.");
                    WinApi.DeleteFileOrFolder(path);
                });

                Loader.Instance?.RefreshSongs();
                if (_view == null || !_view.isInViewControllerHierarchy || !ReferenceEquals(level, _view.difficultyLevel?.level))
                    return;
                _parser.EmitEvent("hide-removal");
                (_view.navigationController as StandardLevelSelectionNavigationController)?.DismissButtonWasPressed();
            }
            catch (Exception error)
            {
                SetText(ref _removalMessage, "Could not remove the map folder.", nameof(RemovalMessage));
                Plugin.Log.Warn($"Could not remove custom map folder '{path}': {error}");
            }
            finally
            {
                _removing = false;
            }
        }

        private bool TryGetRemovalPath(IStandardLevel level, out string path)
        {
            path = null;
            return _view != null && _view.isInViewControllerHierarchy &&
                _view.navigationController is StandardLevelSelectionNavigationController &&
                level != null && Loader.Instance?.TryGetCustomLevelFolder(level.levelID, out path) == true;
        }

        private void SetText(ref string field, string text, string property)
        {
            field = text;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
