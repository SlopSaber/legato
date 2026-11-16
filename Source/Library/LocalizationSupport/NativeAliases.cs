#if BEAT_SABER_1_29_0
global using Localization = Polyglot.Localization;
global using LocalizationModel = Polyglot.Localization;
global using LocalizedTextMeshProUGUI = Polyglot.LocalizedTextMeshProUGUI;
global using ILocalize = Polyglot.ILocalize;
#else
global using Localization = BGLib.Polyglot.Localization;
global using LocalizationModel = BGLib.Polyglot.LocalizationModel;
global using LocalizedTextMeshProUGUI = BGLib.Polyglot.LocalizedTextMeshProUGUI;
global using ILocalize = BGLib.Polyglot.ILocalize;
#endif
