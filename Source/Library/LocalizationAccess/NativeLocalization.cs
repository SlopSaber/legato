using System;
using System.Collections.Generic;
using System.Reflection;
using IPA.Utilities;
using UnityEngine;

namespace Legato.LocalizationSupport {
    internal static class NativeLocalization {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        internal static bool HasInstance =>
#if BEAT_SABER_1_29_0
            Localization.HasInstance;
#else
            typeof(Localization).GetField("_instance", Fields).GetValue(null) != null;
#endif
        internal static List<LocalizationAsset> InputFiles =>
#if BEAT_SABER_1_29_0
            Localization.Instance.InputFiles;
#else
            Localization.Instance.inputFiles;
#endif
        internal static Dictionary<string, List<string>> Strings =>
#if BEAT_SABER_1_29_0
            (Dictionary<string, List<string>>)typeof(LocalizationImporter).GetField("languageStrings", Fields).GetValue(null);
#else
            FieldAccessor<LocalizationModel, Dictionary<string, List<string>>>.Get(Localization.Instance, "_languageStrings");
#endif
        internal static List<LocalizationLanguage> SupportedLanguages {
            get {
#if BEAT_SABER_1_29_0
                return Localization.Instance.SupportedLanguages;
#else
                var data = typeof(LocalizationModel).GetField("_localization", Fields).GetValue(Localization.Instance);
                return (List<LocalizationLanguage>)data.GetType().GetField("supportedLanguages", Fields).GetValue(data);
#endif
            }
        }
        internal static void ImportFiles() {
#if BEAT_SABER_1_29_0
            LocalizationImporter.ImportFromFiles(Localization.Instance);
#else
            LocalizationImporter.ImportFromFiles(InputFiles);
#endif
        }
        internal static void RaiseChanged() {
#if BEAT_SABER_1_29_0
            Localization.Instance.InvokeOnLocalize();
#else
            FieldAccessor<LocalizationModel, Action<LocalizationModel>>.Get(Localization.Instance, "_onChangeLanguage")?.Invoke(Localization.Instance);
#endif
        }
        internal static string Get(LocalizationModel model, string key, LocalizationLanguage language) {
#if BEAT_SABER_1_29_0
            return Localization.Get(key, language);
#else
            model.TryGet(key, language, out string value);
            return value;
#endif
        }
        internal static List<string> GetLanguages(string key) =>
#if BEAT_SABER_1_29_0
            LocalizationImporter.GetLanguages(key, null);
#else
            Localization.Instance.GetLanguages(key);
#endif
        internal static void Remove(LocalizationModel model, ILocalize listener) {
#if BEAT_SABER_1_29_0
            model.RemoveOnLocalizeEvent(listener);
#else
            var handler = FieldAccessor<LocalizationModel, Action<LocalizationModel>>.Get(model, "_onChangeLanguage");
            handler -= listener.OnLocalize;
            model.SetField("_onChangeLanguage", handler);
#endif
        }
        internal static MethodInfo ParseLanguageMethod => typeof(LocalizationLanguageExtensions).GetMethod(
#if BEAT_SABER_1_29_0
            "ToLanguage",
#else
            "ToLocalizationLanguage",
#endif
            new[] { typeof(string) });
        internal static MethodInfo SetSingletonMethod =>
#if BEAT_SABER_1_29_0
            typeof(Localization).GetProperty("Instance").GetSetMethod();
#else
            typeof(Localization).GetMethod("SetSingletonInstance", Fields);
#endif
    }
    internal abstract class LocalizationListener : MonoBehaviour, ILocalize {
#if BEAT_SABER_1_29_0
        public void OnLocalize() => LocaleChanged();
#else
        public void OnLocalize(LocalizationModel model) => LocaleChanged();
#endif
        protected abstract void LocaleChanged();
    }
    internal sealed class LanguagePreferences {
#if BEAT_SABER_1_29_0
        private readonly MainSettingsModelSO _settings;
        public LanguagePreferences(MainSettingsModelSO settings) { _settings = settings; }
        internal LocalizationLanguage Selected => _settings.language.value;
#else
        private readonly SettingsManager _settings;
        public LanguagePreferences(SettingsManager settings) { _settings = settings; }
        internal LocalizationLanguage Selected => LocalizationLanguageExtensions.ToLocalizationLanguage(_settings.settings.misc.language);
#endif
    }
}
