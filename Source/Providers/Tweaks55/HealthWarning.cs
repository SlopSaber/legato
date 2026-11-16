using HarmonyLib;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Tweaks55.Util;

namespace Tweaks55.HarmonyPatches {
	[HarmonyPatch]
	static class PatchHealthWarning {
		[HarmonyPriority(int.MinValue)]
#if BEAT_SABER_1_29_0
        static void Postfix(HealthWarningViewController __instance, bool __1) {
            if(Config.Instance.disableHealthWarning && __1)
                __instance.didFinishEvent?.Invoke();
        }
#else
		static void Postfix(HealthWarningViewController __instance) {
			if(!Config.Instance.disableHealthWarning || __instance._taskCompletionSource == null)
				return;

			__instance.Complete();
		}

#endif

		static MethodBase TargetMethod() => Resolver.GetMethod(nameof(HealthWarningViewController), nameof(HealthWarningViewController.DidActivate));
		static Exception Cleanup(Exception ex) => Plugin.PatchFailed(ex);
	}
}
