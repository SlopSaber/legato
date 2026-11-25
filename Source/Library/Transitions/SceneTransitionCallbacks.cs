using System;

namespace Legato.Gameplay
{
    internal static class SceneTransitionCallbacks
    {
        internal static Delegate Started(Action<float> callback)
        {
#if LEGATO_UNTYPED_SCENE_TRANSITIONS
            return callback;
#else
            return new Action<GameScenesManager.SceneTransitionType, float>((type, progress) => callback(progress));
#endif
        }
    }
}
