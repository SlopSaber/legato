using System;
using Zenject;
#if LEGATO_SCRIPTABLE_SCENE_TRANSITIONS
using SceneTransitionSetup = global::ScenesTransitionSetupDataSO;
#else
using SceneTransitionSetup = global::ScenesTransitionSetupData;
#endif

namespace Legato.Gameplay
{
    internal static class SceneEvents
    {
        internal static IDisposable SubscribeFinished(GameScenesManager manager, Action callback) => new FinishedSubscription(manager, callback);

        private sealed class FinishedSubscription : IDisposable
        {
            private readonly GameScenesManager _manager;
#if LEGATO_UNTYPED_SCENE_TRANSITIONS
            private readonly Action<SceneTransitionSetup, DiContainer> _handler;
#else
            private readonly Action<GameScenesManager.SceneTransitionType, SceneTransitionSetup, DiContainer> _handler;
#endif
            private bool _disposed;

            internal FinishedSubscription(GameScenesManager manager, Action callback)
            {
                _manager = manager;
#if LEGATO_UNTYPED_SCENE_TRANSITIONS
                _handler = (setup, container) => callback();
#else
                _handler = (kind, setup, container) => callback();
#endif
                manager.transitionDidFinishEvent += _handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _manager.transitionDidFinishEvent -= _handler;
            }
        }
    }
}
