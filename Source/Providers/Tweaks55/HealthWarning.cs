using HarmonyLib;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Tweaks55.Util;

namespace Tweaks55.HarmonyPatches {
	[HarmonyPatch]
	static class PatchHealthWarning {
#if BEAT_SABER_1_29_0
        static readonly IPA.Utilities.FieldAccessor<HealthWarningViewController, Action>.Accessor finished =
            IPA.Utilities.FieldAccessor<HealthWarningViewController, Action>.GetAccessor("didFinishEvent");

        [HarmonyPriority(int.MinValue)]
        static void Postfix(HealthWarningViewController __instance, bool __1) {
            if(Config.Instance.disableHealthWarning && __1)
                finished(ref __instance)?.Invoke();
        }
#else
		[HarmonyPriority(int.MinValue)]
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
