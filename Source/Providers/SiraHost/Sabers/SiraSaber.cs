#if LEGATO_BEFORE_MULTIPLAYER || BEAT_SABER_1_29_0 || (BEAT_SABER_1_37_1 && !LEGATO_SEPARATE_SABER_LOGIC)
#define LEGATO_LEGACY_SABER_MODEL_INIT
#endif
using SiraUtil.Extras;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace SiraUtil.Sabers
{
    /// <summary>
    /// A SiraSaber is an extra saber with some useful extension methods. The SiraSaber object is on the same GameObject as the normal Saber object, it's not an overridden version of the default Saber class.
    /// </summary>
    public class SiraSaber : MonoBehaviour
    {
        /// <summary>
        /// The saber this <see cref="SiraSaber"/> is referencing.
        /// </summary>
        public Saber Saber { get; private set; } = null!;
        internal SaberModelController Model { get; private set; } = null!;
        internal Action<Saber, Color> ColorUpdated { get; set; } = null!;

#if !LEGATO_BEFORE_MULTIPLAYER
        private NoteCutter _noteCutter = null!;
#endif
        private ColorManager _colorManager = null!;
        private SaberTypeObject _saberTypeObject = null!;
        private SaberModelProvider _saberModelProvider = null!;
#if !LEGATO_LEGACY_SABER_MODEL_INIT
        private SaberModelContainer.InitData _saberModelContainerInitData = new();
#endif
        private readonly Queue<Action> _colorProcessNextFrame = new();
        private bool _constructedThisFrame = false;

        [Inject]
        internal void Construct(
#if !LEGATO_BEFORE_MULTIPLAYER
            NoteCutter noteCutter,
#endif
            ColorManager colorManager,
            SaberModelProvider saberModelProvider
#if !LEGATO_LEGACY_SABER_MODEL_INIT
            , [InjectOptional] SaberModelContainer.InitData saberModelContainerInitData
#endif
            )
        {
#if !LEGATO_BEFORE_MULTIPLAYER
            _noteCutter = noteCutter;
#endif
            _colorManager = colorManager;
            _saberModelProvider = saberModelProvider;

#if !LEGATO_LEGACY_SABER_MODEL_INIT
            if (saberModelContainerInitData != null)
            {
                _saberModelContainerInitData = saberModelContainerInitData;
            }
#endif
        }

        internal void Setup<T>(SaberType saberType) where T : Saber
        {
            _saberTypeObject = gameObject.AddComponent<SaberTypeObject>();
            Saber saber = Saber = gameObject.AddComponent<T>();
            gameObject.layer = LayerMask.NameToLayer("Saber");
#if !LEGATO_SIRA_EARLY_CONTEXTS
            Saber._saberType = _saberTypeObject;
#endif

            GameObject top = new("Top");
            GameObject bottom = new("Bottom");
            top.transform.SetParent(transform);
            bottom.transform.SetParent(transform);
            top.transform.position = new Vector3(0f, 0f, 1f);

#if LEGATO_SIRA_EARLY_CONTEXTS
            Legato.Sira.EarlySaberNative.Configure(saber, _saberModelProvider.StockSaber(saberType), _saberTypeObject, top.transform, bottom.transform, saberType);
#elif LEGATO_BEFORE_MULTIPLAYER
            saber._topPos = top.transform;
            saber._handlePos = bottom.transform;
            saber._bottomPos = bottom.transform;
#else
            saber._saberBladeTopTransform = top.transform;
            saber._handleTransform = bottom.transform;
            saber._saberBladeBottomTransform = bottom.transform;
            saber._saberBladeTopPos = top.transform.position;
            saber._saberBladeBottomPos = bottom.transform.position;
#endif

#if !LEGATO_SIRA_EARLY_CONTEXTS
            _saberTypeObject._saberType = saberType;
#endif
            Model = _saberModelProvider.NewModel(saberType);
#if LEGATO_SIRA_EARLY_CONTEXTS
            _saberModelProvider.Attach(saber, Model);
#elif LEGATO_BEFORE_MULTIPLAYER
            Model.Init(transform, saberType);
#elif LEGATO_LEGACY_SABER_MODEL_INIT
            Model.Init(transform, Saber);
#elif BEAT_SABER_1_45_1 || BEAT_SABER_1_45_3
            Model.Init(transform, Saber, _saberModelContainerInitData.trailTintColor, gameObject.name);
#else
            Model.Init(transform, Saber, _saberModelContainerInitData.trailTintColor);
#endif
            _constructedThisFrame = true;
        }

        /// <inheritdoc/>
        protected void Update()
        {
#if !LEGATO_SIRA_EARLY_CONTEXTS
            if (Saber != null && Saber.gameObject.activeInHierarchy && Saber.enabled)
            {
                Saber saber = Saber;
#if LEGATO_BEFORE_MULTIPLAYER
                UpdateNativeSaber(saber);
#else
                Transform topTransform = saber._saberBladeTopTransform;
                Transform bottomTransform = saber._saberBladeBottomTransform;
                saber._saberBladeTopPos = topTransform.position;
                saber._saberBladeBottomPos = bottomTransform.position;
                _noteCutter.Cut(Saber);
#endif
            }

#endif
            if (_colorProcessNextFrame.Count > 0)
            {
                _colorProcessNextFrame.Dequeue().Invoke();
            }
        }

#if LEGATO_BEFORE_MULTIPLAYER && !LEGATO_SIRA_EARLY_CONTEXTS
        private static void UpdateNativeSaber(Saber saber)
        {
            for (int index = saber._swingRatingCounters.Count - 1; index >= 0; index--)
            {
                SaberSwingRatingCounter counter = saber._swingRatingCounters[index];
                if (counter.didFinish)
                {
                    counter.Deinit();
                    saber._swingRatingCounters.RemoveAt(index);
                    saber._unusedSwingRatingCounters.Add(counter);
                }
            }

            bool hasPreviousSample = saber._time > 0f;
            SaberMovementData.Data previous = saber._movementData.lastAddedData;
            Vector3 top = saber.saberBladeTopPos;
            Vector3 bottom = saber.saberBladeBottomPos;
            saber._time += Time.deltaTime;
            saber._movementData.AddNewData(top, bottom, saber._time);
            if (hasPreviousSample && !saber.disableCutting)
            {
                saber._cutter.Cut(saber, top, bottom, previous.topPos, previous.bottomPos);
            }
        }
#endif

        /// <inheritdoc/>
        protected void LateUpdate()
        {
            _constructedThisFrame = false;
        }

        /// <summary>
        /// Sets the type of the saber.
        /// </summary>
        public void SetType(SaberType newSaberType)
        {
#if LEGATO_SIRA_EARLY_CONTEXTS
            Legato.Sira.EarlySaberNative.ChangeType(_saberTypeObject, newSaberType);
#else
            _saberTypeObject._saberType = newSaberType;
#endif
            Model.SetColor(_colorManager.ColorForSaberType(newSaberType));
        }

        /// <summary>
        /// Sets the color of the saber.
        /// </summary>
        /// <param name="newColor">The new color.</param>
        public void SetColor(Color newColor)
        {
            if (!_constructedThisFrame)
            {
                Model.SetColor(newColor);
                ColorUpdated?.Invoke(Saber, newColor);
                return;
            }
            else
            {
                // Sabers created on the same frame that the model was constructed wont have their colors be updated, so we have it so the color is only set once per frame.
                _colorProcessNextFrame.Enqueue(() =>
                {
                    Model.SetColor(newColor);
                    ColorUpdated?.Invoke(Saber, newColor);
                });
            }
        }
    }
}
