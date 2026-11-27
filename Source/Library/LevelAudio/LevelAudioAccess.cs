using System;
using System.IO;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER || LEGATO_EARLY_STANDARD_MAPS
using UnityEngine.Networking;
#endif
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;
using UnityEngine;

namespace Legato.Audio {
    internal sealed class LevelAudioData {
        internal string FilePath;
        internal AudioClip AssetClip;
        internal bool IsAsset;
        internal string ProviderName;
    }
#if LEGATO_EARLY_STANDARD_MAPS
    internal static class LevelAudioAccess {
        internal static Task<LevelAudioData> Load(string customLevelFolder, BeatmapLevel level, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            if (level == null) return Task.FromResult<LevelAudioData>(null);
            if (!string.IsNullOrEmpty(customLevelFolder)) {
                var media = Legato.Beatmaps.CustomLevelMedia.ReadLevel(customLevelFolder);
                return Task.FromResult(media == null ? null
                    : new LevelAudioData { FilePath = media.Value.AudioPath, ProviderName = "CustomLevelFolder" });
            }
            return Task.FromResult(new LevelAudioData { IsAsset = true, AssetClip = level.audioClip, ProviderName = level.GetType().Name });
        }
        internal static Task<string> BundlePath(BeatmapLevel level, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(string.Empty);
        }
        internal static Task<AudioClip> BundleClip(AssetBundle bundle, string name, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<AudioClip>(null);
        }
        internal static async Task<AudioClip> LoadDecodedFile(string path) {
            var audioType = string.Equals(Path.GetExtension(path), ".wav", StringComparison.OrdinalIgnoreCase)
                ? AudioType.WAV : AudioType.OGGVORBIS;
            using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.GetFullPath(path)).AbsoluteUri, audioType);
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            if (request.isNetworkError || request.isHttpError) throw new IOException(request.error);
            return DownloadHandlerAudioClip.GetContent(request);
        }
        internal static bool IsPlaying(AudioTimeSyncController controller) {
            var source = controller.GetField<AudioSource, AudioTimeSyncController>("_audioSource");
            return source != null && source.isPlaying;
        }
        internal static BeatmapLevel Selected(StandardLevelDetailViewController controller) => controller?.difficultyLevel?.level;
    }
#else
    internal static class LevelAudioAccess {
        internal static async Task<LevelAudioData> Load(BeatmapLevelsModel model, BeatmapLevel level, CancellationToken token) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            var result = await model.GetBeatmapLevelAsync(level.levelID, token);
            token.ThrowIfCancellationRequested();
            if (result.isError || result.beatmapLevel == null) return null;
            if (Legato.Beatmaps.CustomLevelMedia.TryGetPaths(result.beatmapLevel, out var path, out _))
                return new LevelAudioData { FilePath = path, ProviderName = result.beatmapLevel.GetType().Name };
            return new LevelAudioData { IsAsset = true, AssetClip = result.beatmapLevel.beatmapLevelData.audioClip,
                ProviderName = result.beatmapLevel.GetType().Name };
#else
            var result = await model.LoadBeatmapLevelDataAsync(level.levelID, BeatmapLevelDataVersion.Original, token);
            token.ThrowIfCancellationRequested();
            if (result.isError || result.beatmapLevelData == null) return null;
            var data = result.beatmapLevelData;
            if (data is IFilePathSongAudioClipProvider file) return new LevelAudioData { FilePath = file.songAudioClipPath, ProviderName = data.GetType().Name };
            if (data is IAssetSongAudioClipProvider asset) return new LevelAudioData { IsAsset = true, AssetClip = asset.songAudioClip, ProviderName = data.GetType().Name };
            return new LevelAudioData { ProviderName = data.GetType().Name };
#endif
        }
        internal static async Task<string> BundlePath(BeatmapLevelsModel model, BeatmapLevel level, CancellationToken token) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            var provider = FieldAccessor<BeatmapLevelsModel, IBeatmapDataAssetFileModel>.Get(model, "_beatmapDataAssetFileModel");
            var result = await provider.GetAssetBundleFileForPreviewLevelAsync(level, token);
            token.ThrowIfCancellationRequested();
            return result.assetBundlePath;
#else
            token.ThrowIfCancellationRequested();
            var name = BeatmapDataAssetsModel.GetAssetBundleNameForBeatmapLevel(level.levelID, BeatmapLevelDataVersion.Original);
            var path = BeatmapDataAssetsModel.GetAssetBundleStreamingAssetsPathForBeatmapLevelId(level.levelID, BeatmapLevelDataVersion.Original);
            if (!File.Exists(path)) path = Path.Combine(IPA.Utilities.UnityGame.InstallPath, "DLC", "Levels", level.levelID, name);
            await Task.CompletedTask;
            return path;
#endif
        }
        internal static async Task<AudioClip> BundleClip(AssetBundle bundle, string name, CancellationToken token) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            var request = bundle.LoadAllAssetsAsync<BeatmapLevelSO>();
#else
            var request = bundle.LoadAllAssetsAsync<BeatmapLevelDataSO>();
#endif
            // Unity requests finish acquisition before the caller releases its owned bundle.
            while (!request.isDone) await Task.Yield();
            token.ThrowIfCancellationRequested();
#if LEGATO_BEFORE_MULTIPLAYER
            return request.allAssets.OfType<BeatmapLevelSO>().Select(level => level.previewAudioClip)
#elif BEAT_SABER_1_29_0
            return request.allAssets.OfType<BeatmapLevelSO>().Select(level => level.songAudioClip)
#else
            return request.allAssets.OfType<BeatmapLevelDataSO>().Select(level => level.songAudioClip)
#endif
                .FirstOrDefault(clip => clip != null && clip.name == name);
        }
        internal static async Task<AudioClip> LoadDecodedFile(string path) {
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
#if LEGATO_BEFORE_MULTIPLAYER
            var audioType = string.Equals(Path.GetExtension(path), ".wav", StringComparison.OrdinalIgnoreCase)
                ? AudioType.WAV : AudioType.OGGVORBIS;
#else
            var audioType = AudioTypeHelper.GetAudioTypeFromPath(path);
#endif
            using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.GetFullPath(path)).AbsoluteUri, audioType);
            ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = false;
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            if (request.isNetworkError || request.isHttpError) throw new IOException(request.error);
            return DownloadHandlerAudioClip.GetContent(request);
#else
            return await MediaAsyncLoader.LoadAudioClipAsync(path, false);
#endif
        }
        internal static bool IsPlaying(AudioTimeSyncController controller) =>
#if LEGATO_SYNC_CONTROLLER_AUDIO_STATE
            controller.state == AudioTimeSyncController.State.Playing;
#else
            controller.state == IAudioTimeSource.State.Playing;
#endif
        internal static BeatmapLevel Selected(LevelSelectionNavigationController controller) =>
#if LEGATO_BEFORE_MULTIPLAYER
            controller.selectedDifficultyBeatmap?.level;
#elif BEAT_SABER_1_29_0
            controller.selectedBeatmapLevel;
#else
            controller.beatmapLevel;
#endif
    }
#endif
}
