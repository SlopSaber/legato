using System;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace NoodleExtensions.Managers;

public enum PlayerObject
{
    Root,
    Head,
    LeftHand,
    RightHand
}

public class NoodlePlayerTransformManager : IInitializable
{
#if LEGATO_NOODLE_LEGACY_GAMEPLAY
    private readonly PlayerController _playerController;
    private VRCenterAdjust? _roomAdjust;
#else
    private readonly PlayerTransforms _playerTransforms;
    private readonly PlayerVRControllersManager _playerVRControllersManager;
#if PRE_V1_37_1
    private readonly VRCenterAdjust _vrCenterAdjust;
#else
    private readonly IInstantiator _instantiator;
#endif
#endif
    private GameObject? _root;
    private GameObject? _head;
    private GameObject? _leftHand;
    private GameObject? _rightHand;
    private Transform? _rootTransform;
    private Transform? _headTransform;
    private Transform? _leftHandTransform;
    private Transform? _rightHandTransform;

    [UsedImplicitly]
    private NoodlePlayerTransformManager(
#if LEGATO_NOODLE_LEGACY_GAMEPLAY
        PlayerController playerController,
#else
        PlayerTransforms playerTransforms,
        PlayerVRControllersManager playerVRControllersManager,
#if PRE_V1_37_1
        VRCenterAdjust vrCenterAdjust,
#else
        IInstantiator instantiator,
#endif
#endif
        IReadonlyBeatmapData beatmapData)
    {
#if LEGATO_NOODLE_LEGACY_GAMEPLAY
        _playerController = playerController;
#else
        _playerTransforms = playerTransforms;
        _playerVRControllersManager = playerVRControllersManager;
#if PRE_V1_37_1
        _vrCenterAdjust = vrCenterAdjust;
#else
        _instantiator = instantiator;
#endif
#endif
        Active = beatmapData is CustomBeatmapData custom &&
                 custom.customEventDatas.Any(n => n.eventType == NoodleController.ASSIGN_PLAYER_TO_TRACK);
    }

    public bool Active { get; }

    public Transform Root => _rootTransform ?? throw new InvalidOperationException("No [Root] created.");

    public Transform Head => _headTransform ?? throw new InvalidOperationException("No [Head] created.");

    public Transform LeftHand => _leftHandTransform ?? throw new InvalidOperationException("No [LeftHand] created.");

    public Transform RightHand => _rightHandTransform ?? throw new InvalidOperationException("No [RightHand] created.");

    public void Initialize()
    {
        if (!Active)
        {
            return;
        }

#if LEGATO_NOODLE_LEGACY_GAMEPLAY
        _roomAdjust = _playerController._headTransform.GetComponentInParent<VRCenterAdjust>();
#endif
        _root = Create(PlayerObject.Root);
        _rootTransform = _root.transform;
        _head = Create(PlayerObject.Head);
        _leftHand = Create(PlayerObject.LeftHand);
        _rightHand = Create(PlayerObject.RightHand);
        _headTransform = _head.transform;
        _leftHandTransform = _leftHand.transform;
        _rightHandTransform = _rightHand.transform;
    }

    public GameObject GetByPlayerObject(PlayerObject playerTrackObject)
    {
        if (!Active)
        {
            throw new InvalidOperationException("Not Active.");
        }

        return playerTrackObject switch
        {
            PlayerObject.Root => _root!,
            PlayerObject.Head => _head!,
            PlayerObject.LeftHand => _leftHand!,
            PlayerObject.RightHand => _rightHand!,
            _ => throw new ArgumentOutOfRangeException(nameof(playerTrackObject), playerTrackObject, null)
        };
    }

    private GameObject Create(PlayerObject playerTrackObject)
    {
        GameObject noodleObject = new($"NoodlePlayerTransform{playerTrackObject}");
        Transform origin = noodleObject.transform;

#if LEGATO_NOODLE_LEGACY_GAMEPLAY
        Transform target = playerTrackObject switch
        {
            PlayerObject.Root => _roomAdjust != null ? _roomAdjust.transform : _playerController._headTransform.parent,
            PlayerObject.Head => _playerController._headTransform,
            PlayerObject.LeftHand => _playerController.leftSaber._vrController.transform,
            PlayerObject.RightHand => _playerController.rightSaber._vrController.transform,
            _ => throw new ArgumentOutOfRangeException(nameof(playerTrackObject), playerTrackObject, null)
        };

        if (playerTrackObject == PlayerObject.Root)
        {
            origin.SetParent(target.parent, false);
            target.SetParent(origin, true);
        }
        else
        {
            origin.SetParent(Root, false);
            GameObject roomOffset = new("NoodleRoomOffset");
            roomOffset.transform.SetParent(origin, false);
            if (_roomAdjust != null)
            {
                roomOffset.AddComponent<LegacyRoomOffset>().Initialize(_roomAdjust);
            }

            target.SetParent(roomOffset.transform, true);
        }
#else
        // _playerTransforms._leftHandTransform points to the saber instead of the hand in 1.34+
        Transform target = playerTrackObject switch
        {
            PlayerObject.Root => _playerTransforms._originTransform.parent,
            PlayerObject.Head => _playerTransforms._headTransform,
            PlayerObject.LeftHand => _playerVRControllersManager.leftHandVRController.transform,
            PlayerObject.RightHand => _playerVRControllersManager.rightHandVRController.transform,
            _ => throw new ArgumentOutOfRangeException(nameof(playerTrackObject), playerTrackObject, null)
        };

        // unparent non-root objects so our script can set their position and afterward apply the room offset
        if (playerTrackObject != PlayerObject.Root)
        {
            // can be null for inactive players in multiplayer
            if (_playerTransforms._originParentTransform == null)
            {
                _playerTransforms._originParentTransform = _playerTransforms._originTransform.parent;
            }

            origin.SetParent(_playerTransforms._originParentTransform.transform, false);

            GameObject roomOffset = new("NoodleRoomOffset");
            roomOffset.SetActive(false);
            Transform roomOffsetTransform = roomOffset.transform;
#if PRE_V1_37_1
            VRCenterAdjust vrCenterAdjust = roomOffset.AddComponent<VRCenterAdjust>();
            vrCenterAdjust._roomCenter = _vrCenterAdjust._roomCenter;
            vrCenterAdjust._roomRotation = _vrCenterAdjust._roomRotation;
            vrCenterAdjust._mainSettingsModel = _vrCenterAdjust._mainSettingsModel;
#else
            _instantiator.InstantiateComponent<VRCenterAdjust>(roomOffset);
#endif
            roomOffsetTransform.SetParent(origin);
            roomOffset.SetActive(true);
            target.SetParent(roomOffsetTransform, true);
        }
        else
        {
            origin.SetParent(target.parent, false);
            target.SetParent(origin, true);
        }

#endif
        return noodleObject;
    }
}
