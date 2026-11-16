using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TMPro;

namespace Tweaks55.HarmonyPatches {
	[HarmonyPatch]
	static class RawScoreDisplay {
		[HarmonyPriority(int.MaxValue)]
		static bool Prefix(TextMeshProUGUI ____scoreText) {
			if(!Config.Instance.disableRawScore)
				return true;

			____scoreText.text = "";
			return false;
		}

		static MethodBase TargetMethod() => AccessTools.Method(typeof(ScoreUIController), nameof(ScoreUIController.UpdateScore), 
#if BEAT_SABER_1_29_0
            new[] { typeof(int), typeof(int) }
#else
            new[] { typeof(int) }
#endif
        );

		static Exception Cleanup(Exception ex) => Plugin.PatchFailed(ex);
	}
}
