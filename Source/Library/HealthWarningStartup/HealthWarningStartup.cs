#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Legato.Gameplay
{
    internal static class HealthWarningStartup
    {
        private static Func<bool> _skipWarning = () => false;

        internal static void Install(Harmony harmony, Func<bool> skipWarning)
        {
            _skipWarning = skipWarning;

#if LEGATO_BEFORE_MULTIPLAYER
            Type? transitions = Type.GetType("DefaultScenesTransitionsFromInit, Main");
#else
            Type? resolver = Type.GetType("InitialDestinationResolver, GameInit");
            if (resolver != null)
            {
                MethodInfo? initialize = AccessTools.DeclaredMethod(resolver, "InitializeScenesAsync", Type.EmptyTypes);
                Type? stateMachine = initialize?.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
                MethodInfo? moveNext = stateMachine == null ? null : AccessTools.DeclaredMethod(stateMachine, nameof(IAsyncStateMachine.MoveNext));
                if (moveNext == null)
                    throw new MissingMethodException(resolver.FullName, "InitializeScenesAsync state machine");

                harmony.Patch(moveNext, transpiler: new HarmonyMethod(typeof(HealthWarningStartup), nameof(RewriteDestinationCheck)));
                return;
            }

            Type? transitions = Type.GetType("DefaultScenesTransitionsFromInit, GameInit")
                ?? Type.GetType("DefaultScenesTransitionsFromInit, Main");
#endif
            if (transitions == null)
                throw new TypeLoadException("Could not find the game startup transition.");

#if LEGATO_BEFORE_MULTIPLAYER
            MethodInfo? transition = AccessTools.DeclaredMethod(transitions, "TransitionToNextScene", new[] { typeof(bool) });
            string prefix = nameof(SelectNativeMenuPath);
#else
            MethodInfo? transition = AccessTools.DeclaredMethod(transitions, "TransitionToNextScene",
                new[] { typeof(bool), typeof(bool), typeof(bool), typeof(Action) })
                ?? AccessTools.DeclaredMethod(transitions, "TransitionToNextScene",
                    new[] { typeof(bool), typeof(bool), typeof(bool) });
            string prefix = nameof(SelectLegacyMenuPath);
#endif
            if (transition == null)
                throw new MissingMethodException(transitions.FullName, "TransitionToNextScene");

            harmony.Patch(transition, prefix: new HarmonyMethod(typeof(HealthWarningStartup), prefix));
        }

        private static IEnumerable<CodeInstruction> RewriteDestinationCheck(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int gate = codes.FindIndex(instruction => instruction.operand is MethodInfo method
                && method.DeclaringType?.FullName == "BeatSaber.Destinations.Destination"
                && method.Name == "get_shouldLoadHealthWarning" && method.ReturnType == typeof(bool));
            if (gate < 0)
                throw new InvalidOperationException("Could not find the health warning scene decision.");

            // Intercept the result in the caller so the native getter can be inlined safely.
            codes.Insert(gate + 1, new CodeInstruction(OpCodes.Call,
                AccessTools.DeclaredMethod(typeof(HealthWarningStartup), nameof(ShouldLoadWarning))));
            return codes;
        }

        private static bool ShouldLoadWarning(bool requested) => requested && !_skipWarning();

#if LEGATO_BEFORE_MULTIPLAYER
        private static void SelectNativeMenuPath(ref bool __0)
        {
            if (!__0 && _skipWarning())
                __0 = true;
        }
#else
        private static void SelectLegacyMenuPath(ref bool __0, bool __1, bool __2)
        {
            if (!__0 && !__1 && !__2 && _skipWarning())
                __0 = true;
        }
#endif
    }
}
