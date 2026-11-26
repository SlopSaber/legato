#if BEAT_SABER_1_29_0 || LEGATO_HMUI_LEGACY_GRAPHICS
global using BsmlListSettingTemplate = global::FormattedFloatListSettingsValueController;
#else
global using BsmlListSettingTemplate = global::FormattedFloatListSettingsController;
#endif
