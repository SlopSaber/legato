#nullable enable
#if LEGATO_BEFORE_MULTIPLAYER
using System.Collections.Generic;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace Legato.Rendering {
    public abstract class BloomMeshPass :
#if LEGATO_BEFORE_MULTIPLAYER
        MonoBehaviour {
        private static readonly HashSet<BloomMeshPass> ActivePasses = new();

        private void OnEnable() => ActivePasses.Add(this);
        private void OnDisable() => ActivePasses.Remove(this);

        public static void RenderBeforeBlur(Matrix4x4 view, Matrix4x4 projection) {
            var destination = RenderTexture.active;
            if (destination == null) return;
            foreach (var pass in ActivePasses) pass.Render(destination, view, projection);
        }
#else
        BloomPrePassNonLightPass {
#endif
        protected abstract bool TryGetDraw(Matrix4x4 view, out Mesh mesh, out Matrix4x4 model, out Material material, out MaterialPropertyBlock properties);
#if LEGATO_BLOOM_RENDER_TEXTURE
        private CommandBuffer? _commands;
        private Material? _singlePassMaterial;
        private Material? _sourceMaterial;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int BloomStrength = Shader.PropertyToID("_BloomStrength");
#if LEGATO_BEFORE_MULTIPLAYER
        private void Render(RenderTexture destination, Matrix4x4 view, Matrix4x4 projection) {
#else
        public override void Render(RenderTexture destination, Matrix4x4 view, Matrix4x4 projection) {
#endif
            if (!TryGetDraw(view, out var mesh, out var model, out var material, out var properties)) return;
            if (_commands == null) _commands = new CommandBuffer { name = "Bloom mesh" };
            _commands.Clear();
            _commands.SetRenderTarget(destination);
            _commands.SetViewProjectionMatrices(view, projection);
            int pass = 1;
            if (material.passCount == 1) {
                if (_sourceMaterial != material || _singlePassMaterial == null) {
                    if (_singlePassMaterial != null) Destroy(_singlePassMaterial);
                    _singlePassMaterial = new Material(material);
                    _sourceMaterial = material;
                    _singlePassMaterial.SetInt("_SrcColor", (int)BlendMode.One);
                    _singlePassMaterial.SetInt("_DestColor", (int)BlendMode.One);
                    _singlePassMaterial.SetInt("_SrcAlpha", (int)BlendMode.Zero);
                    _singlePassMaterial.SetInt("_DestAlpha", (int)BlendMode.One);
                }
                properties.SetColor(ColorProperty, material.color * properties.GetFloat(BloomStrength));
                material = _singlePassMaterial;
                pass = 0;
            }
            _commands.DrawMesh(mesh, model, material, 0, pass, properties);
            Graphics.ExecuteCommandBuffer(_commands);
        }
        private void OnDestroy() {
#if LEGATO_BEFORE_MULTIPLAYER
            ActivePasses.Remove(this);
#endif
            _commands?.Release();
            _commands = null;
            if (_singlePassMaterial != null) Destroy(_singlePassMaterial);
            _singlePassMaterial = null;
            _sourceMaterial = null;
        }
#else
        public override void Render(CommandBuffer commands, RTHandle destination, Matrix4x4 view, Matrix4x4 projection) {
            if (!TryGetDraw(view, out var mesh, out var model, out var material, out var properties)) return;
            CoreUtils.SetRenderTarget(commands, destination);
            commands.SetViewProjectionMatrices(view, projection);
            commands.DrawMesh(mesh, model, material, 0, 1, properties);
        }
#endif
    }
}
