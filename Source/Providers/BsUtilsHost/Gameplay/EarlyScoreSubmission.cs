#if LEGATO_BSUTILS_EARLY
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace BS_Utils.Gameplay.HarmonyPatches
{
    [HarmonyPatch]
    internal static class EarlyScoreSubmission
    {
        private sealed class Disabled { }
        private static readonly ConditionalWeakTable<LevelCompletionResults, Disabled> DisabledResults = new();
        private static readonly ConditionalWeakTable<LevelCompletionResults, Disabled>.CreateValueCallback Mark = _ => new Disabled();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PrepareLevelEndMenuSceneSetupData), nameof(PrepareLevelEndMenuSceneSetupData.FillLevelCompletionResults))]
        private static void CapturePreparedResult(LevelCompletionResults __result) => CaptureDisabled(__result);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainGameSceneSetupData), nameof(MainGameSceneSetupData.Finish))]
        private static void CaptureFinishedResult(LevelCompletionResults levelCompletionResults) => CaptureDisabled(levelCompletionResults);

        private static void CaptureDisabled(LevelCompletionResults results)
        {
            if (results != null && (ScoreSubmission.disabled || ScoreSubmission.prolongedDisable || ScoreSubmission.WasDisabled))
            {
                DisabledResults.GetValue(results, Mark);
            }
        }

        [HarmonyAfter("dev.auros.sirautil")]
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ResultsViewController), "DidActivate")]
        private static IEnumerable<CodeInstruction> GateScoreWrites(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new(instructions);
            MethodInfo update = AccessTools.DeclaredMethod(typeof(PlayerLevelStatsData), nameof(PlayerLevelStatsData.UpdateScoreData));
            MethodInfo clear = AccessTools.DeclaredMethod(typeof(LocalLeaderboardsModel), nameof(LocalLeaderboardsModel.ClearLastScorePosition));
            int best = FindUnique(code, instruction => instruction.Calls(update) || IsSiraPersonalBest(instruction));
            int clearPosition = FindUnique(code, instruction => instruction.Calls(clear));
            if (best <= 0 || best + 1 >= code.Count || clearPosition < 3)
            {
                return Unsupported(code);
            }

            int soloGate = code.FindLastIndex(best - 1, instruction => Skips(instruction, code[best + 1]));
            int uploadGate = code.FindIndex(best + 1, instruction => Skips(instruction, code[clearPosition - 3]));
            if (soloGate < 0 || uploadGate <= best || code[soloGate].blocks.Count != 0 || code[uploadGate].blocks.Count != 0)
            {
                return Unsupported(code);
            }

            MethodInfo canStore = AccessTools.DeclaredMethod(typeof(EarlyScoreSubmission), nameof(CanStore));
            InsertGate(code, uploadGate, canStore);
            InsertGate(code, soloGate, canStore);
            return code;
        }

        [HarmonyAfter("dev.auros.sirautil")]
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ResultsFlowCoordinator), nameof(ResultsFlowCoordinator.Present))]
        private static IEnumerable<CodeInstruction> SkipNameEntry(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new(instructions);
            MethodInfo qualifies = AccessTools.DeclaredMethod(typeof(LocalLeaderboardsModel),
                nameof(LocalLeaderboardsModel.WillScoreGoIntoLeaderboard), new[] { typeof(string), typeof(int) });
            int index = FindUnique(code, instruction => instruction.Calls(qualifies));
            if (index < 0)
            {
                return Unsupported(code);
            }

            code.InsertRange(index + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EarlyScoreSubmission), nameof(AllowsResult))),
            });
            return code;
        }

        private static bool CanStore(bool allowed, ResultsViewController view) => AllowsResult(allowed, view.levelCompletionResults);

        private static bool AllowsResult(bool allowed, LevelCompletionResults results)
            => allowed && (results == null || !DisabledResults.TryGetValue(results, out _));

        private static bool IsSiraPersonalBest(CodeInstruction instruction) => instruction.opcode == OpCodes.Call
            && instruction.operand is MethodInfo method && method.Name == "UpdatePersonalBest"
            && method.DeclaringType?.FullName == "SiraUtil.Submissions.EarlySubmission";

        private static bool Skips(CodeInstruction instruction, CodeInstruction target)
            => (instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S)
                && instruction.operand is Label label && target.labels.Contains(label);

        private static int FindUnique(List<CodeInstruction> code, System.Predicate<CodeInstruction> match)
        {
            int index = code.FindIndex(match);
            return index == code.FindLastIndex(match) ? index : -1;
        }

        private static void InsertGate(List<CodeInstruction> code, int index, MethodInfo gate)
        {
            CodeInstruction loadView = new(OpCodes.Ldarg_0);
            loadView.labels.AddRange(code[index].labels);
            code[index].labels.Clear();
            code.InsertRange(index, new[] { loadView, new CodeInstruction(OpCodes.Call, gate) });
        }

        private static IEnumerable<CodeInstruction> Unsupported(List<CodeInstruction> code)
        {
            Utilities.Logger.Log("Could not find the native Results score guards.", IPA.Logging.Logger.Level.Error);
            return code;
        }
    }
}
#endif
