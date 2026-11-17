#ifndef LEGATO_UNITY_UI_COMPATIBILITY_INCLUDED
#define LEGATO_UNITY_UI_COMPATIBILITY_INCLUDED
#include "UnityUI.cginc"

#if UNITY_VERSION < 202000
// Preserve the UI vertex-color conversion used by newer text shaders.
half3 UIGammaToLinear(half3 value)
{
    half3 low = 0.0849710 * value - 0.000163029;
    half3 high = value * (value * (value * 0.265885 + 0.736584) - 0.00980184) + 0.00319697;
    return value < (half3)0.0725490 ? low : high;
}
#endif
#endif
