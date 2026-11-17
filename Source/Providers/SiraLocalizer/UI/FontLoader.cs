using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SiraLocalizer.Utilities;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

namespace SiraLocalizer.UI
{
    internal class FontLoader : IInitializable, IDisposable
    {
        private static readonly string[] kFontNamesToRemove = ["NotoSansJP-Medium SDF", "NotoSansKR-Medium SDF", "SourceHanSansCN-Bold-SDF-Common-1(2k)", "SourceHanSansCN-Bold-SDF-Common-2(2k)", "SourceHanSansCN-Bold-SDF-Uncommon(2k)"];
        private static readonly FontReplacementStrategy[] kFontReplacementStrategies =
        [
            new FontReplacementStrategy(
                ["Teko-Medium SDF"],
                ["Teko-Medium SDF Latin-1 Supplement", "Oswald-Medium SDF Cyrillic", "NotoSansHebrew-Medium SDF", "SourceHanSans-Medium SDF"]),
            new FontReplacementStrategy(
                ["Teko-Bold SDF"],
                ["Teko-Bold SDF Latin-1 Supplement", "Oswald-Bold SDF Cyrillic", "NotoSansHebrew-Bold SDF", "SourceHanSans-Medium SDF"]),
        ];

        private readonly SiraLog _logger;
#if BEAT_SABER_1_29_0
        private readonly CancellationTokenSource _nativeFontLoadCancellation = new();
        private readonly CancellationToken _nativeFontLoadToken;
#endif

        private readonly List<TMP_FontAsset> _fallbackFontAssets = [];
        private readonly List<TMP_FontAsset> _createdFontAssets = [];
        private readonly HashSet<TMP_FontAsset> _processedFontAssets = [];

#if BEAT_SABER_1_29_0
        private TMP_FontAsset _tekoBoldFontAsset;
        internal TMP_FontAsset tekoBoldFontAsset {
            get => _tekoBoldFontAsset != null ? _tekoBoldFontAsset
                : _tekoBoldFontAsset = Legato.Assets.FontResources.RequireLoadedBold();
            private set => _tekoBoldFontAsset = value;
        }
#else
        internal TMP_FontAsset tekoBoldFontAsset { get; private set; }
#endif

        public FontLoader(SiraLog logger)
        {
            _logger = logger;
#if BEAT_SABER_1_29_0
            _nativeFontLoadToken = _nativeFontLoadCancellation.Token;
#endif
        }

        public async void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            try
            {
                await LoadFontAssetsAsync();
            }
#if BEAT_SABER_1_29_0
            catch (OperationCanceledException) when (_nativeFontLoadToken.IsCancellationRequested) { }
#endif
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        public void Dispose()
        {
#if BEAT_SABER_1_29_0
            if (_nativeFontLoadToken.IsCancellationRequested) return;
            _nativeFontLoadCancellation.Cancel();
            _nativeFontLoadCancellation.Dispose();
#endif
            SceneManager.sceneLoaded -= OnSceneLoaded;

            foreach (TMP_FontAsset fontAsset in _processedFontAssets)
            {
                if (fontAsset == null) continue;
                fontAsset.fallbackFontAssetTable.RemoveAll(f => _createdFontAssets.Contains(f) || _fallbackFontAssets.Contains(f));
            }

            foreach (TMP_FontAsset fontAsset in _createdFontAssets)
            {
                Legato.Assets.PortableFonts.Release(fontAsset);
            }

            foreach (TMP_FontAsset fontAsset in _fallbackFontAssets)
            {
                Legato.Assets.PortableFonts.Release(fontAsset);
            }

            _processedFontAssets.Clear();
            _createdFontAssets.Clear();
            _fallbackFontAssets.Clear();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyFallbackFonts();
        }

        private async Task LoadFontAssetsAsync()
        {
            _logger.Info($"Loading fonts");

#if BEAT_SABER_1_29_0
            await LoadTekoBoldAsync();
            _nativeFontLoadToken.ThrowIfCancellationRequested();
            await LoadFallbackFontsAsync();
#else
            await Task.WhenAll(LoadFallbackFontsAsync(), LoadTekoBoldAsync());
#endif

            ApplyFallbackFonts();
        }

        private async Task LoadTekoBoldAsync()
        {
            tekoBoldFontAsset = await Legato.Assets.FontResources.LoadBoldAsync(
#if BEAT_SABER_1_29_0
                _nativeFontLoadToken
#endif
            );
        }

        private async Task LoadFallbackFontsAsync()
        {
#if BEAT_SABER_1_29_0
            var nativeSdfMaterial = Legato.Assets.FontResources.NativeSdfMaterial;
            foreach (string fontName in kFontReplacementStrategies.SelectMany(strategy => strategy.fontNamesToAdd).Distinct()) {
                var font = Legato.Assets.PortableFonts.Load(Assembly.GetExecutingAssembly(),
                    "SiraLocalizer.Resources.PortableFonts.", fontName, nativeSdfMaterial);
                _fallbackFontAssets.Add(font);
            }
            await Task.CompletedTask;
#else
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SiraLocalizer.Resources.Assets");
            AssetBundleCreateRequest assetBundleCreateRequest = await AssetBundle.LoadFromStreamAsync(stream);
            AssetBundle assetBundle = assetBundleCreateRequest.assetBundle;

            if (assetBundle == null)
            {
                _logger.Error("Failed to load fonts asset bundle; some characters may not display as expected");
                return;
            }

            await assetBundle.LoadAllAssetsAsync();

            foreach (string fontName in kFontReplacementStrategies.SelectMany(s => s.fontNamesToAdd).Distinct())
            {
                LoadFontAsset(assetBundle, fontName);
            }

            await assetBundle.UnloadAsync(false);
#endif
        }

        private void LoadFontAsset(AssetBundle assetBundle, string name)
        {
            // this is basically instant since we've called LoadAllAssetsAsync previously
            TMP_FontAsset fontAsset = assetBundle.LoadAsset<TMP_FontAsset>(name);

            if (fontAsset == null)
            {
                _logger.Error($"Font '{name}' could not be loaded; some characters may not display as expected");
                return;
            }

            _logger.Info($"Font '{name}' loaded successfully");

            _fallbackFontAssets.Add(fontAsset);
        }

        private void ApplyFallbackFonts()
        {
            if (!_fallbackFontAssets.Any()) return;

            _processedFontAssets.RemoveWhere(fontAsset => fontAsset == null);
            TMP_FontAsset[] fontAssets = Legato.Unity.ObjectDiscovery.FindLoaded<TMP_FontAsset>();
            bool changed = false;

            foreach (FontReplacementStrategy strategy in kFontReplacementStrategies)
            {
                IEnumerable<TMP_FontAsset> originalFontAssets = fontAssets.Where(f => !_processedFontAssets.Contains(f) && strategy.targetFontNames.Contains(f.name));

                foreach (TMP_FontAsset fontAsset in originalFontAssets)
                {
                    AddFallbacksToFont(fontAsset, strategy.fontNamesToAdd.Select(n => _fallbackFontAssets.Find(f => f.name == n)).Where(f => f != null));
                    changed = true;
                }
            }

            if (!changed) return;

            // Existing text needs rebuilding only when a font's fallbacks changed.
            foreach (TMP_Text text in Legato.Unity.ObjectDiscovery.FindActiveUnsorted<TMP_Text>())
            {
                text.SetAllDirty();
            }
        }

        private void AddFallbacksToFont(TMP_FontAsset fontAsset, IEnumerable<TMP_FontAsset> fallbacks)
        {
            _logger.Info($"Adding fallbacks to '{fontAsset.name}' ({fontAsset.GetInstanceID()})");

            fontAsset.fallbackFontAssetTable.RemoveAll(f => kFontNamesToRemove.Contains(f.name));
            fontAsset.fallbackFontAssetTable.InsertRange(0, fallbacks);

            if (fontAsset.m_CharacterLookupDictionary != null)
            {
                Legato.Assets.FontResources.InvalidateFallbacks(fontAsset);
            }

            _processedFontAssets.Add(fontAsset);
        }

        private record FontReplacementStrategy(string[] targetFontNames, string[] fontNamesToAdd);
    }
}
