#if !BEAT_SABER_1_29_0 && !LEGATO_NATIVE_POLYGLOT
using System;
using IPA.Utilities;
#endif

namespace Legato.LocalizationSupport
{
    /// <summary>
    /// Adapts language change subscriptions across localization models.
    /// </summary>
    internal static class LocalizationEvents
    {
#if !BEAT_SABER_1_29_0 && !LEGATO_NATIVE_POLYGLOT
        private static readonly FieldAccessor<LocalizationModel, Action<LocalizationModel>>.Accessor OnChangeLanguageAccessor = FieldAccessor<LocalizationModel, Action<LocalizationModel>>.GetAccessor("_onChangeLanguage");
#endif

        /// <summary>
        /// Unsubscribes a component from native language changes.
        /// </summary>
        /// <param name="model">The active localization model.</param>
        /// <param name="localize">The component to unsubscribe.</param>
        internal static void RemoveOnLocalizeEvent(this LocalizationModel model, ILocalize localize)
        {
#if BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
            model.RemoveOnLocalizeEvent(localize);
#else
            Action<LocalizationModel> action = OnChangeLanguageAccessor(ref model);
            action -= localize.OnLocalize;
            OnChangeLanguageAccessor(ref model) = action;
#endif
        }
    }
}
