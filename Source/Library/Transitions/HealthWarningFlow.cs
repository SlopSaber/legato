#nullable enable

using System;
using HarmonyLib;
#if BEAT_SABER_1_29_0
using IPA.Utilities;
#else
using System.Reflection;
#endif

namespace Legato.Gameplay
{
    internal static class HealthWarningFlow
    {
#if BEAT_SABER_1_29_0
        private static readonly FieldAccessor<HealthWarningViewController, Action>.Accessor Finished =
            FieldAccessor<HealthWarningViewController, Action>.GetAccessor("didFinishEvent");
#else
        private static readonly FieldInfo CompletionSource = AccessTools.Field(typeof(HealthWarningViewController), "_taskCompletionSource");
        private static readonly Action<HealthWarningViewController> CompleteNative =
            AccessTools.MethodDelegate<Action<HealthWarningViewController>>(AccessTools.Method(typeof(HealthWarningViewController), "Complete"));
#endif

        internal static void Complete(HealthWarningViewController controller)
        {
#if BEAT_SABER_1_29_0
            Finished(ref controller)?.Invoke();
#else
            if (CompletionSource.GetValue(controller) != null)
            {
                CompleteNative(controller);
            }
#endif
        }
    }
}
