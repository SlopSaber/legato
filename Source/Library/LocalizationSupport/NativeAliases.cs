#if BEAT_SABER_1_29_0
global using CsvReader = Polyglot.CsvReader;
global using ILocalize = Polyglot.ILocalize;
global using Localization = Polyglot.Localization;
global using LocalizationAsset = Polyglot.LocalizationAsset;
global using LocalizationImporter = Polyglot.LocalizationImporter;
global using LocalizationLanguage = Polyglot.Language;
global using LocalizationLanguageExtensions = Polyglot.LanguageExtensions;
global using LocalizationModel = Polyglot.Localization;
global using LocalizedTextMeshProUGUI = Polyglot.LocalizedTextMeshProUGUI;
#else
global using CsvReader = BGLib.Polyglot.CsvReader;
global using ILocalize = BGLib.Polyglot.ILocalize;
global using Localization = BGLib.Polyglot.Localization;
global using LocalizationAsset = BGLib.Polyglot.LocalizationAsset;
global using LocalizationAsyncInstaller = BGLib.Polyglot.LocalizationAsyncInstaller;
global using LocalizationImporter = BGLib.Polyglot.LocalizationImporter;
global using LocalizationLanguage = BGLib.Polyglot.LocalizationLanguage;
global using LocalizationLanguageExtensions = BGLib.Polyglot.LocalizationLanguageExtensions;
global using LocalizationModel = BGLib.Polyglot.LocalizationModel;
global using LocalizedTextMeshProUGUI = BGLib.Polyglot.LocalizedTextMeshProUGUI;
#endif
