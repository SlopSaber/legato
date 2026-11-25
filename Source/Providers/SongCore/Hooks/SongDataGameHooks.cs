using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using SongCore.Utilities;
using Zenject;

namespace SongCore.Hooks
{
    internal class SongDataGameHooks : IInitializable, IDisposable
    {
        private readonly BeatmapKey _beatmapKey;

        private Hook? _rotationNoteSpawnLinesHook;
        private Hook? _oneSaberHook;
        private bool _showRotationNoteSpawnLines;
        private bool _oneSaber;
#if LEGATO_BEFORE_MULTIPLAYER
        private readonly PluginConfig _config;

        private SongDataGameHooks(BeatmapKey beatmapKey, PluginConfig config)
        {
            _beatmapKey = beatmapKey;
            _config = config;
        }
#else
        private SongDataGameHooks(BeatmapKey beatmapKey)
        {
            _beatmapKey = beatmapKey;
        }
#endif

        public void Initialize()
        {
            var difficultyData = Collections.GetCustomLevelSongDifficultyData(_beatmapKey);

            if (difficultyData == null)
            {
                return;
            }

            if (difficultyData._showRotationNoteSpawnLines != null
#if LEGATO_BEFORE_MULTIPLAYER
                && !_config.DisableRotationSpawnLinesOverride
#endif
                )
            {
                _showRotationNoteSpawnLines = (bool)difficultyData._showRotationNoteSpawnLines;
                _rotationNoteSpawnLinesHook = new Hook(typeof(BeatLineManager).GetMethod(nameof(BeatLineManager.HandleNoteWasSpawned), BindingFlags.Instance | BindingFlags.NonPublic)!, ShowOrHideRotationNoteSpawnLines, true);
            }

            if (difficultyData._oneSaber != null
#if LEGATO_BEFORE_MULTIPLAYER
                && !_config.DisableOneSaberOverride
#endif
                )
            {
                _oneSaber = difficultyData._oneSaber.Value;
#if LEGATO_BEFORE_MULTIPLAYER
                _oneSaberHook = new Hook(typeof(PlayerController).GetMethod(nameof(PlayerController.Start), BindingFlags.Instance | BindingFlags.NonPublic)!, ForceOneSaber, true);
#else
                _oneSaberHook = new Hook(typeof(SaberManager).GetMethod(nameof(SaberManager.Start), BindingFlags.Instance | BindingFlags.NonPublic)!, ForceOneSaber, true);
#endif
            }
        }

        public void Dispose()
        {
            _rotationNoteSpawnLinesHook?.Dispose();
            _oneSaberHook?.Dispose();
        }

        private void ShowOrHideRotationNoteSpawnLines(Action<BeatLineManager, NoteController> original, BeatLineManager instance, NoteController noteController)
        {
            if (_showRotationNoteSpawnLines)
            {
                original(instance, noteController);
            }
        }

#if LEGATO_BEFORE_MULTIPLAYER
        private void ForceOneSaber(Action<PlayerController> original, PlayerController instance)
        {
            instance._initData = new PlayerController.InitData(_oneSaber, instance._initData.oneSaberType);
            original(instance);
        }
#else
        private void ForceOneSaber(Action<SaberManager> original, SaberManager instance)
        {
            Accessors.SaberManagerInitDataAccessor(ref instance) = new SaberManager.InitData(_oneSaber, instance._initData.oneSaberType);
            original(instance);
        }
#endif
    }
}
