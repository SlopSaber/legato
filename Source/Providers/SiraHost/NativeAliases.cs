#if BEAT_SABER_1_29_0
global using LocalizationAsset = Polyglot.LocalizationAsset;
#else
global using LocalizationAsset = BGLib.Polyglot.LocalizationAsset;
#endif

#if BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0
global using BeatSaberInit = global::PCAppInit;
#endif
