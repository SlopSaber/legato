#if !LEGATO_ENVIRONMENT_COLOR_PROVIDER
using System.Reflection;
using UnityEngine;
using Zenject;

namespace Legato.Rendering
{
    internal static class MenuEnvironmentColors
    {
#if LEGATO_BEFORE_MULTIPLAYER
        private static readonly FieldInfo Scheme = typeof(ColorManager).GetField(
#else
        private static readonly FieldInfo Scheme = typeof(EnvironmentColorManager).GetField(
#endif
            "_colorScheme", BindingFlags.Instance | BindingFlags.NonPublic);

#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
        internal static ScriptableObject CloneSetting(ScriptableObject source)
        {
            var clone = Object.Instantiate(source);
            for (var type = source.GetType(); type != null; type = type.BaseType)
            {
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ObservableVariableSO<>)) continue;
                // Unity cloning omits the observable's nonserialized live value and event subscribers.
                var value = type.GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
                value.SetValue(clone, value.GetValue(source));
                break;
            }
            return clone;
        }
#endif

        internal static GameObject Bind(DiContainer container, ColorScheme scheme)
        {
            // Only palette getters are needed; native lifecycle methods write shared color assets.
            var owner = new GameObject("Menu Environment Color Provider");
            owner.SetActive(false);
#if LEGATO_BEFORE_MULTIPLAYER
            var manager = owner.AddComponent<ColorManager>();
#else
            var manager = owner.AddComponent<EnvironmentColorManager>();
#endif
            Scheme.SetValue(manager, scheme);
            container.BindInstance(manager).AsSingle();
            return owner;
        }
    }
}
#endif
