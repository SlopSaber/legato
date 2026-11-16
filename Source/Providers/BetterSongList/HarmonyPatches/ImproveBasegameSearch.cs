using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BetterSongList.Util;

namespace BetterSongList.HarmonyPatches {
#if BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(BeatmapLevelFilterModel), "LevelContainsText")]
#else
	[HarmonyPatch(typeof(LevelFilter), nameof(LevelFilter.FilterLevelByText))]
#endif
	static class ImproveBasegameSearch {
#if BEAT_SABER_1_29_0
        [HarmonyPriority(int.MinValue + 10)]
        static bool Prefix(IPreviewBeatmapLevel __0, string[] __1, ref bool __result) {
            if(!Config.Instance.ModBasegameSearch) return true;
            var text = __0.songName + " " + __0.songSubName + " " + __0.songAuthorName + " " + __0.levelAuthorName;
            var query = __1.Length > 1 ? string.Join(" ", __1) : __1.FirstOrDefault();
            __result = !string.IsNullOrWhiteSpace(query) && text.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
            return false;
        }
#else
		[HarmonyPriority(int.MinValue + 10)]
		static void Prefix(ref string[] searchTerms) {
			if(!Config.Instance.ModBasegameSearch)
				return;

			if(searchTerms.Length > 1)
				searchTerms = new string[] { string.Join(" ", searchTerms) };
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			if(!Config.Instance.ModBasegameSearch)
				return instructions;

			// This appends a space and the mappers/lighters to the levelString variable.
			var matcher = new CodeMatcher(instructions)
				.MatchForward(true,
					new CodeMatch(OpCodes.Ldloc_2, null, "L_beatmapLevel"),
					new CodeMatch(),
					new CodeMatch(OpCodes.Stelem_Ref),
					new CodeMatch(x => x.opcode == OpCodes.Call && (x.operand as MethodInfo)?.Name == nameof(string.Concat), "Call_Concat")
				);

			matcher.Advance(1).Insert(
				new CodeInstruction(OpCodes.Ldstr, " "),
				new CodeInstruction(matcher.NamedMatch("L_beatmapLevel").opcode),
				Transpilers.EmitDelegate<Func<BeatmapLevel, string>>(level => BeatmapsUtil.ConcatMappers(level)),
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat),
					new[] { typeof(string), typeof(string), typeof(string) }))
			);

			return matcher.InstructionEnumeration();
		}
#endif
	}
}
