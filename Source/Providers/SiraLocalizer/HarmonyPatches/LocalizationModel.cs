using System.Globalization;

using HarmonyLib;

namespace SiraLocalizer.HarmonyPatches
{
#if BEAT_SABER_1_29_0
    [HarmonyPatch(typeof(LocalizationModel), nameof(LocalizationModel.SelectedCultureInfo), MethodType.Getter)]
    internal class LocalizationModel_SelectedCultureInfo_Setter {
        public static void Postfix(ref CultureInfo __result) {
            if (__result != null && __result.IetfLanguageTag.StartsWith("fr")) {
                __result = (CultureInfo)__result.Clone();
                __result.NumberFormat.PercentSymbol = "%";
            }
        }
    }
#else
    [HarmonyPatch(typeof(LocalizationModel), nameof(LocalizationModel.SelectedCultureInfo), MethodType.Setter)]
    internal class LocalizationModel_SelectedCultureInfo_Setter
    {
        public static void Prefix(CultureInfo value)
        {
            // French uses ٪ rather than % for some reason
            if (value.IetfLanguageTag.StartsWith("fr"))
            {
                value.NumberFormat.PercentSymbol = "%";
            }
        }
    }
#endif
}
