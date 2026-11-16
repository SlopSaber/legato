#if BEAT_SABER_1_29_0
using HarmonyLib;
using HMUI;
using UnityEngine;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    [HarmonyPatch(typeof(TableView), nameof(TableView.ScrollToCellWithIdx))]
    internal static class TableView_ScrollToCellWithIdx
    {
        private static bool Prefix(TableView __instance, int idx, TableView.ScrollPositionType scrollPositionType, bool animated)
        {
            if (__instance.numberOfCells == 0)
            {
                return false;
            }

            int index = Mathf.Clamp(idx, 0, __instance.numberOfCells - 1);
            float position = index * __instance.cellSize;
            float freeSpace = __instance.scrollView.scrollPageSize - __instance.cellSize;
            if (scrollPositionType == TableView.ScrollPositionType.Center)
            {
                position -= freeSpace / 2;
            }
            else if (scrollPositionType == TableView.ScrollPositionType.End)
            {
                position -= freeSpace;
            }

            __instance.scrollView.ScrollTo(Mathf.Max(0, position), animated);
            if (!animated)
            {
                __instance.RefreshCells(true, false);
            }

            return false;
        }
    }
}
#else
﻿using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HMUI;
using static HMUI.TableView;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    /// <summary>
    /// This fixes a base-game bug where <see cref="TableView.ScrollToCellWithIdx"/> does not work as expected.
    /// </summary>
    [HarmonyPatch(typeof(TableView), nameof(TableView.ScrollToCellWithIdx))]
    internal class TableView_ScrollToCellWithIdx
    {
        private static readonly MethodInfo GetCellPositionMethod = typeof(TableView).GetMethod("GetCellPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo ScrollToPositionMethod = typeof(TableView).GetMethod(nameof(TableView.ScrollToPosition), BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo ClampIndexMethod = typeof(TableView_ScrollToCellWithIdx).GetMethod(nameof(ClampIndex), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo GetTargetScrollPositionMethod = typeof(TableView_ScrollToCellWithIdx).GetMethod(nameof(GetTargetScrollPosition), BindingFlags.NonPublic | BindingFlags.Static);

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /*
             * What we're doing here is replacing the contents of the method with the following:
             * {
             *     idx = ClampIndex(idx);
             *     float position = GetCellPosition(idx);
             *     GetTargetScrollPosition(position, scrollPositionType);
             *     ScrollToPosition(position, animated);
             * }
             */
            CodeMatcher codeMatcher = new(instructions);

            // find GetCellPosition(idx) call
            codeMatcher
                .MatchForward(false, new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldarg_1), new CodeMatch(i => i.Calls(GetCellPositionMethod)))
                .ThrowIfInvalid("GetCellPosition call not found");

            // remove everything up until that call and call our ClampIndex instead
            int count = codeMatcher.Pos;
            codeMatcher
                .Start()
                .RemoveInstructions(count)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, ClampIndexMethod),
                    new CodeInstruction(OpCodes.Starg_S, 1))
                .Advance(4);

            // find ScrollToPosition(position, animated) call
            int start = codeMatcher.Pos;
            codeMatcher
                .MatchForward(false, new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldloc_2), new CodeMatch(OpCodes.Ldarg_3), new CodeMatch(i => i.Calls(ScrollToPositionMethod)))
                .ThrowIfInvalid("ScrollToPosition call not found");

            // remove everything between GetCellPosition and ScrollToPosition and call our GetTargetScrollPosition
            count = codeMatcher.Pos - start;
            codeMatcher
                .Advance(-count)
                .RemoveInstructions(count)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc_2),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Call, GetTargetScrollPositionMethod),
                    new CodeInstruction(OpCodes.Stloc_2));

            return codeMatcher.InstructionEnumeration();
        }

        private static int ClampIndex(TableView self, int idx)
        {
            if (idx < 0)
            {
                return 0;
            }
            else if (idx >= self.numberOfCells)
            {
                return self.numberOfCells - 1;
            }
            else
            {
                return idx;
            }
        }

        private static float GetTargetScrollPosition(TableView self, float cellPosition, ScrollPositionType scrollPositionType)
        {
            return scrollPositionType switch
            {
                ScrollPositionType.Beginning => cellPosition,
                ScrollPositionType.Center => cellPosition - ((self.scrollView.scrollPageSize - self._cellSize) / 2),
                ScrollPositionType.End => cellPosition - (self.scrollView.scrollPageSize - self._cellSize),
                _ => cellPosition,
            };
        }
    }
}

#endif
