#if LEGATO_EARLY_STANDARD_MAPS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using Heck.PlayView;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace Heck.SettingsSetter;

[PlayViewControllerSettings(0, "settings")]
internal sealed class SettingsSetterViewController : BSMLResourceViewController, IPlayViewController, IParameterModifier
{
    [UIValue("contents")]
    private readonly List<object> _contents = [];
    private readonly List<Action> _apply = [];
    private readonly Stack<Action> _restore = new();

    [UIObject("contentObject")]
    private GameObject? _contentObject;
    private MainSettingsModel _settings = null!;
    private SiraLog _log = null!;
    private StartStandardLevelParameters? _modified;
    private bool _accepted;

    public event Action? Finished;

    public event Action<StartStandardLevelParameters>? ParametersModified;

    public override string ResourceName => "Heck.SettingsSetter.SettingsSetter.bsml";

    public bool Init(StartStandardLevelParameters original)
    {
        _contents.Clear();
        _apply.Clear();
        _accepted = false;
        StartStandardLevelParameters modified = original.Copy();
        _modified = modified;
        CustomData? settings = original.DifficultyBeatmap.GetBeatmapCustomData().Get<CustomData>("_settings");
        if (settings == null)
        {
            return false;
        }

        try
        {
            GameplayOptions options = modified.GameplayOptions;
            CustomData? player = settings.Get<CustomData>("_playerOptions");
            if (player != null)
            {
                options.mirror = Read(player, "_leftHanded", "Left-handed mode", options.mirror);
                Change(player, "_playerHeight", "Player height", () => _settings.playerHeight, value => _settings.playerHeight = value);
            }

            CustomData? modifiers = settings.Get<CustomData>("_modifiers");
            if (modifiers != null)
            {
                options.noEnergy = Read(modifiers, "_noFailOn0Energy", "No fail", options.noEnergy);
                options.obstaclesOption = Read(modifiers, "_enabledObstacleType", "Obstacles", options.obstaclesOption);
            }

            CustomData? graphics = settings.Get<CustomData>("_graphics");
            if (graphics != null)
            {
                Change(graphics, "_mirrorGraphicsSettings", "Mirror detail", () => _settings.mirrorGraphicsSettings, value => _settings.mirrorGraphicsSettings = value);
                Change(graphics, "_mainEffectGraphicsSettings", "Main effect", () => _settings.mainEffectGraphicsSettings, value => _settings.mainEffectGraphicsSettings = value);
                Change(graphics, "_smokeGraphicsSettings", "Smoke", () => _settings.smokeGraphicsSettings, value => _settings.smokeGraphicsSettings = value);
            }

            foreach (KeyValuePair<string, Dictionary<string, ISettableSetting>> group in SettingSetterSettableSettingsManager.SettableSettings)
            {
                CustomData? recommendations = settings.Get<CustomData>(group.Key);
                if (recommendations == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, ISettableSetting> entry in group.Value)
                {
                    ISettableSetting setting = entry.Value;
                    object? recommendation = recommendations.Get<object>(entry.Key);
                    if (recommendation == null)
                    {
                        continue;
                    }

                    object originalValue = setting.TrueValue;
                    object converted = ConvertValue(recommendation, originalValue.GetType());
                    if (Equals(converted, originalValue))
                    {
                        continue;
                    }

                    Add(setting.FieldName, originalValue, converted);
                    _apply.Add(() =>
                    {
                        _restore.Push(() => setting.SetTemporary(null));
                        setting.SetTemporary(converted);
                    });
                }
            }

            return _contents.Count != 0;
        }
        catch (Exception exception)
        {
            _log.Error(exception);
            _contents.Clear();
            _apply.Clear();
            return false;
        }
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
        if (!addedToHierarchy)
        {
            return;
        }

        if (_contentObject != null)
        {
            _contentObject.SetActive(false);
            Destroy(_contentObject);
        }

        using StreamReader reader = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("Heck.SettingsSetter.SettableSettingsContent.bsml")
            ?? throw new InvalidOperationException("Missing settings recommendations layout."));
        BSMLParser.Instance.Parse(reader.ReadToEnd(), gameObject, this);
    }

    private static object ConvertValue(object value, Type type) => type.IsEnum
        ? value is string text ? Enum.Parse(type, text, true) : Enum.ToObject(type, Convert.ToInt32(value))
        : Convert.ChangeType(value, type);

    [Inject]
    private void Construct(MainSettingsModel settings, SiraLog log)
    {
        _settings = settings;
        _log = log;
    }

    [UIAction("accept-click")]
    private void Accept()
    {
        if (_modified != null)
        {
            _accepted = true;
            ParametersModified?.Invoke(_modified);
        }

        Finished?.Invoke();
    }

    [UIAction("decline-click")]
    private void Decline() => Finished?.Invoke();

    private void OnPlay()
    {
        if (!_accepted)
        {
            return;
        }

        foreach (Action apply in _apply)
        {
            apply();
        }
    }

    private void OnFinish()
    {
        while (_restore.Count != 0)
        {
            _restore.Pop().Invoke();
        }
    }

    private void Change<T>(CustomData data, string key, string label, Func<T> get, Action<T> set)
        where T : struct
    {
        T original = get();
        T recommended = Read(data, key, label, original);
        if (!Equals(original, recommended))
        {
            _apply.Add(() =>
            {
                T current = get();
                _restore.Push(() => set(current));
                set(recommended);
            });
        }
    }

    private T Read<T>(CustomData data, string key, string label, T original)
        where T : struct
    {
        object? input = data.Get<object>(key);
        if (input == null)
        {
            return original;
        }

        T recommended = (T)ConvertValue(input, typeof(T));
        if (!Equals(recommended, original))
        {
            Add(label, original, recommended);
        }

        return recommended;
    }

    private void Add(string label, object before, object after) =>
        _contents.Add(new { lefttext = label, righttext = $"{before} → {after}" });
}
#endif
