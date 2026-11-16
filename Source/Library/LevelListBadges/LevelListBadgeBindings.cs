#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HMUI;
using UnityEngine;

namespace Legato.Presentation {
    [HarmonyPatch(typeof(LevelListTableCell), nameof(LevelListTableCell.SetDataFromLevelAsync))]
    internal static class LevelListBadgeBindings {
        private static Func<bool> IsStopped = () => true;
        private static Func<bool> IsReady = () => false;
        private static Func<BeatmapLevel, bool, GameObject, Func<bool>?, bool> Apply = (_, _, _, _) => false;

        internal static void Initialize(Func<bool> isStopped, Func<bool> isReady,
            Func<BeatmapLevel, bool, GameObject, Func<bool>?, bool> apply) {
            IsStopped = isStopped;
            IsReady = isReady;
            Apply = apply;
        }

#if BEAT_SABER_1_45_3
        private sealed class CellBinding
        {
            internal readonly LevelListTableCell Cell;
            internal readonly BeatmapLevel Level;
            internal readonly bool IsPromoted;
            internal readonly GameObject Badge;
            internal bool Prepared;
            internal bool Pooled;
            internal bool ReadyApplied;

            internal CellBinding(LevelListTableCell cell, BeatmapLevel level, bool isPromoted, GameObject badge)
            {
                Cell = cell;
                Level = level;
                IsPromoted = isPromoted;
                Badge = badge;
            }
        }

        private static ConditionalWeakTable<LevelListTableCell, CellBinding> _bindings = new ConditionalWeakTable<LevelListTableCell, CellBinding>();
        private static readonly List<WeakReference<CellBinding>> TrackedCells = new List<WeakReference<CellBinding>>();
        private static readonly AccessTools.FieldRef<LevelListTableCell, BeatmapLevel> CurrentLevel = AccessTools.FieldRefAccess<LevelListTableCell, BeatmapLevel>("_beatmapLevel");
        private static readonly AccessTools.FieldRef<LevelListTableCell, GameObject> CurrentBadge = AccessTools.FieldRefAccess<LevelListTableCell, GameObject>("_promoBadgeGo");
        private static readonly AccessTools.FieldRef<TableCell, ITableCellOwner> CurrentOwner = AccessTools.FieldRefAccess<TableCell, ITableCellOwner>("_tableCellOwner");

        [HarmonyPrefix]
        private static void Prefix(LevelListTableCell __instance, BeatmapLevel beatmapLevel, bool isPromoted,
            string ____settingDataFromLevelId, GameObject ____promoBadgeGo, out CellBinding? __state)
        {
            __state = null;
            if (IsStopped() || beatmapLevel == null || ____settingDataFromLevelId == beatmapLevel.levelID) return;
            _bindings.Remove(__instance);
            PruneBindings();
            __state = new CellBinding(__instance, beatmapLevel, isPromoted, ____promoBadgeGo);
            _bindings.Add(__instance, __state);
            TrackedCells.Add(new WeakReference<CellBinding>(__state));
        }

        [HarmonyPostfix]
        private static void Postfix(LevelListTableCell __instance, CellBinding? __state)
        {
            if (__state != null && IsStored(__state) && ReferenceEquals(CurrentLevel(__instance), __state.Level))
                __state.Prepared = true;
            if (_bindings.TryGetValue(__instance, out var binding))
            {
                if (__state == null && binding.Pooled && binding.Prepared &&
                    ReferenceEquals(CurrentLevel(__instance), binding.Level) && ReferenceEquals(CurrentBadge(__instance), binding.Badge))
                {
                    // A skipped native call retains the previously admitted level and promotion flag.
                    binding = new CellBinding(__instance, binding.Level, binding.IsPromoted, binding.Badge) { Prepared = true };
                    _bindings.Remove(__instance);
                    _bindings.Add(__instance, binding);
                    TrackedCells.Add(new WeakReference<CellBinding>(binding));
                }
                if (IsCurrent(binding, false)) PublishBadge(binding, false);
            }
            PruneBindings();
        }

        private static bool IsStored(CellBinding binding) => binding.Cell != null &&
            _bindings.TryGetValue(binding.Cell, out var current) && ReferenceEquals(current, binding);

        private static bool IsCurrent(CellBinding binding, bool requireVisible)
        {
            if (!binding.Prepared || binding.Pooled || !IsStored(binding) || binding.Badge == null ||
                !ReferenceEquals(CurrentLevel(binding.Cell), binding.Level) ||
                !ReferenceEquals(CurrentBadge(binding.Cell), binding.Badge)) return false;
            if (!requireVisible) return true;
            if (!binding.Cell.isActiveAndEnabled || !binding.Cell.gameObject.activeInHierarchy ||
                !(CurrentOwner(binding.Cell) is TableView table) || table == null || !table.isActiveAndEnabled)
                return false;
            foreach (var cell in table.visibleCells)
                if (ReferenceEquals(cell, binding.Cell)) return true;
            return false;
        }

        private static void PruneBindings()
        {
            for (int i = TrackedCells.Count - 1; i >= 0; --i)
                if (!TrackedCells[i].TryGetTarget(out var binding) || !IsStored(binding) || binding.Pooled || binding.ReadyApplied)
                    TrackedCells.RemoveAt(i);
        }

        internal static void RetireCell(LevelListTableCell cell)
        {
            if (_bindings.TryGetValue(cell, out var binding)) binding.Pooled = true;
            PruneBindings();
        }

        private static void PublishBadge(CellBinding binding, bool requireVisible)
        {
            if (Apply(binding.Level, binding.IsPromoted, binding.Badge, () => IsCurrent(binding, requireVisible)) &&
                IsReady() && IsCurrent(binding, requireVisible))
                binding.ReadyApplied = true;
        }

        internal static void RefreshCell(LevelListTableCell cell)
        {
            if (IsReady() && _bindings.TryGetValue(cell, out var binding) &&
                !binding.ReadyApplied && IsCurrent(binding, true)) PublishBadge(binding, true);
            PruneBindings();
        }

        internal static void RefreshTable(TableView table) => RefreshBindings(table);

        private static void RefreshBindings(TableView? table)
        {
            PruneBindings();
            if (!IsReady()) return;
            foreach (var reference in TrackedCells.ToArray())
            {
                if (reference.TryGetTarget(out var binding) && !binding.ReadyApplied && IsCurrent(binding, true) &&
                    (table == null || ReferenceEquals(CurrentOwner(binding.Cell), table))) PublishBadge(binding, true);
            }
            PruneBindings();
        }
#else
        [HarmonyPostfix]
        private static void Postfix(ref BeatmapLevel beatmapLevel, ref bool isPromoted, GameObject ____promoBadgeGo)
            => Apply(beatmapLevel, isPromoted, ____promoBadgeGo);
#endif

        internal static void ClearBindings()
        {
#if BEAT_SABER_1_45_3
            _bindings = new ConditionalWeakTable<LevelListTableCell, CellBinding>();
            TrackedCells.Clear();
#endif
        }

        internal static void PruneRetiredBindings()
        {
#if BEAT_SABER_1_45_3
            PruneBindings();
#endif
        }

        internal static void RefreshReadyCells()
        {
#if BEAT_SABER_1_45_3
            RefreshBindings(null);
#endif
        }

    }

#if BEAT_SABER_1_45_3
    [HarmonyPatch(typeof(LevelListTableCell), "WasPreparedForReuse")]
    internal static class LevelListTableCellReusePatch
    {
        [HarmonyPrefix]
        private static void Prefix(LevelListTableCell __instance) => LevelListBadgeBindings.RetireCell(__instance);
    }

    [HarmonyPatch(typeof(TableView), "AddCellToReusableCells")]
    internal static class LevelListTableCellPoolPatch
    {
        [HarmonyPrefix]
        private static void Prefix(TableCell cell)
        {
            if (cell is LevelListTableCell levelCell) LevelListBadgeBindings.RetireCell(levelCell);
        }
    }

    [HarmonyPatch(typeof(TableCell), nameof(TableCell.TableViewSetup))]
    internal static class LevelListTableCellOwnerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(TableCell __instance)
        {
            if (__instance is LevelListTableCell cell) LevelListBadgeBindings.RefreshCell(cell);
        }
    }

    [HarmonyPatch(typeof(TableView), nameof(TableView.Show))]
    internal static class LevelListTableShowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(TableView __instance) => LevelListBadgeBindings.RefreshTable(__instance);
    }

    [HarmonyPatch(typeof(TableView), "OnEnable")]
    internal static class LevelListTableEnablePatch
    {
        [HarmonyPostfix]
        private static void Postfix(TableView __instance) => LevelListBadgeBindings.RefreshTable(__instance);
    }
#endif
}
