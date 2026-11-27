using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Legato.Unity
{
    internal sealed class SlicedImage : Image
    {
        private readonly Vector2[] corners = new Vector2[4];
        private readonly Vector2[] uvs = new Vector2[4];
        private float pixelsPerUnitMultiplier = 1f;

        internal float PixelsPerUnitMultiplier
        {
            get => pixelsPerUnitMultiplier;
            set
            {
                pixelsPerUnitMultiplier = Mathf.Max(0.01f, value);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            Sprite activeSprite = overrideSprite;
            if (type != Type.Sliced || activeSprite == null || activeSprite.border == Vector4.zero || pixelsPerUnitMultiplier == 1f)
            {
                base.OnPopulateMesh(mesh);
                return;
            }

            Rect area = GetPixelAdjustedRect();
            float scale = pixelsPerUnit * pixelsPerUnitMultiplier;
            Vector4 border = activeSprite.border / scale;
            Vector4 padding = DataUtility.GetPadding(activeSprite) / scale;
            Vector2 unadjustedSize = rectTransform.rect.size;
            FitBorders(ref border.x, ref border.z, unadjustedSize.x, area.width);
            FitBorders(ref border.y, ref border.w, unadjustedSize.y, area.height);

            corners[0] = area.min + new Vector2(padding.x, padding.y);
            corners[1] = area.min + new Vector2(border.x, border.y);
            corners[2] = area.max - new Vector2(border.z, border.w);
            corners[3] = area.max - new Vector2(padding.z, padding.w);

            Vector4 outer = DataUtility.GetOuterUV(activeSprite);
            Vector4 inner = DataUtility.GetInnerUV(activeSprite);
            uvs[0] = new Vector2(outer.x, outer.y);
            uvs[1] = new Vector2(inner.x, inner.y);
            uvs[2] = new Vector2(inner.z, inner.w);
            uvs[3] = new Vector2(outer.z, outer.w);

            mesh.Clear();
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (fillCenter || x != 1 || y != 1)
                    {
                        AddQuad(mesh, x, y);
                    }
                }
            }
        }

        private static void FitBorders(ref float start, ref float end, float originalSize, float size)
        {
            float ratio = originalSize == 0f ? 1f : size / originalSize;
            float sum = (start + end) * ratio;
            if (sum > size && sum != 0f)
            {
                ratio *= size / sum;
            }

            start *= ratio;
            end *= ratio;
        }

        private void AddQuad(VertexHelper mesh, int x, int y)
        {
            int index = mesh.currentVertCount;
            Color32 tint = color;
            mesh.AddVert(new Vector3(corners[x].x, corners[y].y), tint, new Vector2(uvs[x].x, uvs[y].y));
            mesh.AddVert(new Vector3(corners[x].x, corners[y + 1].y), tint, new Vector2(uvs[x].x, uvs[y + 1].y));
            mesh.AddVert(new Vector3(corners[x + 1].x, corners[y + 1].y), tint, new Vector2(uvs[x + 1].x, uvs[y + 1].y));
            mesh.AddVert(new Vector3(corners[x + 1].x, corners[y].y), tint, new Vector2(uvs[x + 1].x, uvs[y].y));
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index + 2, index + 3, index);
        }
    }
}
