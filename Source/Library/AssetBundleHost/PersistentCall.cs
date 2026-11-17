using System;
using System.Reflection;
using HarmonyLib;
using Legato.Assets;

namespace AssetBundleLoadingTools.Patches
{
    /// <summary>
    /// This patch removes any serialized m_TargetAssemblyTypeName since that field can be used for bad things, potentially including arbitrary code execution.
    /// See https://blog.includesecurity.com/2021/06/hacking-unity-games-malicious-unity-game-objects/ for details.
    /// </summary>
    [HarmonyPatch]
    internal class PersistentCall_OnAfterDeserialize
    {
        [HarmonyPrepare]
        public static bool Prepare() => PersistentCallSanitizer.Required;

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(object __instance)
        {
            PersistentCallSanitizer.ClearTargetAssemblyType(__instance);
        }

        public static MethodBase TargetMethod() => PersistentCallSanitizer.DeserializeMethod;
    }
}
