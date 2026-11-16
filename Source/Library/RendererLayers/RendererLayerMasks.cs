#nullable enable
using System;
using HarmonyLib;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Legato.Rendering {
    internal static class RendererLayerMasks {
        internal static void Apply(int additionalLayers, int transparentGroupLayers,
            Action<string> onGrouped, Action onFallback, Action onMissingRenderers) {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset pipeline) return;
            var renderers = Traverse.Create(pipeline).Field<ScriptableRendererData[]>("m_RendererDataList").Value;
            if (renderers == null) {
                onMissingRenderers();
                return;
            }
            foreach (var data in renderers) {
                if (data is not UniversalRendererData renderer) continue;
                if (TryFindTransparentGroup(data, transparentGroupLayers, out var feature)) {
                    feature.settings.filterSettings.LayerMask |= additionalLayers;
                    onGrouped(feature.name);
                } else {
                    renderer.transparentLayerMask |= additionalLayers;
                    onFallback();
                }
                renderer.prepassLayerMask |= additionalLayers;
                renderer.opaqueLayerMask |= additionalLayers;
            }
        }

        private static bool TryFindTransparentGroup(ScriptableRendererData data, int groupLayers, out RenderObjects feature) {
            foreach (var candidate in data.rendererFeatures) {
                if (candidate is not RenderObjects objects) continue;
                var filter = objects.settings.filterSettings;
                if (filter.RenderQueueType != RenderQueueType.Transparent || (filter.LayerMask & groupLayers) == 0) continue;
                feature = objects;
                return true;
            }
            feature = null!;
            return false;
        }
    }
}
