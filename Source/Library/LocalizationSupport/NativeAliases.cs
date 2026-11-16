#if BEAT_SABER_1_29_0
global using Localization = Polyglot.Localization;
global using LocalizationModel = Polyglot.Localization;
global using LocalizedTextMeshProUGUI = Polyglot.LocalizedTextMeshProUGUI;
global using ILocalize = Polyglot.ILocalize;
global using LocalizationLanguage = Polyglot.Language;
global using LocalizationLanguageExtensions = Polyglot.LanguageExtensions;
global using LocalizationImporter = Polyglot.LocalizationImporter;
global using LocalizationAsset = Polyglot.LocalizationAsset;
global using CsvReader = Polyglot.CsvReader;
#else
global using Localization = BGLib.Polyglot.Localization;
global using LocalizationAsyncInstaller = BGLib.Polyglot.LocalizationAsyncInstaller;
global using LocalizationModel = BGLib.Polyglot.LocalizationModel;
global using LocalizedTextMeshProUGUI = BGLib.Polyglot.LocalizedTextMeshProUGUI;
global using ILocalize = BGLib.Polyglot.ILocalize;
global using LocalizationLanguage = BGLib.Polyglot.LocalizationLanguage;
global using LocalizationLanguageExtensions = BGLib.Polyglot.LocalizationLanguageExtensions;
global using LocalizationImporter = BGLib.Polyglot.LocalizationImporter;
global using LocalizationAsset = BGLib.Polyglot.LocalizationAsset;
global using CsvReader = BGLib.Polyglot.CsvReader;
#endif
