using System;

namespace Legato.Gameplay
{
    internal static class SceneTransitionCallbacks
    {
        internal static Delegate Started(Action<float> callback)
        {
#if BEAT_SABER_1_29_0
            return callback;
#else
            return new Action<GameScenesManager.SceneTransitionType, float>((type, progress) => callback(progress));
#endif
        }
    }
}
