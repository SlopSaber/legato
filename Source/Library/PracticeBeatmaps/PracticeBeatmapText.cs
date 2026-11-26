#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
#if LEGATO_BEFORE_MULTIPLAYER
using System.Runtime.CompilerServices;
#endif
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using Legato.Beatmaps.Formats;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace Legato.Beatmaps {
    internal sealed class PracticeBeatmapText {
        internal BeatmapLevel Level { get; }
        internal Func<float, float>? OfficialBeatToSeconds { get; private set; }
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private readonly string _characteristic;
        private readonly BeatmapDifficulty _difficulty;
#if LEGATO_BEFORE_MULTIPLAYER
        private static readonly ConditionalWeakTable<PracticeViewController, IDifficultyBeatmap> Selections = new();

        internal static void SetSelectedDifficulty(PracticeViewController view, IDifficultyBeatmap? difficulty) {
            Selections.Remove(view);
            if (difficulty != null) Selections.Add(view, difficulty);
        }

        private PracticeBeatmapText(BeatmapLevel level, IDifficultyBeatmap selected) {
            Level = level;
            _characteristic = selected.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            _difficulty = selected.difficulty;
        }
#else
        private PracticeBeatmapText(PracticeViewController view, BeatmapLevel level) {
            Level = level;
            _characteristic = FieldAccessor<PracticeViewController, BeatmapCharacteristicSO>.Get(view, "_beatmapCharacteristic").serializedName;
            _difficulty = FieldAccessor<PracticeViewController, BeatmapDifficulty>.Get(view, "_beatmapDifficulty");
        }
#endif
#else
        private readonly BeatmapKey _key;
        private readonly BeatmapLevelsModel _levels;
        private readonly BeatmapLevelsEntitlementModel _entitlements;
        private PracticeBeatmapText(PracticeViewController view, BeatmapLevel level) {
            Level = level;
            _key = FieldAccessor<PracticeViewController, BeatmapKey>.Get(view, "_beatmapKey");
            _levels = FieldAccessor<PracticeViewController, BeatmapLevelsModel>.Get(view, "_beatmapLevelsModel");
            _entitlements = FieldAccessor<PracticeViewController, BeatmapLevelsEntitlementModel>.Get(view, "_beatmapLevelsEntitlementModel");
        }
#endif
        internal static BeatmapLevel? LevelFor(PracticeViewController view) =>
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            FieldAccessor<PracticeViewController, IBeatmapLevel>.Get(view, "_level");
#else
            FieldAccessor<PracticeViewController, BeatmapLevel>.Get(view, "_beatmapLevel");
#endif
        internal static PracticeBeatmapText? ForView(PracticeViewController view) {
            BeatmapLevel? level = LevelFor(view);
#if LEGATO_BEFORE_MULTIPLAYER
            if (level == null || !Selections.TryGetValue(view, out var selected) || selected.level.levelID != level.levelID)
                return null;
            return new PracticeBeatmapText(level, selected);
#else
            return level == null ? null : new PracticeBeatmapText(view, level);
#endif
        }
        internal async Task<string?> Load(CancellationToken token) {
            token.ThrowIfCancellationRequested();
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            string? text = await Task.Run(() => ReadLegacy(token), token);
#else
            var version = await BeatmapTextData.GetVersion(_entitlements, _key.levelId, token);
            token.ThrowIfCancellationRequested();
            var result = await BeatmapTextData.Load(_levels, _key.levelId, version, token);
            token.ThrowIfCancellationRequested();
            if (!BeatmapTextData.HasData(result)) throw new InvalidDataException(BeatmapTextData.Error(result));
            string? text = await BeatmapTextData.ReadText(result, in _key);
#endif
            token.ThrowIfCancellationRequested();
            return text;
        }
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private string? ReadLegacy(CancellationToken token) {
            if (Level is not CustomBeatmapLevel custom) return null;
            string directory = custom.customLevelPath;
            var info = V4Documents.Read(Path.Combine(directory, "Info.dat"));
            token.ThrowIfCancellationRequested();
            string? version = info.Value<string>("version");
            if (Version.TryParse(version, out Version? parsed) && parsed.Major == 4) {
                var level = new V4LevelInfo(info);
                var difficulty = level.Difficulties.Single(value => value.Characteristic == _characteristic && value.Name == _difficulty.ToString());
                JObject gameplay = V4Documents.Read(level.ResolveAsset(directory, difficulty.BeatmapFilename));
                token.ThrowIfCancellationRequested();
                JObject lightshow = V4Documents.Read(level.ResolveAsset(directory, difficulty.LightshowFilename));
                token.ThrowIfCancellationRequested();
                var tempo = new V4TempoMap(V4Documents.Read(level.ResolveAsset(directory, level.AudioDataFilename)), level.Bpm);
                token.ThrowIfCancellationRequested();
                OfficialBeatToSeconds = beat => (float)tempo.BeatToSeconds(beat);
                return new JObject {
                    ["customData"] = V4LegacyProjection.MergeCustomData(lightshow["customData"], gameplay["customData"])
                }.ToString(Formatting.None);
            }
            var set = custom.standardLevelInfoSaveData.difficultyBeatmapSets.Single(value => value.beatmapCharacteristicName == _characteristic);
            var entry = set.difficultyBeatmaps.Single(value => value.difficulty == _difficulty.ToString());
            string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (Path.IsPathRooted(entry.beatmapFilename)) throw new InvalidDataException("A map asset must use a relative filename.");
            string path = Path.GetFullPath(Path.Combine(root, entry.beatmapFilename));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A map asset leaves its level directory.");
            return V4Documents.Read(path).ToString(Formatting.None);
        }
#endif
    }
}
