using System;
using System.IO;
#if BEAT_SABER_1_29_0
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
    internal static class LevelAudioAccess {
        internal static async Task<LevelAudioData> Load(BeatmapLevelsModel model, BeatmapLevel level, CancellationToken token) {
#if BEAT_SABER_1_29_0
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
#if BEAT_SABER_1_29_0
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
#if BEAT_SABER_1_29_0
            var request = bundle.LoadAllAssetsAsync<BeatmapLevelSO>();
#else
            var request = bundle.LoadAllAssetsAsync<BeatmapLevelDataSO>();
#endif
            // Unity requests finish acquisition before the caller releases its owned bundle.
            while (!request.isDone) await Task.Yield();
            token.ThrowIfCancellationRequested();
#if BEAT_SABER_1_29_0
            return request.allAssets.OfType<BeatmapLevelSO>().Select(level => level.songAudioClip)
#else
            return request.allAssets.OfType<BeatmapLevelDataSO>().Select(level => level.songAudioClip)
#endif
                .FirstOrDefault(clip => clip != null && clip.name == name);
        }
        internal static async Task<AudioClip> LoadDecodedFile(string path) {
#if BEAT_SABER_1_29_0
            using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.GetFullPath(path)).AbsoluteUri,
                AudioTypeHelper.GetAudioTypeFromPath(path));
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
#if BEAT_SABER_1_29_0
            controller.selectedBeatmapLevel;
#else
            controller.beatmapLevel;
#endif
    }
}
