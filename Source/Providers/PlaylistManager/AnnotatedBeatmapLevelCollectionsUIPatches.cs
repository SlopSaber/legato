#if BEAT_SABER_1_29_0
using System;
using System.Collections.Generic;
using SiraUtil.Affinity;
using UnityEngine;

namespace PlaylistManager.AffinityPatches {
    internal class AnnotatedBeatmapLevelCollectionsUIPatches : IAffinity {
        private const int VisibleColumns = 5;
        private const int MaximumRows = 5;
        private readonly SelectLevelCategoryViewController _categoryViewController;
        private readonly PlaylistManager.UI.PlaylistViewButtonsController _playlistButtons;
        private AnnotatedBeatmapLevelCollectionsGridView _collectionsGrid;
        private HorizontalGeometry _viewportGeometry;
        private HorizontalGeometry _contentGeometry;
        private bool _customSongs;
        private bool _layoutApplied;
        private int _columns;
        private float _viewportLeft;

        private AnnotatedBeatmapLevelCollectionsUIPatches(SelectLevelCategoryViewController selectLevelCategoryViewController,
            PlaylistManager.UI.PlaylistViewButtonsController playlistButtons) {
            _categoryViewController = selectLevelCategoryViewController;
            _playlistButtons = playlistButtons;
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridView), nameof(AnnotatedBeatmapLevelCollectionsGridView.SetData))]
        [AffinityPrefix]
        private void ResizeGrid(AnnotatedBeatmapLevelCollectionsGridView __instance,
            IReadOnlyList<NativeSongCollection> annotatedBeatmapLevelCollections) {
            RestoreLayout();
            _collectionsGrid = __instance;
            _customSongs = _categoryViewController.selectedLevelCategory == SelectLevelCategoryViewController.LevelCategory.CustomSongs;
            if (!_customSongs) return;

            _columns = Math.Max(VisibleColumns, Mathf.CeilToInt((annotatedBeatmapLevelCollections?.Count ?? 0) / (float)MaximumRows));
            ApplyLayout();
        }

        private void ApplyLayout() {
            var animator = _collectionsGrid._animator;
            var viewport = animator._viewportTransform;
            _viewportGeometry = new HorizontalGeometry(viewport);
            _contentGeometry = new HorizontalGeometry(animator._contentTransform);
            _layoutApplied = true;
            FixHorizontalAnchors(viewport);
            _viewportLeft = viewport.localPosition.x - viewport.rect.width * viewport.pivot.x * viewport.localScale.x;
            viewport.pivot = new Vector2(0.5f, viewport.pivot.y);
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, VisibleColumns * _collectionsGrid._cellWidth);
            PositionCollapsedViewport(viewport);

            // GridView.ReloadData derives its columns from content width on this native API.
            FixHorizontalAnchors(animator._contentTransform);
            animator._contentTransform.pivot = new Vector2(0.5f, animator._contentTransform.pivot.y);
            animator._contentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _columns * _collectionsGrid._cellWidth);
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.Init))]
        [AffinityPostfix]
        private void InitializeViewport(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance) {
            if (!Owns(__instance)) return;
            SetHorizontalPosition(__instance);
            PositionCollapsedViewport(__instance._viewportTransform);
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
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), nameof(AnnotatedBeatmapLevelCollectionsGridViewAnimator.AnimateClose))]
        [AffinityPostfix]
        private void CollapseViewport(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance, bool animated) {
            if (!Owns(__instance)) return;
            SetTweenTargets(__instance, VisibleColumns * _collectionsGrid._cellWidth, animated);
            PositionCollapsedViewport(__instance._viewportTransform);
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsViewController), nameof(AnnotatedBeatmapLevelCollectionsViewController.DidActivate))]
        [AffinityPostfix]
        private void RestoreCollapsedViewport(AnnotatedBeatmapLevelCollectionsViewController __instance) {
            if (!_customSongs || _collectionsGrid == null || _collectionsGrid != __instance._annotatedBeatmapLevelCollectionsGridView) return;
            if (!_layoutApplied) ApplyLayout();
            var animator = _collectionsGrid._animator;
            animator.AnimateClose(_collectionsGrid._selectedCellIndex / _columns, false);
        }

        [AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsViewController), nameof(AnnotatedBeatmapLevelCollectionsViewController.DidDeactivate))]
        [AffinityPrefix]
        private void ReleaseViewport(AnnotatedBeatmapLevelCollectionsViewController __instance) {
            if (_collectionsGrid != null && _collectionsGrid == __instance._annotatedBeatmapLevelCollectionsGridView) RestoreLayout();
        }

        private void RestoreLayout() {
            if (!_layoutApplied || _collectionsGrid == null) return;
            _layoutApplied = false;
            var animator = _collectionsGrid._animator;
            animator.AnimateClose(Math.Max(0, _collectionsGrid._selectedCellIndex) / _columns, false);
            _viewportGeometry.Restore(animator._viewportTransform);
            _contentGeometry.Restore(animator._contentTransform);
        }

        private bool Owns(AnnotatedBeatmapLevelCollectionsGridViewAnimator animator) =>
            _layoutApplied && _collectionsGrid != null && animator == _collectionsGrid._animator;

        private void PositionCollapsedViewport(RectTransform viewport) {
            var cell = _collectionsGrid._cellWidth * viewport.localScale.x;
            var halfWidth = VisibleColumns * cell / 2f;
            var position = viewport.localPosition;
            position.x = _viewportLeft + halfWidth;
            if (_playlistButtons.TryGetActionLeftEdge(viewport.parent, out var actionsLeft))
                position.x = actionsLeft - halfWidth - cell * 0.08f;
            viewport.localPosition = position;
        }

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
