#if LEGATO_SIRA_EARLY_CONTEXTS
using System;
using System.Collections.Generic;
using HarmonyLib;
using Legato.Sira;
using SiraUtil.Affinity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SiraUtil.Sabers.Effects
{
    internal sealed class EarlyBurnMarkEffects : IDisposable, IAffinity
    {
        private readonly SaberModelManager _models;
        private readonly SiraSaberFactory _factory;
        private readonly List<Saber> _extraSabers = new();
        private readonly Dictionary<SaberBurnMarkArea, Area> _areas = new();
        private int _revision;

        public EarlyBurnMarkEffects(SaberModelManager models, SiraSaberFactory factory)
        {
            _models = models;
            _factory = factory;
            _factory.SaberCreated += SaberCreated;
            _models.ColorUpdated += ColorUpdated;
        }

        public void Dispose()
        {
            _factory.SaberCreated -= SaberCreated;
            _models.ColorUpdated -= ColorUpdated;
            foreach (Area state in _areas.Values)
            {
                state.Refresh(Array.Empty<Saber>(), _models, _revision + 1);
            }

            _areas.Clear();
            _extraSabers.Clear();
        }

        private void SaberCreated(SiraSaber saber)
        {
            _extraSabers.Add(saber.Saber);
            _revision++;
        }

        private void ColorUpdated(Saber saber, Color color)
        {
            foreach (Area state in _areas.Values)
            {
                state.Recolor(saber, color);
            }
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(SaberBurnMarkArea), "Update")]
        private void AddExtraSabers(SaberBurnMarkArea __instance)
        {
            if (!EarlyBurnMarkPatch.SupportsExtraSabers)
            {
                return;
            }

            if (_extraSabers.RemoveAll(saber => saber == null) != 0)
            {
                _revision++;
            }

            if (!_areas.TryGetValue(__instance, out Area state))
            {
                if (_extraSabers.Count == 0)
                {
                    return;
                }

                state = new Area(__instance);
                _areas.Add(__instance, state);
            }

            state.Refresh(_extraSabers, _models, _revision);
        }

        private sealed class Area
        {
            private readonly SaberBurnMarkArea _native;
            private readonly int _stockCount;
            private int _lastRevision = -1;

            internal Area(SaberBurnMarkArea native)
            {
                _native = native;
                _stockCount = Buffers.ReadSabers(native).Length;
            }

            internal void Refresh(IReadOnlyList<Saber> extra, SaberModelManager models, int revision)
            {
                if (_native == null || revision == _lastRevision)
                {
                    return;
                }

                Buffers current = Buffers.Read(_native);
                Buffers next = new(_stockCount + extra.Count);
                for (int index = 0; index < _stockCount; index++)
                {
                    next.CopySlot(index, current, index);
                }

                List<int> created = new();
                try
                {
                    for (int index = 0; index < extra.Count; index++)
                    {
                        Saber saber = extra[index];
                        int target = _stockCount + index;
                        int old = Array.IndexOf(current.Sabers, saber, _stockCount);
                        if (old >= 0)
                        {
                            next.CopySlot(target, current, old);
                        }
                        else
                        {
                            created.Add(target);
                            next.CreateSlot(target, saber, current, _native.transform, models.GetPhysicalSaberColor(saber));
                        }
                    }
                }
                catch
                {
                    foreach (int target in created)
                    {
                        next.ReleaseSlot(target);
                    }

                    throw;
                }

                next.Write(_native);
                for (int index = _stockCount; index < current.Sabers.Length; index++)
                {
                    if (current.Sabers[index] == null || Array.IndexOf(next.Sabers, current.Sabers[index], _stockCount) < 0)
                    {
                        current.ReleaseSlot(index);
                    }
                }

                _lastRevision = revision;
            }

            internal void Recolor(Saber saber, Color color)
            {
                if (_native == null)
                {
                    return;
                }

                int index = Array.IndexOf(Buffers.ReadSabers(_native), saber, _stockCount);
                if (index >= 0)
                {
                    Buffers.Read(_native).Recolor(index, color);
                }
            }
        }

        private sealed class Buffers
        {
            private static readonly AccessTools.FieldRef<SaberBurnMarkArea, Saber[]> SaberSlots =
                AccessTools.FieldRefAccess<SaberBurnMarkArea, Saber[]>("_sabers");
            private static readonly AccessTools.FieldRef<SaberBurnMarkArea, Vector3[]> PreviousPositions =
                AccessTools.FieldRefAccess<SaberBurnMarkArea, Vector3[]>("_prevBurnMarkPos");
            private static readonly AccessTools.FieldRef<SaberBurnMarkArea, bool[]> ValidPreviousPositions =
                AccessTools.FieldRefAccess<SaberBurnMarkArea, bool[]>("_prevBurnMarkPosValid");
            private static readonly AccessTools.FieldRef<SaberBurnMarkArea, LineRenderer[]> BurnLines =
                AccessTools.FieldRefAccess<SaberBurnMarkArea, LineRenderer[]>("_lineRenderers");
            private static readonly AccessTools.FieldRef<SaberBurnMarkArea, ParticleSystem[]> Sparkles =
                AccessTools.FieldRefAccess<SaberBurnMarkArea, ParticleSystem[]>("_sparkleParticleSystems");
            private static readonly AccessTools.FieldRef<SaberBurnMarkArea, ParticleSystem[]> BurnCenters =
                AccessTools.FieldRefAccess<SaberBurnMarkArea, ParticleSystem[]>("_burnMarkCenterParticleSystems");

            private readonly Vector3[] _previous;
            private readonly bool[] _previousValid;
            private readonly LineRenderer[] _lines;
            private readonly ParticleSystem[] _sparkles;
            private readonly ParticleSystem[] _centers;

            private Buffers(SaberBurnMarkArea native)
            {
                Sabers = SaberSlots(native);
                _previous = PreviousPositions(native);
                _previousValid = ValidPreviousPositions(native);
                _lines = BurnLines(native);
                _sparkles = Sparkles(native);
                _centers = BurnCenters(native);
            }

            internal Buffers(int count)
            {
                Sabers = new Saber[count];
                _previous = new Vector3[count];
                _previousValid = new bool[count];
                _lines = new LineRenderer[count];
                _sparkles = new ParticleSystem[count];
                _centers = new ParticleSystem[count];
            }

            internal Saber[] Sabers { get; }

            internal static Saber[] ReadSabers(SaberBurnMarkArea native) => SaberSlots(native);

            internal static Buffers Read(SaberBurnMarkArea native) => new(native);

            internal void Write(SaberBurnMarkArea native)
            {
                SaberSlots(native) = Sabers;
                PreviousPositions(native) = _previous;
                ValidPreviousPositions(native) = _previousValid;
                BurnLines(native) = _lines;
                Sparkles(native) = _sparkles;
                BurnCenters(native) = _centers;
            }

            internal void CopySlot(int index, Buffers previous, int old)
            {
                Sabers[index] = previous.Sabers[old];
                _previous[index] = previous._previous[old];
                _previousValid[index] = previous._previousValid[old];
                _lines[index] = previous._lines[old];
                _sparkles[index] = previous._sparkles[old];
                _centers[index] = previous._centers[old];
            }

            internal void CreateSlot(int index, Saber saber, Buffers previous, Transform parent, Color color)
            {
                Sabers[index] = saber;
                _lines[index] = Object.Instantiate(previous._lines[0], parent, false);
                _lines[index].enabled = false;
                _sparkles[index] = Object.Instantiate(previous._sparkles[0], parent, false);
                _sparkles[index].Clear();
                _centers[index] = Object.Instantiate(previous._centers[0], parent, false);
                _centers[index].Clear();
                Recolor(index, color);
            }

            internal void Recolor(int index, Color color)
            {
                ParticleSystem.MainModule sparkle = _sparkles[index].main;
                sparkle.startColor = color;
                ParticleSystem.MainModule center = _centers[index].main;
                center.startColor = color;
            }

            internal void ReleaseSlot(int index)
            {
                Destroy(_lines[index]);
                Destroy(_sparkles[index]);
                Destroy(_centers[index]);
            }

            private static void Destroy(Component? effect)
            {
                if (effect != null)
                {
                    effect.gameObject.SetActive(false);
                    Object.Destroy(effect.gameObject);
                }
            }
        }
    }
}
#endif
