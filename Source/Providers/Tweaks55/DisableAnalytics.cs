#if BEAT_SABER_1_29_0 || LEGATO_ANALYTICS_DISPATCHER
#if LEGATO_ANALYTICS_DISPATCHER
using BeatGames.Analytics;
using OSCE.Analytics;
#endif
using HarmonyLib;
using System;
using System.Reflection;

namespace Tweaks55.HarmonyPatches {
	[HarmonyPatch]
	static class DisableAnalytics {
#if BEAT_SABER_1_29_0
        [HarmonyPriority(int.MaxValue)]
        static bool Prefix() => !Config.Instance.disableTelemetry;

        static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods() {
            foreach(var name in new[] { "LogEvent", "LogEditAvatarEvent", "LogClick", "LogImpression", "LogExposure" })
                yield return AccessTools.Method(typeof(OculusAnalyticsModel), name);
        }
#else
		[HarmonyPriority(int.MaxValue)]
		static void Prefix(AnalyticsManager analyticsManager) {
			if(!Config.Instance.disableTelemetry)
				return;

			analyticsManager.SetSystemMode(AnalyticsSystemModeEnum.DISABLED);
		}

		static MethodBase TargetMethod() => AccessTools.Constructor(typeof(AnalyticsEventsDispatcher), new[] { typeof(AnalyticsManager), typeof(string) });
#endif
		static Exception Cleanup(Exception ex) => Plugin.PatchFailed(ex);
	}
}
#endif
