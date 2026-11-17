#nullable enable

using System;
using System.Reflection;

namespace Legato.Assets {
    internal static class PersistentCallSanitizer {
        private static readonly Type PersistentCallType = Type.GetType("UnityEngine.Events.PersistentCall, UnityEngine.CoreModule", true)!;
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo? TargetAssemblyTypeName = PersistentCallType.GetField("m_TargetAssemblyTypeName", InstanceMembers);
        internal static bool Required => TargetAssemblyTypeName != null;
        internal static MethodInfo DeserializeMethod => PersistentCallType.GetMethod("OnAfterDeserialize", InstanceMembers, null, Type.EmptyTypes, null)
            ?? throw new MissingMethodException(PersistentCallType.FullName, "OnAfterDeserialize");

        internal static void ClearTargetAssemblyType(object instance) => TargetAssemblyTypeName?.SetValue(instance, null);
    }
}
