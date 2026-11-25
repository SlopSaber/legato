#if BEAT_SABER_1_29_0
global using LocalizationAsset = Polyglot.LocalizationAsset;
#else
global using LocalizationAsset = BGLib.Polyglot.LocalizationAsset;
#endif

#if LEGATO_PC_APP_INIT
global using BeatSaberInit = global::PCAppInit;
#endif
