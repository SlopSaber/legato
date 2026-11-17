#if BEAT_SABER_1_29_0
using System;
using System.Collections.Generic;
using SiraUtil.Affinity;
using UnityEngine;

namespace PlaylistManager.AffinityPatches {
    internal class AnnotatedBeatmapLevelCollectionsUIPatches : IAffinity {
        private readonly MainFlowCoordinator _mainFlowCoordinator;
        private readonly SelectLevelCategoryViewController _selectLevelCategoryViewController;
        private int _originalColumnCount;
        private Vector2 _gridSize;
        private Vector2 _gridPosition;
        private Vector2 _screenSize;
        private bool _expanded;
        private float _extraWidth;
        private bool _screenExpanded;
        private AnnotatedBeatmapLevelCollectionsUIPatches(MainFlowCoordinator mainFlowCoordinator, SelectLevelCategoryViewController selectLevelCategoryViewController) {
            _mainFlowCoordinator = mainFlowCoordinator;
            _selectLevelCategoryViewController = selectLevelCategoryViewController;
        }
        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridView), nameof(AnnotatedBeatmapLevelCollectionsGridView.SetData))]
        [AffinityPrefix]
        private void ResizeGrid(AnnotatedBeatmapLevelCollectionsGridView __instance, IReadOnlyList<NativeSongCollection> annotatedBeatmapLevelCollections) {
            var grid = __instance._gridView;
            var rect = (RectTransform)grid.transform;
            var screen = (RectTransform)_mainFlowCoordinator._screenSystem.mainScreen.transform;
            if (_originalColumnCount == 0) {
                _originalColumnCount = grid._columnCount;
                _gridSize = rect.sizeDelta;
                _gridPosition = rect.anchoredPosition;
                _screenSize = screen.sizeDelta;
            }
            if (_selectLevelCategoryViewController.selectedLevelCategory == SelectLevelCategoryViewController.LevelCategory.CustomSongs) {
                grid._columnCount = Math.Max(Mathf.CeilToInt((annotatedBeatmapLevelCollections?.Count ?? 0) / 5f), _originalColumnCount);
                var extra = (grid._columnCount - _originalColumnCount) * __instance._cellWidth;
                rect.sizeDelta = new Vector2(_gridSize.x + extra, _gridSize.y);
                rect.anchoredPosition = _gridPosition;
                _extraWidth = extra;
                _expanded = true;
            } else {
                grid._columnCount = _originalColumnCount;
                rect.sizeDelta = _gridSize;
                rect.anchoredPosition = _gridPosition;
                RestoreScreenSize();
                _expanded = false;
                _extraWidth = 0;
            }
        }
        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.AnimateOpen))]
        [AffinityPrefix]
        private void ExpandScreenSize() {
            if (!_expanded) return;
            var category = (RectTransform)_selectLevelCategoryViewController.transform;
            if (category.anchorMin.x != 0.5f || category.anchorMax.x != 0.5f) {
                var position = category.localPosition;
                category.anchorMin = new Vector2(0.5f, category.anchorMin.y);
                category.anchorMax = new Vector2(0.5f, category.anchorMax.y);
                category.localPosition = position;
            }
            ((RectTransform)_mainFlowCoordinator._screenSystem.mainScreen.transform).sizeDelta =
                new Vector2(_screenSize.x + _extraWidth * 2, _screenSize.y);
            _screenExpanded = true;
        }
        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.AnimateClose))]
        [AffinityPostfix]
        private void RestoreScreenSize() {
            if (!_screenExpanded) return;
            ((RectTransform)_mainFlowCoordinator._screenSystem.mainScreen.transform).sizeDelta = _screenSize;
            _screenExpanded = false;
        }
    }
}
#else
﻿using System;
using System.Collections.Generic;
using SiraUtil.Affinity;
using UnityEngine;

namespace PlaylistManager.AffinityPatches
{
    // TODO: When UI is overlapping other view controllers, blur them.
    internal class AnnotatedBeatmapLevelCollectionsUIPatches : IAffinity
    {
        private readonly MainFlowCoordinator _mainFlowCoordinator;
        private readonly AnnotatedBeatmapLevelCollectionsViewController _annotatedBeatmapLevelCollectionsViewController;
        private readonly SelectLevelCategoryViewController _selectLevelCategoryViewController;

        private int _originalColumnCount;
        private Vector2 _originalScreenSize;
        private bool _isGridResized;
        private bool _isScreenResized;

        private AnnotatedBeatmapLevelCollectionsUIPatches(MainFlowCoordinator mainFlowCoordinator, AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController, SelectLevelCategoryViewController selectLevelCategoryViewController)
        {
            _mainFlowCoordinator = mainFlowCoordinator;
            _annotatedBeatmapLevelCollectionsViewController = annotatedBeatmapLevelCollectionsViewController;
            _selectLevelCategoryViewController = selectLevelCategoryViewController;
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridView), nameof(AnnotatedBeatmapLevelCollectionsGridView.SetData))]
        [AffinityPrefix]
        private void ResizeGrid(AnnotatedBeatmapLevelCollectionsGridView __instance, IReadOnlyList<NativeSongCollection> annotatedBeatmapLevelCollections)
        {
            if (_originalColumnCount == default)
            {
                _originalColumnCount = __instance._gridView._columnCount;
            }

            var selectedLevelCategory = _selectLevelCategoryViewController.selectedLevelCategory;
            if (selectedLevelCategory == SelectLevelCategoryViewController.LevelCategory.CustomSongs)
            {
                // Number of columns for max visible row count before it starts clipping with the ground.
                __instance._gridView._columnCount = Math.Max(Mathf.CeilToInt((annotatedBeatmapLevelCollections?.Count ?? 0) / 5f), _originalColumnCount);

                // Remove one column to make room for our buttons.
                if (!_isGridResized)
                {
                    __instance._gridView._visibleColumnCount -= 1;

                    var rectTransform = (RectTransform)__instance._gridView.transform;
                    rectTransform.sizeDelta -= new Vector2(__instance._cellWidth, 0);
                    rectTransform.anchoredPosition -= new Vector2(__instance._cellWidth / 2, 0);

                    _isGridResized = true;
                }
            }
            else if (selectedLevelCategory == SelectLevelCategoryViewController.LevelCategory.MusicPacks)
            {
                __instance._gridView._columnCount = _originalColumnCount;

                // Restore the removed column since we don't want to show an empty cell.
                if (_isGridResized)
                {
                    __instance._gridView._visibleColumnCount += 1;

                    var rectTransform = (RectTransform)__instance._gridView.transform;
                    rectTransform.sizeDelta += new Vector2(__instance._cellWidth, 0);
                    rectTransform.anchoredPosition += new Vector2(__instance._cellWidth / 2, 0);

                    _isGridResized = false;
                }
            }
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.GetContentXOffset))]
        private void RecalculateContentXOffsetBasedOnColumnCount(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance, ref float __result)
        {
            if (_annotatedBeatmapLevelCollectionsViewController._annotatedBeatmapLevelCollectionsGridView._annotatedBeatmapLevelCollections == null)
            {
                return;
            }

            if (_annotatedBeatmapLevelCollectionsViewController._annotatedBeatmapLevelCollectionsGridView._annotatedBeatmapLevelCollections.Count <= __instance._visibleColumnCount)
            {
                __result = __instance._columnWidth;

                return;
            }

            var zeroOffset = (__instance._columnCount - 1) / 2f;
            var maxMove = (__instance._columnCount - __instance._visibleColumnCount) / 2f;
            var toMove = zeroOffset - __instance._selectedColumn;
            if (__instance._visibleColumnCount % 2 == 0)
            {
                toMove -= 0.5f;
            }

            __result = Math.Clamp(toMove, -maxMove, maxMove) * __instance._columnWidth;
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.AnimateOpen))]
        private void RecalculateSizeBasedOnColumnCount(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance, bool animated)
        {
            var x = ((__instance._columnCount - __instance._visibleColumnCount) * 2 + __instance._visibleColumnCount) * __instance._columnWidth;
            if (animated)
            {
                __instance._viewportSizeTween.toValue = new Vector2(x, __instance._viewportSizeTween.toValue.y);
            }
            else
            {
                __instance._viewportTransform.sizeDelta = new Vector2(x, __instance._viewportTransform.sizeDelta.y);
            }

            if (_isGridResized)
            {
                // It would otherwise fly away when setting the Screen size.
                var rectTransform = (RectTransform)_selectLevelCategoryViewController.transform;

                if (rectTransform.anchorMin.x == 0 || rectTransform.anchorMax.x == 0)
                {
                    var localPosition = rectTransform.localPosition;
                    rectTransform.anchorMin = new Vector2(0.5f, rectTransform.anchorMin.y);
                    rectTransform.anchorMax = new Vector2(0.5f, rectTransform.anchorMax.y);
                    rectTransform.localPosition = localPosition;
                }

                rectTransform = (RectTransform)_mainFlowCoordinator._screenSystem.mainScreen.transform;

                if (_originalScreenSize == default)
                {
                    _originalScreenSize = rectTransform.sizeDelta;
                }

                // Resizing Screen is needed to allow the hover hint to be shown when the GridView is larger.
                rectTransform.sizeDelta = new Vector2(_originalScreenSize.x + (__instance._columnCount - __instance._visibleColumnCount - 1) * __instance._columnWidth * 2, _originalScreenSize.y);

                _isScreenResized = true;
            }
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.AnimateClose))]
        private void RestoreScreenSize(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance)
        {
            if (_isScreenResized)
            {
                var rectTransform = (RectTransform)_mainFlowCoordinator._screenSystem.mainScreen.transform;
                rectTransform.sizeDelta = _originalScreenSize;

                _isScreenResized = false;
            }
        }
    }
}
#endif
