using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Zenject;
#if BEAT_SABER_1_29_0
using NativeEditorTransition = global::BeatmapEditorScenesTransitionSetupDataSO;
#else
using NativeEditorTransition = global::BeatmapEditorScenesTransitionSetupData;
#endif

namespace SongCore.Hooks
{
    /// <summary>
    /// Ensures songs are refreshed when creating or converting maps in the editor.
    /// </summary>
    // TODO: Only do this if needed?
    internal class RefreshAfterEditorHook : IInitializable, IDisposable
    {
        private readonly Loader _loader;

        private Hook _handleBeatmapEditorSceneDidFinishHook = null!;

        private RefreshAfterEditorHook(Loader loader)
        {
            _loader = loader;
        }

        public void Initialize()
        {
            _handleBeatmapEditorSceneDidFinishHook = new Hook(typeof(MenuTransitionsHelper).GetMethod(nameof(MenuTransitionsHelper.HandleBeatmapEditorSceneDidFinish), BindingFlags.Instance | BindingFlags.NonPublic)!, RefreshSongs, true);
        }

        public void Dispose()
        {
            _handleBeatmapEditorSceneDidFinishHook.Dispose();
        }

        private void RefreshSongs(Action<MenuTransitionsHelper, NativeEditorTransition> original, MenuTransitionsHelper instance, NativeEditorTransition beatmapEditorScenesTransitionSetupData)
        {
            original(instance, beatmapEditorScenesTransitionSetupData);
            _loader.RefreshSongs();
        }
    }
}
