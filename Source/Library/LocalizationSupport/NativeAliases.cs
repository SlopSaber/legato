#if BEAT_SABER_1_29_0 || LEGATO_NATIVE_POLYGLOT
global using CsvReader = Polyglot.CsvReader;
global using ILocalize = Polyglot.ILocalize;
global using Localization = Polyglot.Localization;
global using LocalizationAsset = Polyglot.LocalizationAsset;
global using LocalizationImporter = Polyglot.LocalizationImporter;
global using LocalizationLanguage = Polyglot.Language;
#if !LEGATO_NATIVE_POLYGLOT
global using LocalizationLanguageExtensions = Polyglot.LanguageExtensions;
#endif
global using LocalizationModel = Polyglot.Localization;
global using LocalizedTextMeshProUGUI = Polyglot.LocalizedTextMeshProUGUI;
#else
global using CsvReader = BGLib.Polyglot.CsvReader;
global using ILocalize = BGLib.Polyglot.ILocalize;
global using Localization = BGLib.Polyglot.Localization;
global using LocalizationAsset = BGLib.Polyglot.LocalizationAsset;
global using LocalizationAsyncInstaller = BGLib.Polyglot.LocalizationAsyncInstaller;
global using LocalizationImporter = BGLib.Polyglot.LocalizationImporter;
#if LEGATO_LEGACY_TEXTMESH_PRO
global using LocalizationLanguage = BGLib.Polyglot.Language;
global using LocalizationLanguageExtensions = BGLib.Polyglot.LanguageExtensions;
#else
global using LocalizationLanguage = BGLib.Polyglot.LocalizationLanguage;
global using LocalizationLanguageExtensions = BGLib.Polyglot.LocalizationLanguageExtensions;
#endif
global using LocalizationModel = BGLib.Polyglot.LocalizationModel;
global using LocalizedTextMeshProUGUI = BGLib.Polyglot.LocalizedTextMeshProUGUI;
#endif
