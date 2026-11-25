#if LEGATO_NESTED_MENU_LIGHT_PAIR
global using NativeMenuLightPair = global::MenuLightsPresetSO.LightIdColorPair;
#else
global using NativeMenuLightPair = global::LightIdColorPair;
#endif
