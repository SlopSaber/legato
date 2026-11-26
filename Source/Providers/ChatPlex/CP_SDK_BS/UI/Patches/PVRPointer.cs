using HarmonyLib;
using System;
using VRUIControls;

namespace CP_SDK_BS.UI.Patches
{
    [HarmonyPatch(typeof(VRPointer))]
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
    [HarmonyPatch("OnEnable", new Type[] { })]
#elif LEGATO_ENABLED_LAST_SELECTED_POINTER
    [HarmonyPatch("EnabledLastSelectedPointer", new Type[] { })]
#else
    [HarmonyPatch(nameof(VRPointer.ShowLastSelectedPointer), new Type[] { })]
#endif
    internal class PVRPointer
    {
        internal static event Action<VRPointer> OnActivated;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Prefix
        /// </summary>
        /// <param name="__instance">VRPointer instance</param>
        internal static void Postfix(VRPointer __instance)
        {
            try { OnActivated?.Invoke(__instance); }
            catch { }
        }
    }
}
