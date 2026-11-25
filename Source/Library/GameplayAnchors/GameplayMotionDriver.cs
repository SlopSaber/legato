#nullable enable
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Zenject;
#if LEGATO_BEFORE_MULTIPLAYER
using PlayerTransforms = global::PlayerController;
using GameplayInstaller = global::GameplayCoreSceneSetup;
#else
using GameplayInstaller = global::GameplayCoreInstaller;
#endif

namespace Legato.Gameplay
{
    [DefaultExecutionOrder(1000)]
    internal sealed class GameplayMotionDriver : MonoBehaviour
    {
        private static GameplayMotionDriver? _current;
        private DiContainer _container = null!;
        private PlayerTransforms? _player;
        private AudioTimeSyncController? _clock;
        private GameplayCoreSceneSetupData _scene = null!;
#if LEGATO_BEFORE_MULTIPLAYER
        private BeatmapData? _beatmapData;
#endif
        private Transform? _origin;
        private Transform? _nativeHud;
        private Pose _initial;
        private LaneRotation? _lanes;
        private bool _nativeRotation;
        private float _nextHudSearch;

        [Inject]
        private void Construct(DiContainer container, GameplayCoreSceneSetupData scene,
            [InjectOptional] PlayerTransforms? player, [InjectOptional] AudioTimeSyncController? clock
#if LEGATO_BEFORE_MULTIPLAYER
            , [InjectOptional] BeatmapData? beatmapData
#endif
            )
        {
            _container = container;
            _scene = scene;
            _player = player;
            _clock = clock;
#if LEGATO_BEFORE_MULTIPLAYER
            _beatmapData = beatmapData;
#endif
        }

        private void Start()
        {
            if (_player == null)
                _player = UnityEngine.Resources.FindObjectsOfTypeAll<PlayerTransforms>()
                    .FirstOrDefault(player => player.isActiveAndEnabled && player.gameObject.scene == gameObject.scene);
#if LEGATO_BEFORE_MULTIPLAYER
            Transform? origin = _player != null ? _player._headTransform.parent : null;
#else
            Transform? origin = _player != null ? _player._originTransform : null;
#endif
            _origin = origin != null ? origin.parent : null;
            if (_origin == null) return;

            if (_clock == null)
                _clock = UnityEngine.Resources.FindObjectsOfTypeAll<AudioTimeSyncController>()
                    .FirstOrDefault(clock => clock.isActiveAndEnabled && clock.gameObject.scene == _player!.gameObject.scene);

            _current?.RestoreAnchors();
            _current = this;
            _initial = new Pose(_origin.position, _origin.rotation);
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            string? characteristic = _scene.difficultyBeatmap?.parentDifficultyBeatmapSet?.beatmapCharacteristic?.serializedName;
#elif BEAT_SABER_1_34_2 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            string? characteristic = _scene.beatmapKey.beatmapCharacteristic?.serializedName;
#else
            string? characteristic = _scene.beatmapKey.characteristic.SerializedName();
#endif
            _nativeRotation = characteristic == "90Degree" || characteristic == "360Degree";
        }

        private void LateUpdate()
        {
            if (_current != this || _origin == null || GameplayMotionAnchor.Active.Count == 0) return;

            if (_nativeRotation && _nativeHud == null && Time.unscaledTime >= _nextHudSearch)
            {
                _nativeHud = UnityEngine.Resources.FindObjectsOfTypeAll<FlyingGameHUDRotation>()
                    .FirstOrDefault(hud => hud.isActiveAndEnabled && hud.gameObject.scene.IsValid())?.transform;
                _nextHudSearch = Time.unscaledTime + 0.25f;
            }

            Quaternion lane = Quaternion.identity;
#if LEGATO_BEFORE_MULTIPLAYER
            if (!_nativeRotation && _clock != null && _beatmapData is BeatmapData data)
#else
            if (!_nativeRotation && _clock != null && _scene.transformedBeatmapData is IReadonlyBeatmapData data)
#endif
            {
                _lanes ??= new LaneRotation(data, _scene.playerSpecificSettings.leftHanded, _container);
                lane = _lanes.At(_clock.songTime);
            }

            Quaternion delta = _origin.rotation * Quaternion.Inverse(_initial.rotation);
            foreach (GameplayMotionAnchor anchor in GameplayMotionAnchor.Active)
                anchor.Apply(_origin, _initial, delta, lane, _nativeHud);
        }

        private void OnDestroy()
        {
            if (_current != this) return;
            RestoreAnchors();
            _current = null;
        }

        private void RestoreAnchors()
        {
            foreach (GameplayMotionAnchor anchor in GameplayMotionAnchor.Active)
                anchor.Restore();
        }

        [HarmonyPatch(typeof(GameplayInstaller), nameof(GameplayInstaller.InstallBindings))]
        private static class Install
        {
            private static readonly Func<MonoInstallerBase, DiContainer> GetContainer =
                (Func<MonoInstallerBase, DiContainer>)Delegate.CreateDelegate(typeof(Func<MonoInstallerBase, DiContainer>),
                    AccessTools.PropertyGetter(typeof(MonoInstallerBase), "Container"));

            private static void Postfix(GameplayInstaller __instance, GameplayCoreSceneSetupData ____sceneSetupData)
            {
                GetContainer(__instance).Bind<GameplayMotionDriver>().FromNewComponentOnRoot()
                    .AsSingle().WithArguments(____sceneSetupData).NonLazy();
            }
        }
    }
}
