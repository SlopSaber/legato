#if BEAT_SABER_1_29_0
using System;
using System.Collections.Generic;
using SiraUtil.Affinity;
using UnityEngine;

namespace PlaylistManager.AffinityPatches {
    internal class AnnotatedBeatmapLevelCollectionsUIPatches : IAffinity {
        private const int VisibleColumns = 5;
        private const int MaximumRows = 5;
        private readonly MainFlowCoordinator _mainFlowCoordinator;
        private readonly AnnotatedBeatmapLevelCollectionsViewController _collectionsViewController;
        private readonly SelectLevelCategoryViewController _categoryViewController;
        private AnnotatedBeatmapLevelCollectionsGridView _collectionsGrid;
        private HorizontalGeometry _viewportGeometry;
        private HorizontalGeometry _contentGeometry;
        private HorizontalGeometry _viewGeometry;
        private HorizontalGeometry _categoryGeometry;
        private Vector2 _screenSize;
        private bool _captured;
        private bool _customSongs;
        private bool _screenExpanded;
        private int _columns;

        private AnnotatedBeatmapLevelCollectionsUIPatches(MainFlowCoordinator mainFlowCoordinator,
            AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController,
            SelectLevelCategoryViewController selectLevelCategoryViewController) {
            _mainFlowCoordinator = mainFlowCoordinator;
            _collectionsViewController = annotatedBeatmapLevelCollectionsViewController;
            _categoryViewController = selectLevelCategoryViewController;
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridView), nameof(AnnotatedBeatmapLevelCollectionsGridView.SetData))]
        [AffinityPrefix]
        private void ResizeGrid(AnnotatedBeatmapLevelCollectionsGridView __instance,
            IReadOnlyList<NativeSongCollection> annotatedBeatmapLevelCollections) {
            _collectionsGrid = __instance;
            var animator = __instance._animator;
            var view = (RectTransform)_collectionsViewController.transform;
            var category = (RectTransform)_categoryViewController.transform;
            if (!_captured) {
                _viewportGeometry = new HorizontalGeometry(animator._viewportTransform);
                _contentGeometry = new HorizontalGeometry(animator._contentTransform);
                _viewGeometry = new HorizontalGeometry(view);
                _categoryGeometry = new HorizontalGeometry(category);
                _captured = true;
            }
            RestoreScreenSize();
            _viewGeometry.Restore(view);
            _categoryGeometry.Restore(category);
            _viewportGeometry.Restore(animator._viewportTransform);
            _contentGeometry.Restore(animator._contentTransform);
            _customSongs = _categoryViewController.selectedLevelCategory == SelectLevelCategoryViewController.LevelCategory.CustomSongs;
            if (!_customSongs) return;

            _columns = Math.Max(VisibleColumns, Mathf.CeilToInt((annotatedBeatmapLevelCollections?.Count ?? 0) / (float)MaximumRows));
            FixHorizontalAnchors(view);
            FixHorizontalAnchors(category);
            var viewport = animator._viewportTransform;
            FixHorizontalAnchors(viewport);
            var oldWidth = viewport.rect.width;
            var position = viewport.localPosition;
            // Reserve the native category-control slot before the five playlist slots.
            var left = position.x - oldWidth * viewport.pivot.x * viewport.localScale.x
                + __instance._cellWidth * viewport.localScale.x;
            viewport.pivot = new Vector2(0.5f, viewport.pivot.y);
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, VisibleColumns * __instance._cellWidth);
            position.x = left + viewport.rect.width * viewport.pivot.x * viewport.localScale.x;
            viewport.localPosition = position;

            // GridView.ReloadData derives its columns from content width on this native API.
            FixHorizontalAnchors(animator._contentTransform);
            animator._contentTransform.pivot = new Vector2(0.5f, animator._contentTransform.pivot.y);
            animator._contentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _columns * __instance._cellWidth);
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.Init))]
        [AffinityPostfix]
        private void InitializeViewport(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance) {
            if (!Owns(__instance)) return;
            SetHorizontalPosition(__instance);
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.ScrollToRowIdxInstant))]
        [AffinityPostfix]
        private void ScrollToSelection(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance) {
            if (Owns(__instance)) SetHorizontalPosition(__instance);
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.AnimateOpen))]
        [AffinityPostfix]
        private void ExpandViewport(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance, bool animated) {
            if (!Owns(__instance)) return;
            SetTweenTargets(__instance, (_columns * 2 - VisibleColumns) * _collectionsGrid._cellWidth, animated);
            var screen = (RectTransform)_mainFlowCoordinator._screenSystem.mainScreen.transform;
            if (!_screenExpanded) _screenSize = screen.sizeDelta;
            screen.sizeDelta = new Vector2(_screenSize.x + Math.Max(0, _columns - VisibleColumns) * _collectionsGrid._cellWidth * 2, _screenSize.y);
            _screenExpanded = true;
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.AnimateClose))]
        [AffinityPostfix]
        private void CollapseViewport(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance, bool animated) {
            if (!Owns(__instance)) return;
            SetTweenTargets(__instance, VisibleColumns * _collectionsGrid._cellWidth, animated);
            RestoreScreenSize();
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsViewController), nameof(AnnotatedBeatmapLevelCollectionsViewController.DidDeactivate))]
        [AffinityPrefix]
        private void RestoreScreenSize() {
            if (!_screenExpanded) return;
            ((RectTransform)_mainFlowCoordinator._screenSystem.mainScreen.transform).sizeDelta = _screenSize;
            _screenExpanded = false;
        }

        private bool Owns(AnnotatedBeatmapLevelCollectionsGridViewAnimator animator) =>
            _customSongs && _collectionsGrid != null && animator == _collectionsGrid._animator;

        private float ContentXOffset() {
            var selectedColumn = Math.Max(0, _collectionsGrid._selectedCellIndex) % _columns;
            var maximumMove = (_columns - VisibleColumns) / 2f;
            return Mathf.Clamp((_columns - 1) / 2f - selectedColumn, -maximumMove, maximumMove) * _collectionsGrid._cellWidth;
        }

        private void SetHorizontalPosition(AnnotatedBeatmapLevelCollectionsGridViewAnimator animator) {
            var position = animator._contentTransform.anchoredPosition;
            position.x = ContentXOffset();
            animator._contentTransform.anchoredPosition = position;
        }

        private void SetTweenTargets(AnnotatedBeatmapLevelCollectionsGridViewAnimator animator, float width, bool animated) {
            if (animated && animator._viewportSizeTween != null && animator._contentPositionTween != null) {
                var size = animator._viewportSizeTween.toValue;
                size.x = width;
                animator._viewportSizeTween.toValue = size;
                var position = animator._contentPositionTween.toValue;
                position.x = ContentXOffset();
                animator._contentPositionTween.toValue = position;
            } else {
                animator._viewportTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                SetHorizontalPosition(animator);
            }
        }

        private static void FixHorizontalAnchors(RectTransform rect) {
            var position = rect.localPosition;
            var width = rect.rect.width;
            rect.anchorMin = new Vector2(0.5f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(0.5f, rect.anchorMax.y);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.localPosition = position;
        }

        private readonly struct HorizontalGeometry {
            private readonly float _anchorMin;
            private readonly float _anchorMax;
            private readonly float _sizeDelta;
            private readonly float _position;
            private readonly float _pivot;

            internal HorizontalGeometry(RectTransform rect) {
                _anchorMin = rect.anchorMin.x;
                _anchorMax = rect.anchorMax.x;
                _sizeDelta = rect.sizeDelta.x;
                _position = rect.anchoredPosition.x;
                _pivot = rect.pivot.x;
            }

            internal void Restore(RectTransform rect) {
                rect.anchorMin = new Vector2(_anchorMin, rect.anchorMin.y);
                rect.anchorMax = new Vector2(_anchorMax, rect.anchorMax.y);
                rect.sizeDelta = new Vector2(_sizeDelta, rect.sizeDelta.y);
                rect.pivot = new Vector2(_pivot, rect.pivot.y);
                rect.anchoredPosition = new Vector2(_position, rect.anchoredPosition.y);
            }
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
