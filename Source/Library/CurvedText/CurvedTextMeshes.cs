using System;
using HMUI;
using UnityEngine;

namespace Legato.UI {
    internal static class CurvedTextMeshes {
        internal static void Refresh(CurvedTextMeshPro text, float radius) {
            var curve = new Vector2(radius, 0);
#if !BEAT_SABER_1_29_0
            if (text._curvedMeshInfos == null || text.m_textInfo.materialCount > text._curvedMeshInfos.Length)
                Array.Resize(ref text._curvedMeshInfos, text.m_textInfo.materialCount);
#endif
            RefreshMesh(text, text.m_mesh, 0, curve);
            text.canvasRenderer.SetMesh(text.m_mesh);
            for (int i = 1; i < text.m_textInfo.materialCount; i++) {
                if (text.m_subTextObjects[i] == null) continue;
                var mesh = text.m_textInfo.meshInfo[i].mesh;
                RefreshMesh(text, mesh, i, curve);
                text.m_subTextObjects[i].canvasRenderer.SetMesh(mesh);
            }
        }
        private static void RefreshMesh(CurvedTextMeshPro text, Mesh mesh, int index, Vector2 curve) {
#if BEAT_SABER_1_29_0
            var coordinates = new Vector2[mesh.vertexCount];
            for (int i = 0; i < coordinates.Length; i++) coordinates[i] = curve;
            mesh.uv3 = coordinates;
            if (text.useScriptableObjectColors) {
                var colors = mesh.colors;
                for (int i = 0; i < colors.Length; i++) colors[i] = text.color;
                mesh.colors = colors;
            }
#else
            text.UpdateMesh(mesh, index, curve, text.color);
#endif
        }
    }
}
