#if BEAT_SABER_1_29_0
global using MirrorQuality = Legato.Rendering.LegacyMirrorQuality;
#else
global using MirrorQuality = BeatSaber.Settings.QualitySettings.MirrorQuality;
#endif

using System;
#if BEAT_SABER_1_29_0
using System.Linq;
using UnityEngine;
#endif

namespace Legato.Rendering
{
#if BEAT_SABER_1_29_0
    public enum LegacyMirrorQuality { Off = 0, Low = 1, Medium = 2, High = 3 }
#endif

    internal sealed class MirrorQualitySettings
    {
#if BEAT_SABER_1_29_0
        private readonly MainSettingsModelSO _settings = FindResource<MainSettingsModelSO>();
        private readonly MirrorRendererSO _renderer = FindResource<MirrorRendererSO>();
        private readonly MirrorRendererGraphicsSettingsPresets _presets = FindResource<MirrorRendererGraphicsSettingsPresets>();

        private static T FindResource<T>() where T : UnityEngine.Object => UnityEngine.Resources.FindObjectsOfTypeAll<T>().FirstOrDefault()
            ?? throw new InvalidOperationException("Native mirror resource is unavailable: " + typeof(T).FullName);

        internal MirrorQuality Current => (MirrorQuality)_settings.mirrorGraphicsSettings.value;

        internal void Apply(MirrorQuality quality)
        {
            int index = (int)quality;
            if ((uint)index >= (uint)_presets.presets.Length) throw new ArgumentOutOfRangeException(nameof(quality));
            var preset = _presets.presets[index];
            _settings.mirrorGraphicsSettings.value = index;
            _renderer.Init(preset.reflectLayers, preset.stereoTextureWidth, preset.stereoTextureHeight,
                preset.monoTextureWidth, preset.monoTextureHeight, preset.maxAntiAliasing, preset.enableBloomPrePassFog);
        }
#else
        private readonly SettingsManager _settings;
        private readonly SettingsApplicatorSO _applicator;

        public MirrorQualitySettings(SettingsManager settings, SettingsApplicatorSO applicator)
        {
            _settings = settings;
            _applicator = applicator;
        }

        internal MirrorQuality Current => _settings.settings.quality.mirror;

        internal void Apply(MirrorQuality quality)
        {
            var settings = _settings.settings;
            settings.quality.mirror = quality;
            _settings.settings = settings;
            _applicator.ApplyGraphicSettings(settings, SceneType.Menu);
            _applicator.ApplyGraphicSettings(settings, SceneType.Game);
        }
#endif
    }
}
