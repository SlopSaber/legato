#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
using System.Collections.Generic;
using UnityEngine;
using Vivify.ObjectPrefab.Pools;
using Xft;

namespace Vivify.ObjectPrefab;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
internal sealed class FollowedSaberTrail : MonoBehaviour
{
    private static readonly int _color = Shader.PropertyToID("_Color");

    private readonly List<BladeSample> _samples = new();
    private Mesh _mesh = null!;
    private MeshRenderer _renderer = null!;
    private MaterialPropertyBlock _properties = null!;
    private XWeaponTrail? _source;
    private Transform? _sampleSpace;
    private Transform _bladeBase = null!;
    private Transform _orientation = null!;
    private Vector3 _top;
    private Vector3 _bottom;
    private float _duration;
    private float _sampleInterval;
    private Vector3[] _vertices = [];
    private Color[] _vertexColors = [];

    internal Material Material { get; set; } = null!;

    internal void InitProperties(TrailProperties properties)
    {
        _top = properties.TopPos ?? Vector3.forward;
        _bottom = properties.BottomPos ?? Vector3.zero;
        _duration = Mathf.Max(0.001f, properties.Duration ?? 0.4f);
        _sampleInterval = 1f / Mathf.Max(1, properties.SamplingFrequency ?? 50);
        int rows = Mathf.Max(2, properties.Granularity ?? 60);
        if (_vertices.Length == rows * 3)
        {
            return;
        }

        _vertices = new Vector3[rows * 3];
        _vertexColors = new Color[rows * 3];
        _mesh.Clear();
        if (_vertices.Length > ushort.MaxValue)
        {
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        var uv = new Vector2[_vertices.Length];
        var triangles = new int[(rows - 1) * 12];
        for (int row = 0; row < rows; row++)
        {
            int index = row * 3;
            float age = row / (rows - 1f);
            uv[index] = new Vector2(0f, age);
            uv[index + 1] = new Vector2(0.5f, age);
            uv[index + 2] = new Vector2(1f, age);
            if (row == rows - 1)
            {
                continue;
            }

            int offset = row * 12;
            triangles[offset] = index + 3;
            triangles[offset + 1] = index + 4;
            triangles[offset + 2] = index;
            triangles[offset + 3] = index + 4;
            triangles[offset + 4] = index + 1;
            triangles[offset + 5] = index;
            triangles[offset + 6] = index + 4;
            triangles[offset + 7] = index + 5;
            triangles[offset + 8] = index + 1;
            triangles[offset + 9] = index + 5;
            triangles[offset + 10] = index + 2;
            triangles[offset + 11] = index + 1;
        }

        _mesh.vertices = _vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
    }

    internal void Init(XWeaponTrail source, Transform parent)
    {
        _source = source;
        XWeaponTrailRenderer? renderSource = source._trailRenderer;
        _sampleSpace = renderSource != null ? renderSource.transform.parent : null;
        gameObject.layer = renderSource != null ? renderSource.gameObject.layer : source.gameObject.layer;
        Saber? saber = source.GetComponentInParent<Saber>();
        _bladeBase = saber != null ? saber._bottomPos : source._pointEnd;
        _orientation = parent;
        _renderer.sharedMaterial = Material;
        _samples.Clear();
    }

    private void Awake()
    {
        _mesh = new Mesh { name = "Vivify saber trail" };
        _mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = _mesh;
        _renderer = GetComponent<MeshRenderer>();
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _properties = new MaterialPropertyBlock();
        _renderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (_source == null || !_source.isActiveAndEnabled || _vertices.Length == 0)
        {
            _renderer.enabled = false;
            _samples.Clear();
            return;
        }

        float now = Time.time;
        Vector3 origin = _bladeBase.position;
        Vector3 top = origin + _orientation.TransformVector(_top);
        Vector3 bottom = origin + _orientation.TransformVector(_bottom);
        BladeSample current = _sampleSpace == null
            ? new BladeSample(now, top, bottom)
            : new BladeSample(now, _sampleSpace.InverseTransformPoint(top), _sampleSpace.InverseTransformPoint(bottom));

        if (_samples.Count == 0 || now - _samples[_samples.Count - 1].Time >= _sampleInterval)
        {
            _samples.Add(current);
        }

        float cutoff = now - _duration;
        int expired = 0;
        while (expired + 1 < _samples.Count && _samples[expired + 1].Time < cutoff)
        {
            expired++;
        }

        if (expired != 0)
        {
            _samples.RemoveRange(0, expired);
        }

        DrawTrail(current, _source.color);
    }

    private void OnDestroy() => Destroy(_mesh);

    private void DrawTrail(BladeSample current, Color color)
    {
        int cursor = _samples.Count - 1;
        BladeSample newer = current;
        int rows = _vertices.Length / 3;
        for (int row = 0; row < rows; row++)
        {
            float time = current.Time - (_duration * row / (rows - 1f));
            while (cursor > 0 && _samples[cursor].Time > time)
            {
                newer = _samples[cursor--];
            }

            BladeSample older = _samples[cursor];
            float weight = Mathf.InverseLerp(older.Time, newer.Time, time);
            Vector3 top = ToMeshSpace(Vector3.Lerp(older.Top, newer.Top, weight));
            Vector3 bottom = ToMeshSpace(Vector3.Lerp(older.Bottom, newer.Bottom, weight));
            int index = row * 3;
            _vertices[index] = top;
            _vertices[index + 1] = (top + bottom) * 0.5f;
            _vertices[index + 2] = bottom;
            _vertexColors[index] = color;
            _vertexColors[index + 1] = color;
            _vertexColors[index + 2] = color;
        }

        _mesh.vertices = _vertices;
        _mesh.colors = _vertexColors;
        _mesh.RecalculateBounds();
        _properties.SetColor(_color, color);
        _renderer.SetPropertyBlock(_properties);
        _renderer.enabled = true;
    }

    private Vector3 ToMeshSpace(Vector3 position)
    {
        Vector3 world = _sampleSpace == null ? position : _sampleSpace.TransformPoint(position);
        return transform.InverseTransformPoint(world);
    }

    private readonly struct BladeSample
    {
        internal BladeSample(float time, Vector3 top, Vector3 bottom)
        {
            Time = time;
            Top = top;
            Bottom = bottom;
        }

        internal float Time { get; }

        internal Vector3 Top { get; }

        internal Vector3 Bottom { get; }
    }
}
#endif
