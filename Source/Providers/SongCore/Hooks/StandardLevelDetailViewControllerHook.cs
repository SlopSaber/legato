using System;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Zenject;

namespace SongCore.Hooks
{
    /// <summary>
    /// This displays an error message to the user in the <see cref="StandardLevelDetailView"/> when the game fails to load beatmap levels.
    /// </summary>
    internal class StandardLevelDetailViewControllerHook : IInitializable, IDisposable
    {
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;

        private ILHook _showLoadingAndDoSomethingHook = null!;

        private StandardLevelDetailViewControllerHook(StandardLevelDetailViewController standardLevelDetailViewController)
        {
            _standardLevelDetailViewController = standardLevelDetailViewController;
        }

        public void Initialize()
        {
            _showLoadingAndDoSomethingHook = new ILHook(typeof(StandardLevelDetailViewController).GetMethod(nameof(StandardLevelDetailViewController.ShowLoadingAndDoSomething), BindingFlags.Instance | BindingFlags.NonPublic)!.GetStateMachineTarget()!, ctx => {
            {
                var cursor = new ILCursor(ctx);
#if BEAT_SABER_1_29_0
                var handler = ctx.Body.ExceptionHandlers.Single(h => h.HandlerType == ExceptionHandlerType.Catch && h.CatchType?.FullName == typeof(Exception).FullName);
                cursor.Goto(handler.HandlerStart, MoveType.After);
#else
                cursor.GotoNext(MoveType.After, i => i.MatchCall(out var method) && method.Name == nameof(UnityEngine.Debug.LogException));
#endif
                cursor.EmitDelegate(ShowError);
            }}, true);
        }

        public void Dispose()
        {
            _showLoadingAndDoSomethingHook?.Dispose();
        }

        private void ShowError()
        {
            _standardLevelDetailViewController.ShowContent(StandardLevelDetailViewController.ContentType.Error, Localization.Get("ERROR_LOADING_DATA"));
        }
    }
}
