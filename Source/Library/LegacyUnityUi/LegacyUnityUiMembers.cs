#if LEGATO_EARLY_SCENE_SETUP || LEGATO_EARLY_STANDARD_MAPS
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity 2019 UI members expressed with the Unity 2018.1 UI system.
/// </summary>
internal static class LegacyUnityUiMembers
{
    private static readonly Dictionary<Type, PropertyInfo?> MultiplierProperties = new();

    extension(Image image)
    {
        /// <summary>Sliced sprite border scale. Images without a sliced renderer keep the native scale.</summary>
        internal float pixelsPerUnitMultiplier
        {
            get => MultiplierProperty(image.GetType()) is { } property ? (float)property.GetValue(image) : 1f;
            set => MultiplierProperty(image.GetType())?.SetValue(image, value);
        }
    }

    extension(ref ColorBlock colors)
    {
        /// <summary>Unity 2018.1 has no selected state; selection uses the highlighted color.</summary>
        internal Color selectedColor
        {
            get => colors.highlightedColor;
            set { }
        }
    }

    extension(HorizontalOrVerticalLayoutGroup group)
    {
        /// <summary>Unity 2018.1 layout groups always use unscaled child sizes.</summary>
        internal bool childScaleWidth
        {
            get => false;
            set { }
        }

        /// <summary>Unity 2018.1 layout groups always use unscaled child sizes.</summary>
        internal bool childScaleHeight
        {
            get => false;
            set { }
        }
    }

    extension(CanvasGroup group)
    {
        /// <summary>Unity 2018.1 canvas groups cannot be disabled; disabling restores their neutral state.</summary>
        internal bool enabled
        {
            get => true;
            set
            {
                if (value)
                    return;
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }
    }

    private static PropertyInfo? MultiplierProperty(Type type)
    {
        lock (MultiplierProperties)
        {
            if (!MultiplierProperties.TryGetValue(type, out PropertyInfo? property))
            {
                property = type.GetProperty("PixelsPerUnitMultiplier", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property?.PropertyType != typeof(float) || !property.CanWrite) property = null;
                MultiplierProperties[type] = property;
            }
            return property;
        }
    }
}
#endif
