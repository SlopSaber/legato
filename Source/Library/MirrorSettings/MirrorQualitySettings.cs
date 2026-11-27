#if LEGATO_EARLY_SCENE_SETUP || LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0 || LEGATO_APPLY_PERFORMANCE_PRESET
global using MirrorQuality = Legato.Rendering.LegacyMirrorQuality;
#else
global using MirrorQuality = BeatSaber.Settings.QualitySettings.MirrorQuality;
#endif

using System;
#if LEGATO_EARLY_SCENE_SETUP || LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
using System.Linq;
using UnityEngine;
#if LEGATO_EARLY_SCENE_SETUP
using IPA.Utilities;
#endif
#elif LEGATO_APPLY_PERFORMANCE_PRESET
using BeatSaber.GameSettings;
using BeatSaber.PerformancePresets;
using IPA.Utilities;
#endif

namespace Legato.Rendering
{
#if LEGATO_EARLY_SCENE_SETUP || LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0 || LEGATO_APPLY_PERFORMANCE_PRESET
    public enum LegacyMirrorQuality { Off = 0, Low = 1, Medium = 2, High = 3 }
#endif

    internal sealed class MirrorQualitySettings
    {
#if LEGATO_EARLY_SCENE_SETUP
        private readonly MainSettingsModel _settings = FindResource<MainSettingsModel>();
        private readonly MirrorRenderer _renderer = FindResource<MirrorRenderer>();
        private readonly MirrorRendererGraphicsSettingsPresets _presets = FindResource<MirrorRendererGraphicsSettingsPresets>();

        internal MirrorQuality Current => _settings.mirrorGraphicsSettings == 2
            ? MirrorQuality.High : (MirrorQuality)_settings.mirrorGraphicsSettings;

        internal void Apply(MirrorQuality quality)
        {
            int index = quality == MirrorQuality.High ? 2 : (int)quality;
            if ((uint)index >= (uint)_presets.presets.Length) throw new ArgumentOutOfRangeException(nameof(quality));
            _renderer.InitFromPreset(_presets.presets[index]);
            _renderer.InvokeMethod<object, MirrorRenderer>("ValidateParams");
        }
#elif LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
        private readonly MainSettingsModelSO _settings = FindResource<MainSettingsModelSO>();
        private readonly MirrorRendererSO _renderer = FindResource<MirrorRendererSO>();
        private readonly MirrorRendererGraphicsSettingsPresets _presets = FindResource<MirrorRendererGraphicsSettingsPresets>();

        internal MirrorQuality Current
        {
            get
            {
                int index = _settings.mirrorGraphicsSettings.value;
#if LEGATO_BEFORE_MULTIPLAYER
                if (index == 2) return MirrorQuality.High;
#endif
                return (MirrorQuality)index;
            }
        }

        internal void Apply(MirrorQuality quality)
        {
            int index = (int)quality;
#if LEGATO_BEFORE_MULTIPLAYER
            if (quality == MirrorQuality.High) index = 2;
#endif
            if ((uint)index >= (uint)_presets.presets.Length) throw new ArgumentOutOfRangeException(nameof(quality));
            var preset = _presets.presets[index];
            _settings.mirrorGraphicsSettings.value = index;
            _renderer.Init(preset.reflectLayers, preset.stereoTextureWidth, preset.stereoTextureHeight,
                preset.monoTextureWidth, preset.monoTextureHeight, preset.maxAntiAliasing, preset.enableBloomPrePassFog);
        }
#elif LEGATO_APPLY_PERFORMANCE_PRESET
        private static readonly FieldAccessor<PerformancePreset, MirrorQualityPreset>.Accessor MirrorPreset =
            FieldAccessor<PerformancePreset, MirrorQualityPreset>.GetAccessor("_mirrorGraphics");

        private readonly GraphicSettingsHandler _settings;
        private readonly MirrorRendererSO _renderer;
        private readonly MirrorRendererGraphicsSettingsPresets _presets;

        public MirrorQualitySettings(GraphicSettingsHandler settings, SettingsApplicatorSO applicator)
        {
            _settings = settings;
            _renderer = FieldAccessor<SettingsApplicatorSO, MirrorRendererSO>.Get(applicator, "_mirrorRenderer");
            _presets = FieldAccessor<SettingsApplicatorSO, MirrorRendererGraphicsSettingsPresets>.Get(applicator, "_mirrorRendererGraphicsSettingsPresets");
        }

        private PerformancePreset CurrentPreset => _settings.TryGetCurrentPerformancePreset(out var preset)
            ? preset
            : throw new InvalidOperationException("The native performance preset has not loaded.");

        internal MirrorQuality Current => (MirrorQuality)CurrentPreset.mirrorGraphics;

        internal void Apply(MirrorQuality quality)
        {
            int index = (int)quality;
            if ((uint)index >= (uint)_presets.presets.Length) throw new ArgumentOutOfRangeException(nameof(quality));
            var mirror = _presets.presets[index];
            var preset = CurrentPreset;
            if (preset is CustomPerformancePreset custom)
                custom.mirrorGraphics = (MirrorQualityPreset)quality;
            else
                MirrorPreset(ref preset) = (MirrorQualityPreset)quality;
            _renderer.Init(mirror.reflectLayers, mirror.stereoTextureWidth, mirror.stereoTextureHeight,
                mirror.monoTextureWidth, mirror.monoTextureHeight, mirror.maxAntiAliasing, mirror.enableBloomPrePassFog);
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
#if LEGATO_EARLY_SCENE_SETUP || LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0
        private static T FindResource<T>() where T : UnityEngine.Object => UnityEngine.Resources.FindObjectsOfTypeAll<T>().FirstOrDefault()
            ?? throw new InvalidOperationException("Native mirror resource is unavailable: " + typeof(T).FullName);
#endif
    }
}
