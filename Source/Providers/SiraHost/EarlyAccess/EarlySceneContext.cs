#if LEGATO_SIRA_EARLY_CONTEXTS
using System.Collections.Generic;
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
using System.Linq;
#endif
using SiraUtil.Affinity;
using SiraUtil.Zenject;
using SiraUtil.Zenject.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Legato.Sira
{
    internal sealed class EarlySceneContext : Context
    {
        private Scene _scene;
        private DiContainer _container = null!;

        public override DiContainer Container => _container;

        internal Location Location { get; private set; }

        internal Scene Scene => _scene;

        public override IEnumerable<GameObject> GetRootGameObjects()
        {
            if (Location == Location.App || !_scene.isLoaded)
            {
                yield break;
            }

            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root != gameObject)
                {
                    yield return root;
                }
            }
        }

        internal void Install(Location location, Scene scene, DiContainer? parent, System.Action<DiContainer> bindNative
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
            , DiContainer? nativeContainer
#endif
        )
        {
            _scene = scene;
            Location = location;
#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
            if (nativeContainer != null && parent != null && !parent.ParentContainers.Contains(nativeContainer))
            {
                _container = new DiContainer(new[] { parent, nativeContainer });
            }
            else
#endif
            {
                _container = parent == null ? new DiContainer() : new DiContainer(parent);
            }

            _container.IsInstalling = true;
            try
            {
                if (parent == null)
                {
                    ZenjectManagersInstaller.Install(_container);
                }

                _container.Bind<Context>().FromInstance(this).AsSingle();
                _container.Bind<SceneKernel>().FromNewComponentOn(gameObject).AsSingle().NonLazy();
                _container.Bind<AffinityManager>().AsSingle();
                _container.Bind<ILateDisposable>().To<AffinityKernel>().AsSingle().NonLazy();
                _container.Bind(typeof(IInitializable), typeof(System.IDisposable)).To<SiraKernel>().AsSingle();
                bindNative(_container);

#if LEGATO_SIRA_EARLY_NATIVE_CONTEXTS
                if (nativeContainer == null)
#endif
                {
                    List<MonoBehaviour> behaviours = new();
                    GetInjectableMonoBehaviours(behaviours);
                    foreach (MonoBehaviour behaviour in behaviours)
                    {
                        _container.QueueForInject(behaviour);
                    }

                    InstallSceneBindings(behaviours);
                }

                InstallInstallers();
            }
            finally
            {
                _container.IsInstalling = false;
            }

            _container.ResolveRoots();
            _container.Resolve<SceneKernel>().Initialize();
        }

#if LEGATO_SIRA_PUBLICIZED_ZENJECT
        public override void GetInjectableMonoBehaviours(List<MonoBehaviour> behaviours)
#else
        protected override void GetInjectableMonoBehaviours(List<MonoBehaviour> behaviours)
#endif
        {
            foreach (GameObject root in GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour != null && behaviour.GetComponentInParent<Context>() == null)
                    {
                        behaviours.Add(behaviour);
                    }
                }
            }
        }
    }
}
#endif
