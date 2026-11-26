using System.Collections.Generic;

using HarmonyLib;
using SiraLocalizer.Records;
using UnityEngine;

namespace SiraLocalizer.HarmonyPatches
{
#if BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
    [HarmonyPatch(typeof(LocalizationImporter), nameof(LocalizationImporter.ImportFromFiles))]
    internal class LocalizationLoader {
        [HarmonyPriority(Priority.First)]
        public static void Prefix(Localization __0) {
            LocalizationDefinition.Remove("beat-saber");
            LocalizationDefinition.Add("beat-saber", "Beat Saber", __0.InputFiles.ConvertAll(asset => asset.TextAsset));
        }
    }
#else
    [HarmonyPatch(typeof(LocalizationAsyncInstaller), "LoadResourcesBeforeInstall")]
    internal class LocalizationLoader
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix(IList<TextAsset> assets)
        {
            LocalizationDefinition.Remove("beat-saber");
            LocalizationDefinition.Add("beat-saber", "Beat Saber", assets);
        }
    }
#endif
}
