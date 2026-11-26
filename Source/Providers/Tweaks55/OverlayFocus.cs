using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
#if LEGATO_BEFORE_MULTIPLAYER
using System.Runtime.CompilerServices;
#endif
using UnityEngine;
using UnityEngine.XR;
#if LEGATO_MENU_CONTROLLER_FOCUS_CAPTURE
using OverlayControllerDeactivator = DeactivateMenuControllersOnFocusCaptureOrTrackingLost;
#elif LEGATO_CONTROLLER_FOCUS_CAPTURE
using OverlayControllerDeactivator = DeactivateVRControllersOnFocusCapture;
#endif

namespace Tweaks55.HarmonyPatches {
#if LEGATO_BEFORE_MULTIPLAYER
	[HarmonyPatch]
	static class NativeOverlayFocus {
		private sealed class CaptureState {
			internal bool Captured;
		}

		private static readonly ConditionalWeakTable<VRPlatformHelper, CaptureState> States = new();

		internal static bool HasFocus(VRPlatformHelper platform) =>
			!States.TryGetValue(platform, out var state) || !state.Captured;

		static IEnumerable<MethodBase> TargetMethods() {
			yield return AccessTools.DeclaredMethod(typeof(VRPlatformHelper), "HandleInputFocusWasCaptured");
			yield return AccessTools.DeclaredMethod(typeof(VRPlatformHelper), "HandleInputFocusWasReleased");
		}

		static void Postfix(VRPlatformHelper __instance, MethodBase __originalMethod) {
			var state = States.GetValue(__instance, _ => new CaptureState());
			state.Captured = __originalMethod.Name == "HandleInputFocusWasCaptured";
		}

		static Exception Cleanup(Exception ex) => Plugin.PatchFailed(ex);
	}
#endif

#if !LEGATO_BEFORE_MULTIPLAYER
#if !LEGATO_MENU_CONTROLLER_FOCUS_CAPTURE && !LEGATO_CONTROLLER_FOCUS_CAPTURE
    [HarmonyPatch]
    static class KeepSabersInOverlay {
        static IEnumerable<MethodBase> TargetMethods() {
#if BEAT_SABER_1_29_0
            yield return AccessTools.Method(typeof(DeactivateOnInputFocusCapture), "OnEnable");
#else
            yield return AccessTools.Method(typeof(DeactivateOnInputFocusCapture), "Setup");
#endif
            yield return AccessTools.Method(typeof(DeactivateOnInputFocusCapture), "HandleInputFocusCaptured");
        }

        static bool Prefix(DeactivateOnInputFocusCapture __instance) {
            var helper = __instance._vrPlatformHelper;
            if(!Config.Instance.keepSabersInOverlay || helper == null || __instance.GetComponentInChildren<VRController>(true) == null)
                return true;

            // Keep the native subscription ownership without deactivating controller objects.
            helper.inputFocusWasCapturedEvent -= __instance.HandleInputFocusCaptured;
            helper.inputFocusWasCapturedEvent += __instance.HandleInputFocusCaptured;
            helper.inputFocusWasReleasedEvent -= __instance.HandleInputFocusReleased;
            return false;
        }

        static Exception Cleanup(Exception ex) => Plugin.PatchFailed(ex);
    }
#else
#if LEGATO_MENU_CONTROLLER_FOCUS_CAPTURE
	[HarmonyPatch(typeof(OverlayControllerDeactivator), nameof(OverlayControllerDeactivator.UpdateMenuControllersState))]
#else
	[HarmonyPatch(typeof(OverlayControllerDeactivator), nameof(OverlayControllerDeactivator.UpdateVRControllerActiveState))]
#endif
	static class KeepSabersInOverlay {
		static bool Prefix(OverlayControllerDeactivator __instance) {
			if(!Config.Instance.keepSabersInOverlay || __instance._vrControllers == null ||
#if LEGATO_CONTROLLER_FOCUS_CAPTURE
				__instance._vrControllerGameObjects == null ||
#endif
#if LEGATO_XR_SYSTEM_STATE
				__instance._xrSystemState.hasInputFocus)
#else
				__instance._vrPlatformHelper.hasInputFocus)
#endif
				return true;

			// Without input focus the runtime may report invalid poses; VRController then keeps its last tracked pose.
			var controllers = __instance._vrControllers;
			for(var i = 0; i < controllers.Length; i++) {
				if(controllers[i] == null)
					continue;

#if LEGATO_MENU_CONTROLLER_FOCUS_CAPTURE
				var gameObject = controllers[i].gameObject;
#else
				var gameObject = __instance._vrControllerGameObjects[i];
#endif
				if(gameObject != null && !gameObject.activeSelf)
					gameObject.SetActive(true);
			}

			return false;
		}

		static Exception Cleanup(Exception ex) => Plugin.PatchFailed(ex);
	}

#endif
#endif

	// OpenXR stops updating input actions while another application has input focus,
	// so the controllers would freeze. On SteamVR, follow OpenVR's raw poses instead,
	// aligned to the game's tracking space when focus is lost.
	[HarmonyPatch(typeof(VRController), nameof(VRController.Update))]
	static class TrackControllersInOverlay {
		static bool hasWorld;
		static Pose world;
		static readonly bool[] hasOffset = new bool[2];
		static readonly Pose[] offset = new Pose[2];
		static int poseFrame = -1;
		static bool posesValid;
		static Pose head, left, right;
		static bool leftValid, rightValid;

		static void Postfix(VRController __instance) {
			if(!Config.Instance.keepSabersInOverlay)
				return;
			var node = __instance._node;
			if(node != XRNode.LeftHand && node != XRNode.RightHand)
				return;
			if(__instance._vrPlatformHelper == null ||
#if LEGATO_XR_SYSTEM_STATE
                __instance._xrSystemState.hasInputFocus
#elif LEGATO_BEFORE_MULTIPLAYER
                NativeOverlayFocus.HasFocus(__instance._vrPlatformHelper)
#else
                __instance._vrPlatformHelper.hasInputFocus
#endif
            ) {
				hasWorld = false;
				hasOffset[0] = hasOffset[1] = false;
				return;
			}
			// Without a running XR display there is no OpenXR session yet; connecting to OpenVR
			// first would make SteamVR reject the session the game creates afterwards.
			if(!XRSettings.isDeviceActive)
				return;

			if(poseFrame != Time.frameCount) {
				poseFrame = Time.frameCount;
				posesValid = OpenVRClient.TryGetPoses(out head, out left, out right, out leftValid, out rightValid);
			}
			if(!posesValid)
				return;

			if(!hasWorld) {
				if(!__instance._vrPlatformHelper.GetNodePose(XRNode.Head, 0, out var headPosition, out var headRotation))
					return;
				world = Multiply(new Pose(headPosition, headRotation), Inverse(head));
				hasWorld = true;
			}

			var index = node == XRNode.LeftHand ? 0 : 1;
			if(!(index == 0 ? leftValid : rightValid))
				return;

			var raw = Multiply(world, index == 0 ? left : right);
			var transform = __instance.transform;
			if(!hasOffset[index]) {
				// The frozen pose is the last tracked one, so it fixes the grip offset.
				offset[index] = Multiply(Inverse(raw), new Pose(transform.localPosition, transform.localRotation));
				hasOffset[index] = true;
			}
			var pose = Multiply(raw, offset[index]);
#if BEAT_SABER_1_29_0 || LEGATO_BEFORE_MULTIPLAYER
            transform.localPosition = pose.position;
            transform.localRotation = pose.rotation;
#else
            transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
#endif
		}

		static Pose Multiply(Pose a, Pose b) => new Pose(a.position + a.rotation * b.position, a.rotation * b.rotation);

		static Pose Inverse(Pose pose) {
			var rotation = Quaternion.Inverse(pose.rotation);
			return new Pose(rotation * -pose.position, rotation);
		}

		static Exception Cleanup(Exception ex) => Plugin.PatchFailed(ex);
	}

#if !LEGATO_BEFORE_MULTIPLAYER
#if !LEGATO_XR_SYSTEM_STATE
    [HarmonyPatch]
    static class KeepMusicInOverlay {
        static IEnumerable<MethodBase> TargetMethods() {
            yield return AccessTools.Method(typeof(SongPreviewPlayerPauseOnInputFocusLost), "HandleInputFocusCaptured");
            yield return AccessTools.Method(typeof(SongPreviewPlayerPauseOnInputFocusLost), "HandleInputFocusReleased");
        }
        static bool Prefix() => !Config.Instance.keepMusicInOverlay;
#else
	[HarmonyPatch(typeof(SongPreviewPlayerPauseOnInputFocusLost),nameof(SongPreviewPlayerPauseOnInputFocusLost.HandleSystemStateChange))]
	static class KeepMusicInOverlay {
		static bool Prefix(XRSystemEventType eventType) =>
			!Config.Instance.keepMusicInOverlay ||
			(eventType != XRSystemEventType.InputFocusLost && eventType != XRSystemEventType.InputFocusAcquired);

#endif

		static Exception Cleanup(Exception ex) => Plugin.PatchFailed(ex);
	}
#endif
}
