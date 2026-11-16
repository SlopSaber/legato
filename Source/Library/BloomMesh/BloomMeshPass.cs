using UnityEngine;
using UnityEngine.Rendering;

namespace Legato.Rendering {
    public abstract class BloomMeshPass : BloomPrePassNonLightPass {
        protected abstract bool TryGetDraw(Matrix4x4 view, out Mesh mesh, out Matrix4x4 model, out Material material, out MaterialPropertyBlock properties);
#if BEAT_SABER_1_29_0
        private CommandBuffer _commands;
        public override void Render(RenderTexture destination, Matrix4x4 view, Matrix4x4 projection) {
            if (!TryGetDraw(view, out var mesh, out var model, out var material, out var properties)) return;
            if (_commands == null) _commands = new CommandBuffer { name = "Bloom mesh" };
            _commands.Clear();
            _commands.SetRenderTarget(destination);
            _commands.SetViewProjectionMatrices(view, projection);
            _commands.DrawMesh(mesh, model, material, 0, 1, properties);
            Graphics.ExecuteCommandBuffer(_commands);
        }
        private void OnDestroy() { _commands?.Release(); _commands = null; }
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
