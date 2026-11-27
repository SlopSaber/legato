#if !LEGATO_EARLY_SCENE_SETUP
#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HMUI;
using UnityEngine;
#if BEAT_SABER_1_11_1
using TMPro;
#endif

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

#if BEAT_SABER_1_45_3 || BEAT_SABER_1_29_0 || BEAT_SABER_1_11_1
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
#if BEAT_SABER_1_11_1
        private static readonly ConditionalWeakTable<LevelListTableCell, GameObject> LegacyBadges =
            new ConditionalWeakTable<LevelListTableCell, GameObject>();
        private static readonly AccessTools.FieldRef<LevelListTableCell, string> CurrentLevelId =
            AccessTools.FieldRefAccess<LevelListTableCell, string>("_settingDataFromLevelId");

        private static GameObject? CurrentBadge(LevelListTableCell cell) =>
            LegacyBadges.TryGetValue(cell, out var badge) ? badge : null;

        private static GameObject CreateLegacyBadge(LevelListTableCell cell, TMP_Text title)
        {
            var badge = new GameObject("Ranked Badge", typeof(RectTransform), typeof(ImageView));
            badge.SetActive(false);
            var rect = (RectTransform)badge.transform;
            rect.SetParent(cell.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-1f, 0.45f);
            rect.sizeDelta = new Vector2(13f, 1.9f);
            badge.GetComponent<ImageView>().raycastTarget = false;

            var textObject = new GameObject("PromoText", typeof(RectTransform), typeof(TextMeshProUGUI));
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(rect, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = title.font;
            label.fontSharedMaterial = title.fontSharedMaterial;
            label.fontSize = 1.65f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return badge;
        }
#elif !BEAT_SABER_1_29_0
        private static readonly AccessTools.FieldRef<LevelListTableCell, BeatmapLevel> CurrentLevel = AccessTools.FieldRefAccess<LevelListTableCell, BeatmapLevel>("_beatmapLevel");
#endif

        private static bool MatchesLevel(CellBinding binding) =>
#if BEAT_SABER_1_11_1
            CurrentLevelId(binding.Cell) == binding.Level.levelID;
#elif BEAT_SABER_1_29_0
            IsStored(binding);
#else
            ReferenceEquals(CurrentLevel(binding.Cell), binding.Level);
#endif
#if !BEAT_SABER_1_11_1
        private static readonly AccessTools.FieldRef<LevelListTableCell, GameObject> CurrentBadge = AccessTools.FieldRefAccess<LevelListTableCell, GameObject>("_promoBadgeGo");
#endif
        private static readonly AccessTools.FieldRef<TableCell, ITableCellOwner> CurrentOwner = AccessTools.FieldRefAccess<TableCell, ITableCellOwner>("_tableCellOwner");

        [HarmonyPrefix]
        private static void Prefix(LevelListTableCell __instance, BeatmapLevel __0, string ____settingDataFromLevelId,
#if BEAT_SABER_1_11_1
            TextMeshProUGUI ____songNameText, out CellBinding? __state)
#else
            bool isPromoted, GameObject ____promoBadgeGo, out CellBinding? __state)
#endif
        {
            __state = null;
            if (IsStopped() || __0 == null || ____settingDataFromLevelId == __0.levelID) return;
            _bindings.Remove(__instance);
            PruneBindings();
#if BEAT_SABER_1_11_1
            var badge = LegacyBadges.GetValue(__instance, cell => CreateLegacyBadge(cell, ____songNameText));
            badge.SetActive(false);
            __state = new CellBinding(__instance, __0, false, badge);
#else
            __state = new CellBinding(__instance, __0, isPromoted, ____promoBadgeGo);
#endif
            _bindings.Add(__instance, __state);
            TrackedCells.Add(new WeakReference<CellBinding>(__state));
        }

        [HarmonyPostfix]
        private static void Postfix(LevelListTableCell __instance, CellBinding? __state)
        {
            if (__state != null && IsStored(__state) && MatchesLevel(__state))
                __state.Prepared = true;
            if (_bindings.TryGetValue(__instance, out var binding))
            {
                if (__state == null && binding.Pooled && binding.Prepared &&
                    MatchesLevel(binding) && ReferenceEquals(CurrentBadge(__instance), binding.Badge))
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
                !MatchesLevel(binding) ||
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
                if (!TrackedCells[i].TryGetTarget(out var binding) || !IsStored(binding) || binding.Pooled
#if !BEAT_SABER_1_11_1
                    || binding.ReadyApplied
#endif
                )
                    TrackedCells.RemoveAt(i);
        }

        internal static void RetireCell(LevelListTableCell cell)
        {
            if (_bindings.TryGetValue(cell, out var binding))
            {
                binding.Pooled = true;
#if BEAT_SABER_1_11_1
                if (binding.Badge != null) binding.Badge.SetActive(false);
#endif
            }
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
        private static void Postfix(ref BeatmapLevel __0, ref bool isPromoted, GameObject ____promoBadgeGo)
            => Apply(__0, isPromoted, ____promoBadgeGo, null);
#endif

        internal static void ClearBindings()
        {
#if BEAT_SABER_1_45_3 || BEAT_SABER_1_29_0 || BEAT_SABER_1_11_1
#if BEAT_SABER_1_11_1
            foreach (var reference in TrackedCells)
                if (reference.TryGetTarget(out var binding) && binding.Badge != null) binding.Badge.SetActive(false);
#endif
            _bindings = new ConditionalWeakTable<LevelListTableCell, CellBinding>();
            TrackedCells.Clear();
#endif
        }

        internal static void PruneRetiredBindings()
        {
#if BEAT_SABER_1_45_3 || BEAT_SABER_1_29_0 || BEAT_SABER_1_11_1
            PruneBindings();
#endif
        }

        internal static void RefreshReadyCells()
        {
#if BEAT_SABER_1_45_3 || BEAT_SABER_1_29_0 || BEAT_SABER_1_11_1
            RefreshBindings(null);
#endif
        }

    }

#if BEAT_SABER_1_45_3 || BEAT_SABER_1_29_0 || BEAT_SABER_1_11_1
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
#endif
