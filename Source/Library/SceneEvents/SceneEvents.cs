using System;
using Zenject;

namespace Legato.Gameplay
{
    internal static class SceneEvents
    {
        internal static IDisposable SubscribeFinished(GameScenesManager manager, Action callback) => new FinishedSubscription(manager, callback);

        private sealed class FinishedSubscription : IDisposable
        {
            private readonly GameScenesManager _manager;
#if BEAT_SABER_1_29_0
            private readonly Action<ScenesTransitionSetupDataSO, DiContainer> _handler;
#else
            private readonly Action<GameScenesManager.SceneTransitionType, ScenesTransitionSetupData, DiContainer> _handler;
#endif
            private bool _disposed;

            internal FinishedSubscription(GameScenesManager manager, Action callback)
            {
                _manager = manager;
#if BEAT_SABER_1_29_0
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
