#if LEGATO_EARLY_SCENE_SETUP
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HMUI;
using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;

namespace Legato.Presentation;

internal static class LevelListBadgeBindings
{
    private static ConditionalWeakTable<TableCell, Badge> _badges = new();
    private static readonly List<WeakReference<Badge>> Tracked = new();
    private static Func<bool> _isStopped = () => true;
    private static Func<bool> _isReady = () => false;
    private static Func<string, GameObject, Func<bool>, bool> _apply = (_, _, _) => false;

    internal static void Initialize(Func<bool> isStopped, Func<bool> isReady, Func<string, GameObject, Func<bool>, bool> apply)
    {
        _isStopped = isStopped;
        _isReady = isReady;
        _apply = apply;
    }

    internal static void Bind(TableCell cell, TMP_Text title, string? levelId)
    {
        Retire(cell);
        if (_isStopped() || !title || string.IsNullOrEmpty(levelId) || !Path.IsPathRooted(levelId)) return;
        Badge badge = _badges.GetValue(cell, row =>
        {
            var created = new Badge(row, title);
            Tracked.Add(new WeakReference<Badge>(created));
            return created;
        });
        badge.LevelId = levelId;
    }

    internal static void Attach(TableCell cell, TableView table, int row)
    {
        if (!_badges.TryGetValue(cell, out Badge badge) || badge.LevelId == null) return;
        badge.Table = table;
        badge.Row = row;
        Publish(badge);
    }

    internal static void Retire(TableCell cell)
    {
        if (!_badges.TryGetValue(cell, out Badge badge)) return;
        badge.LevelId = null;
        badge.Table = null;
        badge.Applied = false;
        badge.RestoreTitle();
        if (badge.View) badge.View.SetActive(false);
    }

    internal static void RefreshReadyCells()
    {
        PruneRetiredBindings();
        foreach (WeakReference<Badge> reference in Tracked.ToArray())
            if (reference.TryGetTarget(out Badge badge)) Publish(badge);
    }

    private static void Publish(Badge badge)
    {
        string? levelId = badge.LevelId;
        TableView? table = badge.Table;
        int row = badge.Row;
        if (levelId == null || badge.Applied || !_isReady() || !IsCurrent()) return;

        if (_apply(levelId, badge.View, IsCurrent) && IsCurrent())
        {
            badge.Applied = true;
            if (badge.View.activeSelf) badge.ReserveTitle();
        }

        bool IsCurrent() => !_isStopped() && badge.Cell && badge.View && table && table!.isActiveAndEnabled &&
            badge.Cell.isActiveAndEnabled && badge.Cell.row == row && badge.Table == table && badge.LevelId == levelId;
    }

    internal static void PruneRetiredBindings() => Tracked.RemoveAll(reference => !reference.TryGetTarget(out Badge badge) || !badge.Cell);

    internal static void ClearBindings()
    {
        foreach (WeakReference<Badge> reference in Tracked)
        {
            if (!reference.TryGetTarget(out Badge badge)) continue;
            badge.RestoreTitle();
            if (badge.View) Object.Destroy(badge.View);
        }
        _badges = new ConditionalWeakTable<TableCell, Badge>();
        Tracked.Clear();
    }

    private sealed class Badge
    {
        private const float Width = 23f;
        private const float Inset = 1f;
        private readonly TMP_Text _title;
        private float? _previousRightMargin;

        internal Badge(TableCell cell, TMP_Text title)
        {
            Cell = cell;
            _title = title;
            View = new GameObject("Ranked Badge", typeof(RectTransform), typeof(Image));
            View.SetActive(false);
            View.GetComponent<Image>().raycastTarget = false;
            var rect = (RectTransform)View.transform;
            rect.SetParent(title.rectTransform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-Inset, 0f);
            rect.sizeDelta = new Vector2(Width, 4.5f);

            var text = new GameObject("PromoText", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(rect, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.font = title.font;
            text.fontSharedMaterial = title.fontSharedMaterial;
            text.fontSize = 2.8f;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
        }

        internal TableCell Cell { get; }
        internal GameObject View { get; }
        internal string? LevelId { get; set; }
        internal TableView? Table { get; set; }
        internal int Row { get; set; }
        internal bool Applied { get; set; }

        internal void ReserveTitle()
        {
            if (!_title || _previousRightMargin.HasValue) return;
            Vector4 margin = _title.margin;
            _previousRightMargin = margin.z;
            margin.z += Width + 2f * Inset;
            _title.margin = margin;
        }

        internal void RestoreTitle()
        {
            if (_title && _previousRightMargin is float previous)
            {
                Vector4 margin = _title.margin;
                if (Mathf.Approximately(margin.z, previous + Width + 2f * Inset))
                {
                    margin.z = previous;
                    _title.margin = margin;
                }
            }
            _previousRightMargin = null;
        }
    }
}

[HarmonyPatch(typeof(StandardLevelListTableView), nameof(StandardLevelListTableView.CellForRow))]
internal static class EarlyStandardLevelBadgePatch
{
    private static readonly AccessTools.FieldRef<StandardLevelListTableCell, TextMeshProUGUI> Title =
        AccessTools.FieldRefAccess<StandardLevelListTableCell, TextMeshProUGUI>("_songNameText");

    private static void Postfix(int __0, IStandardLevel[] ____levels, TableCell __result)
    {
        if (__result is StandardLevelListTableCell cell)
            LevelListBadgeBindings.Bind(cell, Title(cell), ____levels[__0]?.levelID);
    }
}

#if !LEGATO_EARLY_STANDARD_MAPS
[HarmonyPatch(typeof(CustomLevelListTableView), nameof(CustomLevelListTableView.CellForRow))]
internal static class EarlyImportedLevelBadgePatch
{
    private static readonly AccessTools.FieldRef<CustomLevelListTableCell, TextMeshProUGUI> Title =
        AccessTools.FieldRefAccess<CustomLevelListTableCell, TextMeshProUGUI>("_songNameText");

    private static void Postfix(TableCell __result)
    {
        if (__result is CustomLevelListTableCell cell)
            LevelListBadgeBindings.Bind(cell, Title(cell), cell.levelID);
    }
}
#endif

[HarmonyPatch(typeof(TableCell), nameof(TableCell.TableViewSetup))]
internal static class EarlyLevelBadgeAttachPatch
{
    private static void Postfix(TableCell __instance, TableView __0, int __1) => LevelListBadgeBindings.Attach(__instance, __0, __1);
}

[HarmonyPatch(typeof(TableView), "AddCellToReusableCells")]
internal static class EarlyLevelBadgePoolPatch
{
    private static void Prefix(TableCell __0) => LevelListBadgeBindings.Retire(__0);
}
#endif
