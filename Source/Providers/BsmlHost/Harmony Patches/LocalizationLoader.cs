#if !LEGATO_EARLY_VR_UI
#if BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    /// <summary>
    /// Imports BSML's strings after the legacy localization files.
    /// </summary>
    [HarmonyPatch(typeof(Polyglot.LocalizationImporter), nameof(Polyglot.LocalizationImporter.ImportInputFiles))]
    internal class LocalizationLoader
    {
        private static void Postfix()
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BeatSaberMarkupLanguage.Resources.beat-saber-markup-language.csv");
            using StreamReader reader = new(stream);
            Polyglot.LocalizationImporter.ImportTextFile(reader.ReadToEnd(), Polyglot.GoogleDriveDownloadFormat.CSV);
        }
    }
}
#else
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    /// <summary>
    /// Adds BSML's strings to the localization installer's resources.
    /// </summary>
    [HarmonyPatch(typeof(LocalizationAsyncInstaller), "LoadResourcesBeforeInstall")]
    internal class LocalizationLoader
    {
        /// <summary>
        /// Adds the embedded localization CSV to the resource list.
        /// </summary>
        /// <param name="assets">The native localization resources.</param>
        public static void Prefix(IList<TextAsset> assets)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BeatSaberMarkupLanguage.Resources.beat-saber-markup-language.csv"))
            using (StreamReader reader = new(stream))
            {
                string content = reader.ReadToEnd();
                assets.Add(new TextAsset(content));
            }
        }
    }
}

#endif
#endif
