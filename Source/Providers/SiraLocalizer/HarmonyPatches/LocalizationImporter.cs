using System.Reflection;
using HarmonyLib;
using Legato.LocalizationSupport;

namespace SiraLocalizer.HarmonyPatches {
    [HarmonyPatch(typeof(LocalizationImporter), nameof(LocalizationImporter.ImportFromFiles))]
    internal static class LocalizationImporter_Initialize {
        public static void Postfix() {
            if (NativeLocalization.HasInstance) NativeLocalization.RaiseChanged();
        }
    }
    [HarmonyPatch]
    internal static class Localization_SetSingletonInstance {
        private static MethodBase TargetMethod() => NativeLocalization.SetSingletonMethod;
        public static void Postfix() => NativeLocalization.RaiseChanged();
    }
}
