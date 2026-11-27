#if LEGATO_SIRA_EARLY_CONTEXTS
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SiraUtil.Affinity;
using TMPro;
using UnityEngine;

namespace SiraUtil.Submissions
{
    internal sealed class EarlySubmissionCompletionInjector : IAffinity
    {
        private readonly Submission _submission;
        private readonly SubmissionDataContainer _submissionData;

        public EarlySubmissionCompletionInjector(Submission submission, SubmissionDataContainer submissionData)
        {
            _submission = submission;
            _submissionData = submissionData;
            _submissionData.SSS(true);
        }

        [AffinityPostfix]
        [AffinityPatch(typeof(PrepareLevelEndMenuSceneSetupData), nameof(PrepareLevelEndMenuSceneSetupData.FillLevelCompletionResults),
            AffinityMethodType.Normal, new[] { typeof(LevelCompletionResults.LevelEndStateType) })]
        private void RecordResult(LevelCompletionResults __result)
        {
            if (_submission.Activated)
            {
                EarlySubmission.Disable(__result, SubmissionDataContainer.Format(_submission.Tickets()));
                _submissionData.SSS(false);
            }
        }
    }

    [HarmonyPatch]
    internal static class EarlySubmission
    {
        private const string NoticeName = "SiraUtil Score Submission";
        private static readonly ConditionalWeakTable<LevelCompletionResults, string> DisabledResults = new();

        internal static void Disable(LevelCompletionResults results, string reasons)
            => DisabledResults.GetValue(results, _ => reasons);

        internal static bool IsEnabled(LevelCompletionResults? results)
            => results == null || !DisabledResults.TryGetValue(results, out _);

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ResultsViewController), "DidActivate")]
        private static IEnumerable<CodeInstruction> GateScoreWrites(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new(instructions);
            MethodInfo update = AccessTools.DeclaredMethod(typeof(PlayerLevelStatsData), nameof(PlayerLevelStatsData.UpdateScoreData));
            MethodInfo clear = AccessTools.DeclaredMethod(typeof(LocalLeaderboardsModel), nameof(LocalLeaderboardsModel.ClearLastScorePosition));
            int updateIndex = FindUniqueCall(code, update);
            int clearIndex = FindUniqueCall(code, clear);
            int gate = clearIndex < 3 ? -1 : code.FindIndex(instruction =>
                (instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S)
                && instruction.operand is Label target && code[clearIndex - 3].labels.Contains(target));

            if (updateIndex < 0 || gate <= updateIndex || code[gate - 1].opcode != OpCodes.Ldloc_0
                || code[gate].blocks.Count != 0 || code[updateIndex].blocks.Count != 0)
            {
                Debug.LogError("SiraUtil could not find the native Results score gates.");
                return code;
            }

            // Use the stock no-upload path so leaderboard refresh and Party score modifiers still run.
            InsertBefore(code, gate, new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EarlySubmission), nameof(CanUpload))));
            code[updateIndex].opcode = OpCodes.Call;
            code[updateIndex].operand = AccessTools.DeclaredMethod(typeof(EarlySubmission), nameof(UpdatePersonalBest));
            InsertBefore(code, updateIndex, new CodeInstruction(OpCodes.Ldarg_0));
            return code;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ResultsFlowCoordinator), nameof(ResultsFlowCoordinator.Present))]
        private static IEnumerable<CodeInstruction> SkipNameEntry(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new(instructions);
            MethodInfo qualifies = AccessTools.DeclaredMethod(typeof(LocalLeaderboardsModel),
                nameof(LocalLeaderboardsModel.WillScoreGoIntoLeaderboard), new[] { typeof(string), typeof(int) });
            int index = FindUniqueCall(code, qualifies);
            if (index < 0)
            {
                Debug.LogError("SiraUtil could not find the native Results name-entry gate.");
                return code;
            }

            code.InsertRange(index + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EarlySubmission), nameof(RequiresNameEntry))),
            });
            return code;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ResultsViewController), "SetDataToUI")]
        private static void ShowNotice(ResultsViewController __instance, TextMeshProUGUI ____scoreInfoText)
        {
            Transform existing = __instance.transform.Find(NoticeName);
            LevelCompletionResults results = __instance.levelCompletionResults;
            if (!DisabledResults.TryGetValue(results, out string reasons))
            {
                if (existing != null)
                {
                    existing.gameObject.SetActive(false);
                }

                return;
            }

            TextMeshProUGUI notice = existing != null ? existing.GetComponent<TextMeshProUGUI>() : CreateNotice(__instance, ____scoreInfoText);
            notice.text = $"<color=#f03030>Score Submission Disabled By</color>\n{reasons.TrimEnd()}";
            notice.gameObject.SetActive(true);
        }

        private static TextMeshProUGUI CreateNotice(ResultsViewController view, TextMeshProUGUI stockText)
        {
            TextMeshProUGUI notice = UnityEngine.Object.Instantiate(stockText, view.transform, false);
            notice.gameObject.name = NoticeName;
            notice.raycastTarget = false;
            notice.enableWordWrapping = true;
            notice.enableAutoSizing = true;
            notice.fontSizeMin = 1.8f;
            notice.fontSizeMax = 3f;
            notice.overflowMode = TextOverflowModes.Ellipsis;
            notice.alignment = TextAlignmentOptions.Center;
            RectTransform rect = notice.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 18f);
            rect.sizeDelta = new Vector2(130f, 8f);
            return notice;
        }

        private static bool CanUpload(bool canUpload, ResultsViewController view)
            => canUpload && IsEnabled(view.levelCompletionResults);

        private static bool RequiresNameEntry(bool qualifies, LevelCompletionResults results)
            => qualifies && IsEnabled(results);

        private static void UpdatePersonalBest(PlayerLevelStatsData stats, int score, int maxCombo, LevelCompletionResults.Rank rank, ResultsViewController view)
        {
            if (IsEnabled(view.levelCompletionResults))
            {
                stats.UpdateScoreData(score, maxCombo, rank);
            }
        }

        private static int FindUniqueCall(List<CodeInstruction> code, MethodInfo method)
        {
            bool IsTarget(CodeInstruction instruction) => (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
                && Equals(instruction.operand, method);
            int first = code.FindIndex(IsTarget);
            return first == code.FindLastIndex(IsTarget) ? first : -1;
        }

        private static void InsertBefore(List<CodeInstruction> code, int index, params CodeInstruction[] inserted)
        {
            inserted[0].labels.AddRange(code[index].labels);
            code[index].labels.Clear();
            code.InsertRange(index, inserted);
        }
    }
}
#endif
