#if LEGATO_BEFORE_MULTIPLAYER
global using ReactiveGradientDirection = Reactive.BeatSaber.Components.LegacyGradientDirection;
#else
global using ReactiveGradientDirection = HMUI.ImageView.GradientDirection;
#endif

#if LEGATO_BEFORE_MULTIPLAYER
using UnityEngine;
using UnityEngine.UI;

namespace Reactive.BeatSaber.Components;

public enum LegacyGradientDirection {
    Horizontal,
    Vertical
}

internal sealed class FixedImageView : UnityEngine.UI.Image {
    private Color _color0 = Color.white;
    private Color _color1 = Color.white;
    private bool _gradient;
    private float _skew;
    private LegacyGradientDirection _direction;

    public Color color0 {
        get => _color0;
        set {
            _color0 = value;
            SetVerticesDirty();
        }
    }

    public Color color1 {
        get => _color1;
        set {
            _color1 = value;
            SetVerticesDirty();
        }
    }

    public bool gradient {
        get => _gradient;
        set {
            _gradient = value;
            SetVerticesDirty();
        }
    }

    public float Skew {
        get => _skew;
        set {
            _skew = value;
            SetVerticesDirty();
        }
    }

    public LegacyGradientDirection GradientDirection {
        get => _direction;
        set {
            _direction = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper mesh) {
        base.OnPopulateMesh(mesh);
        if (!gradient && Mathf.Approximately(Skew, 0))
            return;

        Rect rect = GetPixelAdjustedRect();
        bool horizontal = GradientDirection == LegacyGradientDirection.Horizontal;
        UIVertex vertex = default;
        for (int i = 0; i < mesh.currentVertCount; i++) {
            mesh.PopulateUIVertex(ref vertex, i);
            if (gradient) {
                float progress = horizontal
                    ? Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x)
                    : Mathf.InverseLerp(rect.yMax, rect.yMin, vertex.position.y);
                vertex.color = (Color)vertex.color * Color.Lerp(color0, color1, progress);
            }
            vertex.position.x += Skew * (vertex.position.y - rect.center.y);
            mesh.SetUIVertex(vertex, i);
        }
    }
}
#endif
