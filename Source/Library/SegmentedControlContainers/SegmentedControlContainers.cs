using System;
using System.Reflection;
using HMUI;
using Zenject;

namespace Legato.Unity
{
    internal static class SegmentedControlContainers
    {
        private static readonly FieldInfo TextContainer = FindContainer(typeof(TextSegmentedControl));
        private static readonly FieldInfo IconContainer = FindContainer(typeof(IconSegmentedControl));

        internal static DiContainer GetContainer(TextSegmentedControl control) =>
            (DiContainer)TextContainer.GetValue(control);

        internal static DiContainer GetContainer(IconSegmentedControl control) =>
            (DiContainer)IconContainer.GetValue(control);

        internal static void SetContainer(TextSegmentedControl control, DiContainer container) =>
            TextContainer.SetValue(control, container);

        internal static void SetContainer(IconSegmentedControl control, DiContainer container) =>
            IconContainer.SetValue(control, container);

        private static FieldInfo FindContainer(Type controlType)
        {
            for (Type current = controlType; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField("_container", BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null && field.FieldType == typeof(DiContainer))
                {
                    return field;
                }
            }

            throw new MissingFieldException(controlType.FullName, "_container");
        }
    }
}
