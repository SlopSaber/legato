#if LEGATO_SIRA_EARLY_CONTEXTS
using System;
using System.Collections.Generic;
using SiraUtil.Extras;
using SiraUtil.Interfaces;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace SiraUtil.Sabers
{
    /// <summary>
    /// Creates copies of the stock or registered saber models.
    /// </summary>
    public class SaberModelProvider : IInitializable, IDisposable
    {
        private readonly SiraLog _log;
        private readonly DiContainer _container;
        private readonly PlayerController _player;
        private readonly ColorManager _colors;
        private readonly SaberModelRegistration? _registration;
        private readonly Dictionary<Saber, SaberModelController> _stock = new();
        private readonly Dictionary<Saber, SaberModelController> _replacements = new();

        internal SaberModelProvider(SiraLog log, DiContainer container, PlayerController player, ColorManager colors, List<SaberModelRegistration> registrations)
        {
            _log = log;
            _container = container;
            _player = player;
            _colors = colors;
            foreach (SaberModelRegistration registration in registrations)
            {
                if (registration.Priority > (_registration?.Priority ?? 0))
                {
                    _registration = registration;
                }
            }
        }

        /// <summary>
        /// Installs the registered models on the stock player's sabers.
        /// </summary>
        public void Initialize()
        {
            InstallReplacement(_player.leftSaber);
            InstallReplacement(_player.rightSaber);
        }

        /// <summary>
        /// Creates a model. A null type requests a white copy of the first saber.
        /// </summary>
        public SaberModelController NewModel(Saber.SaberType? saberType)
        {
            Saber.SaberType type = saberType ?? Saber.SaberType.SaberA;
            Saber source = StockSaber(type);
            SaberModelController? model = null;
            if (_registration != null)
            {
                try
                {
                    model = CreateRegisteredModel(type, _registration);
                }
                catch (Exception exception)
                {
                    _log.Error($"Could not create the registered saber model: {exception.Message}");
                }
            }

            model ??= Observe(source).CopyStock(source);
            model.SetColor(saberType.HasValue ? _colors.ColorForSaberType(type) : Color.white);
            model.gameObject.SetActive(true);
            return model;
        }

        /// <summary>
        /// Removes owned models and restores the stock visuals.
        /// </summary>
        public void Dispose()
        {
            foreach (SaberModelController model in _replacements.Values)
            {
                if (model != null)
                {
                    model.gameObject.SetActive(false);
                    Object.Destroy(model.gameObject);
                }
            }

            foreach (SaberModelController model in _stock.Values)
            {
                if (model != null)
                {
                    model.RestoreStock();
                    Object.Destroy(model.gameObject);
                }
            }

            _replacements.Clear();
            _stock.Clear();
        }

        internal Saber StockSaber(Saber.SaberType type) => _player.SaberForType(type);

        internal SaberModelController? GetStockPlayerModel(Saber saber)
        {
            if (saber != _player.leftSaber && saber != _player.rightSaber)
            {
                return null;
            }

            return _replacements.TryGetValue(saber, out SaberModelController model) && model != null ? model : Observe(saber);
        }

        internal void Attach(Saber saber, SaberModelController model)
        {
            Transform parent = saber.transform;
            IPreSaberModelInit pre = model.GetComponent<IPreSaberModelInit>();
            if (pre == null || pre.PreInit(parent, saber))
            {
                model.Init(parent, saber.saberType);
            }

            model.AttachNativeTrail(saber, StockSaber(saber.saberType));
            model.GetComponent<IPostSaberModelInit>()?.PostInit(parent, saber);
        }

        private SaberModelController Observe(Saber saber)
        {
            if (!_stock.TryGetValue(saber, out SaberModelController model) || model == null)
            {
                model = SaberModelController.ObserveStock(saber, _colors.ColorForSaberType(saber.saberType));
                _stock[saber] = model;
            }

            return model;
        }

        private void InstallReplacement(Saber saber)
        {
            SaberModelController stock = Observe(saber);
            if (_registration == null)
            {
                return;
            }

            SaberModelController? model = null;
            try
            {
                model = NewModel(saber.saberType);
                Attach(saber, model);
                _replacements.Add(saber, model);
                stock.SetStockVisible(false);
            }
            catch (Exception exception)
            {
                if (model != null)
                {
                    Object.Destroy(model.gameObject);
                }

                _log.Error($"Could not attach the saber model: {exception.Message}");
            }
        }

        private SaberModelController CreateRegisteredModel(Saber.SaberType type, SaberModelRegistration registration)
        {
            bool first = type == Saber.SaberType.SaberA;
            Type? modelType = first ? registration.LeftType : registration.RightType;
            if (modelType != null)
            {
                GameObject root = new(modelType.Name);
                root.SetActive(false);
                try
                {
                    return (SaberModelController)_container.InstantiateComponent(modelType, root);
                }
                catch
                {
                    Object.Destroy(root);
                    throw;
                }
            }

            SaberModelController? prefab = first ? registration.LeftTemplate : registration.RightTemplate;
            if (prefab != null)
            {
                return _container.InstantiatePrefabForComponent<SaberModelController>(prefab);
            }

            Func<SaberModelController>? create = first ? registration.LeftInstruction : registration.RightInstruction;
            SaberModelController model = create?.Invoke() ?? throw new InvalidOperationException("The saber registration has no model.");
            _container.InjectGameObject(model.gameObject);
            return model;
        }
    }
}
#endif
