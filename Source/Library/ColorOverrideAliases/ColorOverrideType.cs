#if LEGATO_SINGLE_COLOR_OVERRIDE
global using LegatoColorOverrideType = Legato.Gameplay.LegacyColorOverride;
#elif BEAT_SABER_1_45_3
global using LegatoColorOverrideType = global::ColorOverrideType;
#else
global using LegatoColorOverrideType = global::ColorSchemesSettings.ColorOverrideType;
#endif

#if LEGATO_SINGLE_COLOR_OVERRIDE
namespace Legato.Gameplay {
    /// <summary>Color override choices for the legacy transition adapter.</summary>
    public enum LegacyColorOverride {
        /// <summary>Use the map colors.</summary>
        None,
        /// <summary>Override note colors.</summary>
        Notes,
        /// <summary>Override note and environment colors.</summary>
        All
    }
}
#endif
