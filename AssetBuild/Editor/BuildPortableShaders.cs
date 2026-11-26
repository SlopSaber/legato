using System;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

namespace Legato.AssetBuild
{
    public static class BuildPortableShaders
    {
        [Serializable]
        private sealed class Settings
        {
            public string bundleName = "";
            public string outputDirectory = "";
            public string[] shaders = Array.Empty<string>();
            public bool singlePassStereo = false;
        }

        public static void Build()
        {
            var settings = JsonUtility.FromJson<Settings>(File.ReadAllText("legato-shaders.json"));
#if UNITY_6000_0_OR_NEWER
            Directory.CreateDirectory("Assets/Generated");
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Generated/Renderer.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/Generated/Renderer.asset");
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Generated/Pipeline.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, "Assets/Generated/Pipeline.asset");
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            var stripping = GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
            stripping.stripUnusedVariants = false;
            stripping.stripUnusedPostProcessingVariants = false;
#else
            GraphicsSettings.renderPipelineAsset = null;
            QualitySettings.renderPipeline = null;
#endif
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            graphics.FindProperty("m_InstancingStripping").intValue = 2;
            graphics.ApplyModifiedPropertiesWithoutUndo();
#pragma warning disable CS0618
            PlayerSettings.virtualRealitySupported = true;
            PlayerSettings.stereoRenderingPath = settings.singlePassStereo ? StereoRenderingPath.SinglePass : StereoRenderingPath.Instancing;
#pragma warning restore CS0618
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            AssetDatabase.SaveAssets();
            foreach (string path in settings.shaders) CheckShader(path);
            Directory.CreateDirectory(settings.outputDirectory);
            var builds = new[] { new AssetBundleBuild { assetBundleName = settings.bundleName, assetNames = settings.shaders } };
            if (BuildPipeline.BuildAssetBundles(settings.outputDirectory, builds,
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
                BuildTarget.StandaloneWindows64) == null)
                throw new InvalidOperationException("Portable shader bundle build failed.");
            foreach (string path in settings.shaders) CheckShader(path);
            Debug.Log("Legato portable shader bundle built: " + settings.bundleName + " (" + settings.shaders.Length + " shaders)");
        }

        private static void CheckShader(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null) throw new InvalidOperationException("Missing shader: " + path);
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                if (message.severity == ShaderCompilerMessageSeverity.Error)
                    throw new InvalidOperationException(path + ": " + message.message);
            if (ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Shader compilation failed: " + path);
        }
    }
}
