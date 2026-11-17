#if BEAT_SABER_1_29_0
using System.Reflection;
using UnityEngine;
using Zenject;

namespace Legato.Rendering
{
    internal static class MenuEnvironmentColors
    {
        private static readonly FieldInfo Scheme = typeof(EnvironmentColorManager).GetField(
            "_colorScheme", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static GameObject Bind(DiContainer container, ColorScheme scheme)
        {
            // Only palette getters are needed; native lifecycle methods write shared color assets.
            var owner = new GameObject("Menu Environment Color Provider");
            owner.SetActive(false);
            var manager = owner.AddComponent<EnvironmentColorManager>();
            Scheme.SetValue(manager, scheme);
            container.BindInstance(manager).AsSingle();
            return owner;
        }
    }
}
#endif
