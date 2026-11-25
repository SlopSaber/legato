using System;
using MonoMod.RuntimeDetour;
using Zenject;

namespace SongCore.Hooks
{
    /// <summary>
    /// This binds the <see cref="BeatmapLevel"/> to the game scene.
    /// </summary>
    internal class BindBeatmapLevelHook : IInitializable, IDisposable
    {
        private Hook _installBindingsHook = null!;

        public void Initialize()
        {
            _installBindingsHook = new Hook(typeof(GameplayCoreInstaller).GetMethod(nameof(GameplayCoreInstaller.InstallBindings))!, BindBeatmapLevel, true);
        }

        public void Dispose()
        {
            _installBindingsHook.Dispose();
        }

        private void BindBeatmapLevel(Action<GameplayCoreInstaller> original, GameplayCoreInstaller instance)
        {
            original(instance);
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            instance.Container.Bind<BeatmapLevel>().FromInstance(instance._sceneSetupData.difficultyBeatmap.level).AsSingle();
#else
            instance.Container.Bind<BeatmapLevel>().FromInstance(instance._sceneSetupData.beatmapLevel).AsSingle();
#endif
        }
    }
}
