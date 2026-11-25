#if LEGATO_BEFORE_MULTIPLAYER
using System;
using System.Linq;
using MonoMod.RuntimeDetour;
using SongCore.UI;
using SongCore.Utilities;
using Zenject;

namespace SongCore.Hooks
{
    internal sealed class SongDataMenuHooks : IInitializable, IDisposable
    {
        private readonly RequirementsUI _requirements;
        private Hook? _refreshContent;

        private SongDataMenuHooks(RequirementsUI requirements) => _requirements = requirements;

        public void Initialize() =>
            _refreshContent = new Hook(typeof(StandardLevelDetailView).GetMethod(nameof(StandardLevelDetailView.RefreshContent))!, RefreshContent, true);

        public void Dispose() => _refreshContent?.Dispose();

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
