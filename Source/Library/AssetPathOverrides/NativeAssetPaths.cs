using System;
using System.IO;
using HarmonyLib;
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace Legato.Assets
{
    internal static class NativeAssetPaths
    {
        private static Func<string, string> _rewrite;

        internal static void Install(Harmony harmony, Func<string, string> rewrite)
        {
            _rewrite = rewrite ?? throw new ArgumentNullException(nameof(rewrite));
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            var prefix = new HarmonyMethod(typeof(NativeAssetPaths), nameof(RewriteLegacyFile));
            harmony.Patch(AccessTools.Method(typeof(SteamBeatmapDataAssetFileModel), "GetAssetBundleFileForPreviewLevelAsync"), prefix: prefix);
            harmony.Patch(AccessTools.Method(typeof(OculusBeatmapDataAssetFileModel), "GetAssetBundleFileForPreviewLevelAsync"), prefix: prefix);
#else
            var postfix = new HarmonyMethod(typeof(NativeAssetPaths), nameof(RewritePath));
            harmony.Patch(AccessTools.Method(typeof(BeatmapDataAssetsModel), nameof(BeatmapDataAssetsModel.GetAssetBundleStreamingAssetsPathForBeatmapLevelId)), postfix: postfix);
            harmony.Patch(AccessTools.Method(typeof(BeatmapDataAssetsModel), nameof(BeatmapDataAssetsModel.AssetBundleStreamingAssetsPathForAssetBundleName)), postfix: postfix);
#endif
        }

#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        private static bool RewriteLegacyFile(IPreviewBeatmapLevel previewBeatmapLevel, CancellationToken cancellationToken, ref Task<GetAssetBundleFileResult> __result)
        {
            var filename = BeatmapDataAssetsModel.AssetBundleNameForBeatmapLevel(previewBeatmapLevel.levelID);
            var path = _rewrite(filename);
            if (string.Equals(path, filename, StringComparison.Ordinal) || !File.Exists(path))
            {
                return true;
            }

            __result = cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<GetAssetBundleFileResult>(cancellationToken)
                : Task.FromResult(new GetAssetBundleFileResult(false, path));
            return false;
        }
#else
        private static void RewritePath(ref string __result)
        {
            __result = _rewrite(__result);
        }
#endif
    }
}
