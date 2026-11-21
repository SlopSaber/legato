#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Legato.Gameplay
{
    internal sealed class GameplayMotionAnchor : MonoBehaviour
    {
        internal static readonly HashSet<GameplayMotionAnchor> Active = new();
        private Func<Transform?>? _poseParent;
        private bool _followNativeHud;
        private bool _moved;
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _localPosition;
        private Quaternion _localRotation;

        internal static Behaviour Attach(Transform target, bool followNativeHud, Func<Transform?>? poseParent)
        {
            GameplayMotionAnchor anchor = target.gameObject.AddComponent<GameplayMotionAnchor>();
            anchor._poseParent = poseParent;
            anchor._followNativeHud = followNativeHud;
            return anchor;
        }

        private void OnEnable() => Active.Add(this);

        private void OnDisable()
        {
            Active.Remove(this);
            Restore();
        }

        internal void Apply(Transform origin, Pose initial, Quaternion playerDelta, Quaternion lane, Transform? nativeHud)
        {
            if (!_moved)
            {
                _position = transform.position;
                _rotation = transform.rotation;
                _localPosition = transform.localPosition;
                _localRotation = transform.localRotation;
                _moved = true;
            }

            Transform? poseParent = _poseParent?.Invoke();
            if (_followNativeHud && nativeHud != null)
            {
                transform.SetPositionAndRotation(nativeHud.TransformPoint(_position), nativeHud.rotation * _rotation);
                return;
            }

            Vector3 position = poseParent != null ? transform.position : _position;
            Quaternion rotation = poseParent != null ? transform.rotation : _rotation;
            Transform? parent = transform.parent;
            if (poseParent == null && parent != null && parent.IsChildOf(origin))
            {
                position = parent.TransformPoint(_localPosition);
                rotation = parent.rotation * _localRotation;
            }
            else if (poseParent == null || !poseParent.IsChildOf(origin))
            {
                position = origin.position + playerDelta * (position - initial.position);
                rotation = playerDelta * rotation;
            }

            transform.SetPositionAndRotation(origin.position + lane * (position - origin.position), lane * rotation);
        }

        internal void Restore()
        {
            if (!_moved) return;
            _moved = false;
            transform.localPosition = _localPosition;
            transform.localRotation = _localRotation;
        }
    }
}
