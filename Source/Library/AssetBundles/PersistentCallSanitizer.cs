#nullable enable

using System;
using System.Reflection;
using HarmonyLib;

namespace Legato.Assets {
    internal static class PersistentCallSanitizer {
        private static readonly Type PersistentCallType = Type.GetType("UnityEngine.Events.PersistentCall, UnityEngine.CoreModule", true)!;
        internal static readonly MethodInfo DeserializeMethod = AccessTools.DeclaredMethod(PersistentCallType, "OnAfterDeserialize");
        private static readonly FieldInfo TargetAssemblyTypeName = AccessTools.DeclaredField(PersistentCallType, "m_TargetAssemblyTypeName");

        internal static void ClearTargetAssemblyType(object instance) => TargetAssemblyTypeName.SetValue(instance, null);
    }
}
