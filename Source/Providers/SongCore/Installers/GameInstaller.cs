using SongCore.Hooks;
using Zenject;

namespace SongCore.Installers
{
    internal class GameInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<SongDataGameHooks>().AsSingle();
            Container.BindInterfacesTo<DisableSubmissionHooks>().AsSingle();
#if BEAT_SABER_1_29_0
            Container.BindInterfacesTo<V4GameHooks>().AsSingle();
            Container.BindInterfacesTo<V4MovementHooks>().AsSingle();
            Container.BindInterfacesTo<V4LightingHooks>().AsSingle();
#elif LEGATO_FIXED_NOTE_JUMP_SPEED
            Container.BindInterfacesTo<AllowNegativeNoteJumpSpeedHook>().AsSingle();
#else
            Container.BindInterfacesTo<AllowNegativeVariableNoteJumpSpeedHook>().AsSingle();
#endif
        }
    }
}
