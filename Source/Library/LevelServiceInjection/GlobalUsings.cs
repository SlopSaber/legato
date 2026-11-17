// Legacy transitions receive loaded level data instead of these modern loader services.
#if BEAT_SABER_1_29_0
global using NativeLevelServiceInject = global::Zenject.InjectOptionalAttribute;
#else
global using NativeLevelServiceInject = global::Zenject.InjectAttribute;
#endif
