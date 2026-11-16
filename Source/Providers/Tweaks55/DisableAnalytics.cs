#if !BEAT_SABER_1_29_0
using BeatGames.Analytics;
using HarmonyLib;
using OSCE.Analytics;
#endif
using System;
using System.Reflection;
using Tweaks55.Util;
using UnityEngine;

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
