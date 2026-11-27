using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Legato.LocalizationSupport {
    internal static class LocalizationResource {
        private static TextAsset _asset = null!;
        private static Harmony _harmony = null!;

        internal static void Patch(string resource, string owner) {
            if (_harmony != null) return;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)) {
                if (stream == null) throw new FileNotFoundException("Missing embedded localization resource: " + resource);
                using (var reader = new StreamReader(stream)) _asset = new TextAsset(reader.ReadToEnd());
            }
            _harmony = new Harmony(owner);
#if LEGATO_EARLY_VR_UI
            Legato.LocalizationSupport.EarlyLocalization.AddCsv(_asset.text);
#elif BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(LocalizationImporter), "ImportInputFiles"), postfix: new HarmonyMethod(typeof(LocalizationResource), nameof(ImportLegacy)));
#else
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(LocalizationAsyncInstaller), "LoadResourcesBeforeInstall"), prefix: new HarmonyMethod(typeof(LocalizationResource), nameof(AddAsset)));
#endif
        }

        internal static string Format(string key, params object[] values) =>
#if LEGATO_EARLY_VR_UI
            string.Format(Legato.LocalizationSupport.EarlyLocalization.Get(key), values);
#elif BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
            Localization.GetFormat(key, values);
#else
            Localization.Instance.GetFormatOrKey(key, values);
#endif

#if LEGATO_EARLY_VR_UI
#elif BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
        private static void ImportLegacy() => Polyglot.LocalizationImporter.ImportTextFile(_asset.text, Polyglot.GoogleDriveDownloadFormat.CSV);
#else
        private static void AddAsset(IList<TextAsset> assets) => assets.Add(_asset);
#endif
    }
}
