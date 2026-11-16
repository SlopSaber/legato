#if BEAT_SABER_1_29_0
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
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
using Legato.LocalizationSupport;
using HarmonyLib;
using UnityEngine;

namespace BeatSaberMarkupLanguage.Harmony_Patches
{
    [HarmonyPatch(typeof(LocalizationAsyncInstaller), "LoadResourcesBeforeInstall")]
    internal class LocalizationLoader
    {
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
