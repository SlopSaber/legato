#if LEGATO_SIRA_EARLY_CONTEXTS
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Xft;

namespace Legato.Sira
{
    internal static class EarlySaberNative
    {
        private static readonly AccessTools.FieldRef<Saber, Transform> Top = AccessTools.FieldRefAccess<Saber, Transform>("_topPos");
        private static readonly AccessTools.FieldRef<Saber, Transform> Bottom = AccessTools.FieldRefAccess<Saber, Transform>("_bottomPos");
        private static readonly AccessTools.FieldRef<Saber, Transform> Handle = AccessTools.FieldRefAccess<Saber, Transform>("_handlePos");
        private static readonly AccessTools.FieldRef<Saber, VRController> Controller = AccessTools.FieldRefAccess<Saber, VRController>("_vrController");
        private static readonly AccessTools.FieldRef<Saber, SaberTypeObject> TypeObject = AccessTools.FieldRefAccess<Saber, SaberTypeObject>("_saberType");
        private static readonly AccessTools.FieldRef<SaberTypeObject, Saber.SaberType> TypeValue = AccessTools.FieldRefAccess<SaberTypeObject, Saber.SaberType>("_saberType");
        private static readonly AccessTools.FieldRef<SaberWeaponTrail, ColorManager> Colors = AccessTools.FieldRefAccess<SaberWeaponTrail, ColorManager>("_colorManager");
        private static readonly AccessTools.FieldRef<SaberWeaponTrail, Color> TrailTint = AccessTools.FieldRefAccess<SaberWeaponTrail, Color>("_multiplierSaberColor");
        private static readonly AccessTools.FieldRef<SetSaberGlowColor, MeshRenderer> GlowRenderer = AccessTools.FieldRefAccess<SetSaberGlowColor, MeshRenderer>("_meshRenderer");
        private static readonly AccessTools.FieldRef<SetSaberGlowColor, Color> GlowTint = AccessTools.FieldRefAccess<SetSaberGlowColor, Color>("_multiplierSaberColor");
        private static readonly AccessTools.FieldRef<XWeaponTrail, XWeaponTrailRenderer> TrailPrefab = AccessTools.FieldRefAccess<XWeaponTrail, XWeaponTrailRenderer>("_trailRendererPrefab");
        private static readonly AccessTools.FieldRef<XWeaponTrail, Transform> TrailStart = AccessTools.FieldRefAccess<XWeaponTrail, Transform>("_pointStart");
        private static readonly AccessTools.FieldRef<XWeaponTrail, Transform> TrailEnd = AccessTools.FieldRefAccess<XWeaponTrail, Transform>("_pointEnd");
        private static readonly AccessTools.FieldRef<XWeaponTrail, int> TrailFrames = AccessTools.FieldRefAccess<XWeaponTrail, int>("_maxFrame");
        private static readonly AccessTools.FieldRef<XWeaponTrail, int> TrailGranularity = AccessTools.FieldRefAccess<XWeaponTrail, int>("_granularity");
        private static readonly AccessTools.FieldRef<XWeaponTrail, Color> OwnedColor = AccessTools.FieldRefAccess<XWeaponTrail, Color>("_color");
        private static readonly AccessTools.FieldRef<XWeaponTrail, int> TrailWarmup = AccessTools.FieldRefAccess<XWeaponTrail, int>("_skipFirstFrames");
        private static readonly AccessTools.FieldRef<Rotate, Vector3> RotationOrigin = AccessTools.FieldRefAccess<Rotate, Vector3>("_startRotationAngles");
        private static readonly ConditionalWeakTable<SaberWeaponTrail, Tint> TrailColors = new();

        internal static void Configure(Saber saber, Saber template, SaberTypeObject type, Transform top, Transform bottom, Saber.SaberType saberType)
        {
            Top(saber) = top;
            Bottom(saber) = bottom;
            Handle(saber) = bottom;
            Controller(saber) = Controller(template);
            TypeObject(saber) = type;
            ChangeType(type, saberType);
        }

        internal static void ChangeType(SaberTypeObject type, Saber.SaberType value) => TypeValue(type) = value;

        internal static ColorManager? GetColors(Saber? saber)
        {
            SaberWeaponTrail? trail = saber == null ? null : saber.GetComponent<SaberWeaponTrail>();
            return trail == null ? null : Colors(trail);
        }

        internal static MeshRenderer GetRenderer(SetSaberGlowColor glow) => GlowRenderer(glow);

        internal static Color GetTint(SetSaberGlowColor glow) => GlowTint(glow);

        internal static void CopyRotation(Rotate source, GameObject target)
        {
            target.transform.localEulerAngles = RotationOrigin(source);
            Rotate rotation = target.AddComponent<Rotate>();
            rotation._rotationVector = source._rotationVector;
            rotation._speed = source._speed;
        }

        internal static XWeaponTrail? CreateTrail(Transform parent, SaberWeaponTrail? template, Saber saber, out Color tint)
        {
            tint = Color.white;
            if (template == null || TrailPrefab(template) == null)
            {
                return null;
            }

            GameObject root = new("Saber Trail") { layer = parent.gameObject.layer };
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            XWeaponTrail trail = root.AddComponent<XWeaponTrail>();
            TrailPrefab(trail) = TrailPrefab(template);
            TrailStart(trail) = saber.saberBladeBottomPosTransform;
            TrailEnd(trail) = saber.saberBladeTopPosTransform;
            TrailFrames(trail) = TrailFrames(template);
            TrailGranularity(trail) = TrailGranularity(template);
            TrailWarmup(trail) = TrailWarmup(template);
            tint = TrailTint(template);
            return trail;
        }

        internal static void SetOwnedTrailColor(XWeaponTrail trail, Color color) => OwnedColor(trail) = color;

        internal static void SetTrailColor(SaberWeaponTrail trail, Color color)
        {
            TrailColors.GetValue(trail, _ => new Tint()).Color = color * TrailTint(trail);
        }

        internal static void RestoreTrailColor(SaberWeaponTrail trail) => TrailColors.Remove(trail);

        [HarmonyPatch(typeof(SaberWeaponTrail), "Color", MethodType.Getter)]
        private static class TrailColorPatch
        {
            private static bool Prefix(SaberWeaponTrail __instance, ref Color __result)
            {
                if (!TrailColors.TryGetValue(__instance, out Tint tint))
                {
                    return true;
                }

                __result = tint.Color;
                return false;
            }
        }

        private sealed class Tint
        {
            public Color Color { get; set; }
        }
    }
}
#endif
