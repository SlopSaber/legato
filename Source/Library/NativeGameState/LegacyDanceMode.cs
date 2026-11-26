#if LEGATO_BEFORE_MULTIPLAYER
using System;
using UnityEngine;
using UnityEngine.XR;

namespace Legato.Gameplay {
    internal static class LegacyDanceMode {
        internal static void Drain(GameEnergyCounter energy, float amount, ref float pendingBatteryDrain) {
            if (!energy.isActiveAndEnabled || energy.noFail || energy.energy <= 0f) return;
            if (energy.energyType != GameplayModifiers.EnergyType.Battery || energy.instaFail) {
                energy.AddEnergy(-amount);
                return;
            }

            var life = 1f / energy.batteryLives;
            pendingBatteryDrain += amount;
            while (pendingBatteryDrain >= life && energy.energy > 0f) {
                pendingBatteryDrain -= life;
                energy.AddEnergy(-life);
            }
        }

        internal static (float Bottom, float Height) WallHeight(BeatmapObjectSpawnController spawner, ObstacleData obstacle) {
            var movement = spawner._beatmapObjectSpawnMovementData;
            if (obstacle.obstacleType == ObstacleType.Top) {
                return (movement._topObstaclePosY + spawner._initData.jumpOffsetY, movement._topObstacleHeight);
            }

            var bottom = movement._verticalObstaclePosY;
            var height = movement._verticalObstacleHeight;
            var type = (int)obstacle.obstacleType;
            if (type is >= 1000 and <= 4000) {
                height *= (type - 1000) / 1000f;
            } else if (type is >= 4001 and <= 4005000) {
                var encodedHeight = type - 4001;
                height *= (encodedHeight / 1000) / 1000f;
                bottom += (encodedHeight % 1000) * 0.004f;
            }
            return (bottom, height);
        }

        internal static float BeatDuration(BeatmapObjectSpawnController? spawner) =>
            spawner == null || spawner.currentBPM <= 0f ? 0f : 60f / spawner.currentBPM;

        internal static void Pulse(HapticFeedbackController? controller, float strength) {
            if (controller == null) return;
            controller.Rumble(XRNode.LeftHand, 0.08f, strength, 0f);
            controller.Rumble(XRNode.RightHand, 0.08f, strength, 0f);
        }
    }

    internal sealed class LegacyDanceFog : IDisposable {
        private readonly BloomFogSO _fog;
        private readonly BloomFogEnvironmentParams _originalStart;
        private readonly BloomFogEnvironmentParams _originalEnd;
        private readonly float _originalTransition;
        private readonly BloomFogEnvironmentParams _baseFog;
        private readonly BloomFogEnvironmentParams _dreadFog;

        internal LegacyDanceFog(BloomFogSO fog, float thickening, float minimumAttenuation) {
            _fog = fog;
            _originalStart = fog.fog0Params;
            _originalEnd = fog.fog1Params;
            _originalTransition = fog.transition;
            var end = _originalEnd == null ? _originalStart : _originalEnd;
            _baseFog = ScriptableObject.CreateInstance<BloomFogEnvironmentParams>();
            _baseFog.attenuation = Mathf.Lerp(_originalStart.attenuation, end.attenuation, _originalTransition);
            _baseFog.offset = Mathf.Lerp(_originalStart.offset, end.offset, _originalTransition);
            _dreadFog = ScriptableObject.CreateInstance<BloomFogEnvironmentParams>();
            _dreadFog.attenuation = Mathf.Max(_baseFog.attenuation * thickening, minimumAttenuation);
            _dreadFog.offset = _baseFog.offset * 0.3f;
            _fog.fog0Params = _baseFog;
            _fog.fog1Params = _dreadFog;
            if (_originalTransition == 0f) _fog.UpdateShaderParams();
            else _fog.transition = 0f;
        }

        internal void SetAmount(float amount) => _fog.transition = amount;

        public void Dispose() {
            if (_fog != null) {
                _fog.fog0Params = _originalStart;
                _fog.fog1Params = _originalEnd;
                if (_fog.transition == _originalTransition) _fog.UpdateShaderParams();
                else _fog.transition = _originalTransition;
            }
            UnityEngine.Object.Destroy(_baseFog);
            UnityEngine.Object.Destroy(_dreadFog);
        }
    }
}
#endif
