using System;
using IPA.Utilities;

namespace Legato.LocalizationSupport
{
    internal static class LocalizationEvents
    {
#if !BEAT_SABER_1_29_0
        private static readonly FieldAccessor<LocalizationModel, Action<LocalizationModel>>.Accessor OnChangeLanguageAccessor = FieldAccessor<LocalizationModel, Action<LocalizationModel>>.GetAccessor("_onChangeLanguage");
#endif

        internal static void RemoveOnLocalizeEvent(this LocalizationModel model, ILocalize localize)
        {
#if BEAT_SABER_1_29_0
            model.RemoveOnLocalizeEvent(localize);
#else
            Action<LocalizationModel> action = OnChangeLanguageAccessor(ref model);
            action -= localize.OnLocalize;
            OnChangeLanguageAccessor(ref model) = action;
#endif
        }
    }
}
