#if !LEGATO_BEFORE_MULTIPLAYER
using System;
using MonoMod.RuntimeDetour;
using Zenject;

namespace SongCore.Hooks
{
    /// <summary>
    /// This removes the download icon from empty custom annotated beatmap collections.
    /// </summary>
    internal class RemoveDownloadIconHook : IInitializable, IDisposable
    {
        private Hook _refreshAvailabilityAsyncHook = null!;

        public void Initialize()
        {
            _refreshAvailabilityAsyncHook = new Hook(typeof(AnnotatedBeatmapLevelCollectionCell).GetMethod(nameof(AnnotatedBeatmapLevelCollectionCell.RefreshAvailabilityAsync))!, HideDownloadIcon, true);
        }

        public void Dispose()
        {
            _refreshAvailabilityAsyncHook.Dispose();
        }

        private static void HideDownloadIcon(Action<AnnotatedBeatmapLevelCollectionCell, NativeEntitlements> original, AnnotatedBeatmapLevelCollectionCell instance, NativeEntitlements entitlementModel)
        {
            original(instance, entitlementModel);
            if (SongCoreNative.PackId(instance).StartsWith(CustomLevelLoader.kCustomLevelPackPrefixId, StringComparison.Ordinal))
            {
                instance.SetDownloadIconVisible(false);
            }
        }
    }
}
#endif
