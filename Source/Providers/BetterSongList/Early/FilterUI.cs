using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BetterSongList.FilterModels;
using BetterSongList.Interfaces;
using BetterSongList.SortModels;
using BetterSongList.Util;
using HarmonyLib;
using Legato.SongBrowser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VRUI;

namespace BetterSongList.UI
{
    [HarmonyPatch]
    internal static class EarlySongListHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StandardLevelSelectionFlowCoordinator), nameof(StandardLevelSelectionFlowCoordinator.Present))]
        private static void SongMode(GameplayMode __2) => FilterUI.Mode = __2;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StandardLevelListViewController), nameof(StandardLevelListViewController.Init))]
        private static void SetSongs(StandardLevelListViewController __instance, IStandardLevel[] __0) =>
            FilterUI.For(__instance).SetSongs(__0);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StandardLevelListViewController), "DidActivate")]
        private static void Activate(StandardLevelListViewController __instance, VRUIViewController.ActivationType __1)
        {
            if (__1 == VRUIViewController.ActivationType.AddedToHierarchy)
                __instance.GetComponent<FilterUI>()?.Open();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StandardLevelListViewController), "HandleLevelSelectionDidChange")]
        private static void SaveSelection(StandardLevelListViewController __instance, bool __1)
        {
            if (__1 && __instance.selectedLevel != null)
            {
                Config.Instance.LastSong = __instance.selectedLevel.levelID;
                Config.Instance.LastCategory = FilterUI.Mode.ToString();
                Config.Instance.Changed();
            }
        }
    }

    internal sealed class FilterUI : MonoBehaviour, INotifyPropertyChanged
    {
        private const float ControlsHeight = 26f;
        private const float PageButtonClearance = 6f;
        private readonly Dictionary<string, ISorter> _sorters = new();
        private readonly Dictionary<string, IFilter> _filters = new();
        private readonly List<object> _sortOptions = new();
        private readonly List<object> _filterOptions = new();
        private readonly List<object> _legendOptions = new() { "Choose a group" };
        private readonly System.Random _random = new();
        private StandardLevelListViewController _view;
        private RectTransform _controls;
        private IStandardLevel[] _input = Array.Empty<IStandardLevel>();
        private IStandardLevel[] _output = Array.Empty<IStandardLevel>();
        private CancellationTokenSource _refresh;
        private bool _restoreSelection;
        private string _query = string.Empty;
        private string _status = string.Empty;

        [UIComponent("sort-dropdown")]
        private DropDownListSetting _sortDropdown = null;

        [UIComponent("filter-dropdown")]
        private DropDownListSetting _filterDropdown = null;

        [UIComponent("legend-dropdown")]
        private DropDownListSetting _legendDropdown = null;

        [UIObject("scroll-buttons")]
        private GameObject _scrollButtons = null;

        internal static bool initialized { get; private set; }
        internal static GameplayMode Mode { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        internal static FilterUI For(StandardLevelListViewController view)
        {
            var control = view.GetComponent<FilterUI>() ?? view.gameObject.AddComponent<FilterUI>();
            control._view = view;
            return control;
        }

        internal void SetSongs(IStandardLevel[] levels)
        {
            _refresh?.Cancel();
            _input = levels ?? Array.Empty<IStandardLevel>();
            _output = _input;
            ShowLegend(null);
            _restoreSelection = true;
        }

        internal void Open()
        {
            var category = _input.Any(IsCustom) ? LevelCategory.CustomSongs : LevelCategory.OstAndExtras;
            _sorters.Clear();
            foreach (var option in SortMethods.methods)
                if (IsAvailable(option.Key, option.Value, category)) _sorters.Add(option.Key, option.Value);
            _filters.Clear();
            foreach (var option in FilterMethods.methods)
                if (IsAvailable(option.Key, option.Value, category)) _filters.Add(option.Key, option.Value);

            _sortOptions.Clear();
            _sortOptions.AddRange(_sorters.Keys);
            _filterOptions.Clear();
            _filterOptions.AddRange(_filters.Keys);
            _sortDropdown?.UpdateChoices();
            _sortDropdown?.ReceiveValue();
            _filterDropdown?.UpdateChoices();
            _filterDropdown?.ReceiveValue();
            Refresh();
        }

        private static bool IsCustom(IStandardLevel level) => !string.IsNullOrEmpty(level?.levelID) && Path.IsPathRooted(level.levelID);

        private static bool IsAvailable(string name, object option, LevelCategory category)
        {
            try
            {
                if (option is ITransformerPlugin transformer)
                {
                    transformer.ContextSwitch(category, null);
                    if (!transformer.visible) return false;
                }
                return option is not IAvailabilityCheck availability || availability.GetUnavailabilityReason() == null;
            }
            catch (Exception error)
            {
                Plugin.Log.Warn($"Ignoring unavailable song list option '{name}': {error}");
                return false;
            }
        }

        private void CreateControls()
        {
            if (_controls != null)
                return;

            RectTransform container = EarlyStandardSongList.Container(_view);
            Vector2 originalOffset = container.offsetMax;
            container.offsetMax = new Vector2(originalOffset.x, originalOffset.y - ControlsHeight - PageButtonClearance);
            GameObject controls = new("BetterSongList Controls", typeof(RectTransform));
            controls.layer = _view.gameObject.layer;
            _controls = (RectTransform)controls.transform;
            _controls.SetParent(container, false);
            _controls.anchorMin = new Vector2(0, 1);
            _controls.anchorMax = new Vector2(1, 1);
            _controls.pivot = new Vector2(0.5f, 0);
            _controls.sizeDelta = new Vector2(0, ControlsHeight);
            _controls.anchoredPosition = new Vector2(0, PageButtonClearance);
            try
            {
                BSMLParser.Instance.Parse(Utilities.GetResourceContent(GetType().Assembly,
                    "BetterSongList.UI.BSML.EarlyBrowser.bsml"), controls, this);
                _scrollButtons.SetActive(Config.Instance.ExtendSongsScrollbar);
                ShowLegend(null);
                initialized = true;
            }
            catch
            {
                controls.SetActive(false);
                Destroy(controls);
                _controls = null;
                container.offsetMax = originalOffset;
                throw;
            }
        }

        [UIValue("sort-options")]
        private List<object> Sorts => _sortOptions;

        [UIValue("filter-options")]
        private List<object> Filters => _filterOptions;

        [UIValue("legend")]
        private List<object> Legend => _legendOptions;

        private void ShowLegend(ISorterWithLegend sorter)
        {
            if (_legendDropdown == null) return;
            _legendOptions.Clear();
            _legendOptions.Add("Choose a group");
            if (Config.Instance.EnableAlphabetScrollbar && sorter != null)
                foreach (var entry in sorter.BuildLegend(_output) ?? Enumerable.Empty<KeyValuePair<string, int>>())
                    _legendOptions.Add(new JumpGroup(entry.Key, entry.Value));

            _legendDropdown.gameObject.SetActive(_legendOptions.Count > 2);
            if (_legendDropdown.gameObject.activeSelf)
            {
                _legendDropdown.UpdateChoices();
                _legendDropdown.Value = _legendOptions[0];
            }
        }

        [UIAction("jump-to-group")]
        private void JumpToGroup(object choice)
        {
            if (choice is not JumpGroup group) return;
            EarlyStandardSongList.ScrollTo(_view, group.Row);
            _legendDropdown.Value = _legendOptions[0];
        }

        private sealed class JumpGroup
        {
            private readonly string _label;
            internal int Row { get; }

            internal JumpGroup(string label, int row)
            {
                _label = label;
                Row = row;
            }

            public override string ToString() => _label;
        }

        [UIValue("sort")]
        private string Sort
        {
            get => _sorters.ContainsKey(Config.Instance.LastSort ?? "") ? Config.Instance.LastSort : "Default";
            set
            {
                if (Sort == value) return;
                Config.Instance.LastSort = value;
                Config.Instance.Changed();
                Refresh();
            }
        }

        [UIValue("filter")]
        private string Filter
        {
            get => _filters.ContainsKey(Config.Instance.LastFilter ?? "") ? Config.Instance.LastFilter : "All";
            set
            {
                if (Filter == value) return;
                Config.Instance.LastFilter = value;
                Config.Instance.Changed();
                Refresh();
            }
        }

        [UIValue("direction")]
        private string Direction => Config.Instance.SortAsc ? "▲" : "▼";

        [UIValue("query")]
        private string Query
        {
            get => _query;
            set
            {
                value = value?.Trim() ?? string.Empty;
                if (_query == value) return;
                _query = value;
                Refresh();
            }
        }

        [UIValue("status")]
        private string Status => _status;

        [UIAction("reverse-order")]
        private void ReverseOrder()
        {
            Config.Instance.SortAsc = !Config.Instance.SortAsc;
            Config.Instance.Changed();
            Notify(nameof(Direction));
            Refresh();
        }

        [UIAction("random-song")]
        private void SelectRandom()
        {
            if (_output.Length > 0)
                EarlyStandardSongList.TrySelect(_view, _random.Next(_output.Length));
        }

        [UIAction("top")]
        private void ScrollTop() => EarlyStandardSongList.ScrollTo(_view, 0);

        [UIAction("bottom")]
        private void ScrollBottom() => EarlyStandardSongList.ScrollTo(_view, _output.Length - 1);

        [UIValue("mapper-search")]
        private bool MapperSearch
        {
            get => Config.Instance.ModBasegameSearch;
            set => Config.Instance.ModBasegameSearch = value;
        }

        [UIValue("scroll-jumps")]
        private bool ScrollJumps
        {
            get => Config.Instance.ExtendSongsScrollbar;
            set => Config.Instance.ExtendSongsScrollbar = value;
        }

        [UIValue("group-jumps")]
        private bool GroupJumps
        {
            get => Config.Instance.EnableAlphabetScrollbar;
            set => Config.Instance.EnableAlphabetScrollbar = value;
        }

        [UIValue("leaderboards")]
        private List<object> Leaderboards { get; } = new() { "ScoreSaber", "BeatLeader" };

        [UIValue("leaderboard")]
        private string Leaderboard
        {
            get => Config.Instance.PreferredLeaderboard;
            set => Config.Instance.PreferredLeaderboard = value;
        }

        [UIValue("chart-metrics")]
        private List<object> ChartMetrics { get; } = new() { "Reaction Time", "Jump Distance", "Map Age" };

        [UIValue("chart-metric")]
        private string ChartMetric
        {
            get => Config.Instance.PreferredMiscSetting;
            set => Config.Instance.PreferredMiscSetting = value;
        }

        [UIValue("accuracy-scale")]
        private float AccuracyScale
        {
            get => Config.Instance.AccuracyMultiplier;
            set => Config.Instance.AccuracyMultiplier = value;
        }

        [UIAction("save-settings")]
        private void SaveSettings()
        {
            Config.Instance.Changed();
            _scrollButtons.SetActive(Config.Instance.ExtendSongsScrollbar);
            Refresh();
        }

        private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void SetStatus(string text)
        {
            _status = text;
            Notify(nameof(Status));
        }

        private async void Refresh()
        {
            if (_view == null) return;
            _refresh?.Cancel();
            var request = _refresh = new CancellationTokenSource();
            CancellationToken token = request.Token;
            try
            {
                // Let VRUI finish its native hierarchy rebuild before touching selection or layout.
                await Task.Yield();
                token.ThrowIfCancellationRequested();
                if (_view == null || !_view.isInViewControllerHierarchy) return;
                CreateControls();

                _sorters.TryGetValue(Sort, out var sorter);
                _filters.TryGetValue(Filter, out var filter);
                if (sorter?.isReady == false || filter?.isReady == false)
                    SetStatus("Loading song data");
                var sortPreparation = sorter is FolderDateSorter folders ? folders.PrepareFor(_input, token)
                    : sorter?.isReady == false ? sorter.Prepare(token) : Task.CompletedTask;
                var preparation = Task.WhenAll(sortPreparation,
                    filter is PlayedFilter || filter?.isReady == false ? filter.Prepare(token) : Task.CompletedTask);
                await AwaitPreparation(preparation, token);
                token.ThrowIfCancellationRequested();
                if (_view == null || !_view.isInViewControllerHierarchy) return;

                string[] terms = _query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                IEnumerable<IStandardLevel> levels = _input.Where(level => level != null && Matches(level, terms));
                if (filter?.isReady == true)
                    levels = levels.Where(filter.GetValueFor);
                if (sorter?.isReady == true)
                {
                    bool ascending = Config.Instance.SortAsc;
                    if (sorter is ISorterCustom custom)
                        custom.DoSort(ref levels, ascending);
                    else if (sorter is ISorterPrimitive primitive)
                        levels = ascending ? levels.OrderBy(level => primitive.GetValueFor(level) ?? float.MaxValue)
                            : levels.OrderByDescending(level => primitive.GetValueFor(level) ?? float.MinValue);
                }

                var visible = levels.ToArray();
                if (!EarlyStandardSongList.Replace(_view, visible)) return;
                _output = visible;
                ShowLegend(sorter?.isReady == true ? sorter as ISorterWithLegend : null);
                SetStatus(visible.Length == 0 ? "No matching songs" : string.Empty);
                if (_restoreSelection && _view.selectedLevel == null && Config.Instance.LastCategory == Mode.ToString())
                {
                    int index = Array.FindIndex(visible, song => song.levelID == Config.Instance.LastSong);
                    if (index >= 0) EarlyStandardSongList.TrySelect(_view, index);
                }
                _restoreSelection = false;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                SetStatus("Song list refresh failed");
                Plugin.Log.Warn($"Refreshing the native song list failed: {error}");
            }
            finally
            {
                if (_refresh == request) _refresh = null;
                request.Dispose();
            }
        }

        private static bool Matches(IStandardLevel level, string[] terms)
        {
            if (terms.Length == 0) return true;
            string name = level.songName + " " + level.songSubName + " " + level.songAuthorName;
            if (Config.Instance.ModBasegameSearch) name += " " + BeatmapsUtil.ConcatMappers(level);
            return terms.All(term => name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static async Task AwaitPreparation(Task preparation, CancellationToken token)
        {
            var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (token.Register(() => canceled.TrySetResult(true)))
            {
                if (await Task.WhenAny(preparation, canceled.Task) != preparation)
                {
                    _ = preparation.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    token.ThrowIfCancellationRequested();
                }
                await preparation;
            }
        }

        private void OnDisable() => _refresh?.Cancel();
    }
}
