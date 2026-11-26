using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.PlayView;
using JetBrains.Annotations;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Zenject;
using static Vivify.VivifyController;
#if !PRE_V1_37_1
using CustomJSONData;
#endif

// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace Vivify.Controllers;

[PlayViewControllerSettings(100, "vivify")]
internal class AssetBundleDownloadViewController : BSMLResourceViewController, IPlayViewController
{
    [UIComponent("loadingbar")]
    private VerticalLayoutGroup _barGroup = null!;

    private Config _config = null!;
    private AssetDownloader _assetDownloader = null!;
    private View _currentView = View.None;

    private DownloadOperation? _downloadOperation;
    private uint _downloadChecksum;
    private bool _downloadFinished;

    [UIComponent("downloading")]
    private VerticalLayoutGroup _downloadingGroup = null!;

    private string? _downloadPath;
    private float _downloadProgress;
    private Coroutine? _downloadWaiter;

    [UIComponent("error")]
    private VerticalLayoutGroup _error = null!;

    [UIComponent("errortext")]
    private TMP_Text _errorText = null!;

    private string _lastError = string.Empty;

    private Image _loadingBar = null!;
    private SiraLog _log = null!;
    private View _newView = View.None;

    [UIComponent("percentage")]
    private TMP_Text _percentageText = null!;

    [UIComponent("tos")]
    private VerticalLayoutGroup _tosGroup = null!;

    public event Action? Finished;

    private enum View
    {
        None,
        Tos,
        Downloading,
        Error
    }

    public override string ResourceName => "Vivify.Resources.AssetBundleDownloading.bsml";

    public bool Init(StartStandardLevelParameters standardLevelParameters)
    {
        CancelDownload();
        _downloadPath = null;
        _downloadChecksum = 0;
        _downloadFinished = false;
        _downloadProgress = 0;
        _currentView = View.None;
        _newView = View.None;

#if !PRE_V1_37_1
        if (standardLevelParameters.BeatmapLevel == null ||
            standardLevelParameters.BeatmapLevel.previewMediaData is not FileSystemPreviewMediaData fileSystemPreviewMediaData)
        {
            return false;
        }

        CustomData beatmapCustomData =
            standardLevelParameters.BeatmapLevel.GetBeatmapCustomData(standardLevelParameters.BeatmapKey);
        CustomData levelCustomData = standardLevelParameters.BeatmapLevel.GetLevelCustomData();
#else
        if (standardLevelParameters.DifficultyBeatmap is not CustomDifficultyBeatmap customDifficultyBeatmap)
        {
            return false;
        }

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        if (customDifficultyBeatmap.beatmapData is not CustomBeatmapData chart)
        {
            return false;
        }

        CustomData beatmapCustomData = chart.beatmapCustomData;
        CustomData levelCustomData = chart.levelCustomData;
#else
        Version3CustomBeatmapSaveData saveData = (Version3CustomBeatmapSaveData)customDifficultyBeatmap.beatmapSaveData;
        CustomData beatmapCustomData = saveData.beatmapCustomData;
        CustomData levelCustomData = saveData.levelCustomData;
#endif
#endif

        // check is vivify map
        string[] requirements = beatmapCustomData.Get<List<object>>("_requirements")?.Cast<string>().ToArray() ?? [];
        if (!requirements.Contains(CAPABILITY))
        {
            return false;
        }

        // check if bundle already downloaded
#if !PRE_V1_37_1
        string path =
            Path.Combine(
                Path.GetDirectoryName(fileSystemPreviewMediaData._previewAudioClipPath)!,
                BUNDLE_FILE);
#else
        string path = Path.Combine(
            ((CustomBeatmapLevel)customDifficultyBeatmap.level).customLevelPath,
            BUNDLE_FILE);
#endif
        if (File.Exists(path))
        {
            return false;
        }

        uint? assetBundleChecksum =
            levelCustomData.Get<CustomData>(ASSET_BUNDLE)?.Get<uint?>(BUNDLE_CHECKSUM);

        if (assetBundleChecksum == null)
        {
            _lastError =
                $"This map is missing required assets for your game version.\n" +
                $"Please contact the mapper to update their map to include the assets.";
            _newView = View.Error;
        }
        else
        {
            _log.Info($"[{path}] not found, attempting to download remotely");
            uint checksum = assetBundleChecksum.Value;
            DownloadOperation operation = new();
            _downloadOperation = operation;
            _newView = _config.AllowDownload ? View.Downloading : View.Tos;
            if (_config.AllowDownload)
            {
                _assetDownloader.StartCoroutine(
                    DownloadAndSave(
                        path,
                        checksum,
                        operation));
            }
            else
            {
                _downloadPath = path;
                _downloadChecksum = checksum;
            }
        }

        return true;
    }

    private static BundleSaveResult SaveBundle(string path, byte[] data, DownloadOperation operation, SiraLog log)
    {
        string? temporaryPath = null;
        try
        {
            temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                for (int offset = 0; offset < data.Length;)
                {
                    if (operation.IsCancelled)
                    {
                        return new BundleSaveResult(false);
                    }

                    int count = Math.Min(81920, data.Length - offset);
                    stream.Write(data, offset, count);
                    offset += count;
                }

                if (operation.IsCancelled)
                {
                    return new BundleSaveResult(false);
                }

                stream.Flush(true);
            }

            return new BundleSaveResult(operation.Commit(temporaryPath, path));
        }
        catch (Exception exception)
        {
            log.Error($"Could not save downloaded bundle to [{path}]\n{exception}");
            return new BundleSaveResult(false, exception);
        }
        finally
        {
            if (temporaryPath != null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception exception)
                {
                    log.Warn($"Could not remove temporary bundle file [{temporaryPath}]\n{exception}");
                }
            }
        }
    }

    [UsedImplicitly]
    [Inject]
    private void Construct(SiraLog log, Config config, AssetDownloader assetDownloader)
    {
        _log = log;
        _config = config;
        _assetDownloader = assetDownloader;
    }

    // TODO: figure out a way to resolve the fact that multiplayer does NOT have enough time to download bundles
    private IEnumerator DownloadAndSave(
        string savePath,
        uint checksum,
        DownloadOperation operation)
    {
        if (!IsCurrent(operation))
        {
            yield break;
        }

        _newView = View.Downloading;
        string url = _config.BundleRepository + checksum;
        _log.Debug($"Fetching asset bundle info from [{url}]");
        using UnityWebRequest apiRequest = UnityWebRequest.Get(url);
        apiRequest.SendWebRequest();

        while (!apiRequest.isDone)
        {
            if (IsCurrent(operation))
            {
                yield return null;
                continue;
            }

            apiRequest.Abort();
            _log.Debug("Fetch cancelled");
            yield break;
        }

        if (!IsCurrent(operation))
        {
            yield break;
        }

#pragma warning disable CS0618
        if (apiRequest.isNetworkError || apiRequest.isHttpError)
        {
            if (apiRequest.isNetworkError)
            {
                _lastError = $"Network error while fetching bundle.\n{apiRequest.error}";
            }
            else if (apiRequest.isHttpError)
            {
                _lastError = $"Server sent error response code while fetching bundle.\n({apiRequest.responseCode})";
            }

            _log.Error(_lastError);
            _newView = View.Error;
            yield break;
        }
#pragma warning restore CS0618

        RepoJson repoJson = JsonUtility.FromJson<RepoJson>(apiRequest.downloadHandler.text);
        string downloadUrl = repoJson.downloadUrl;
        _log.Debug($"Attempting to download asset bundle from [{downloadUrl}]");
        using UnityWebRequest www = UnityWebRequest.Get(downloadUrl);
        www.SendWebRequest();
        while (!www.isDone)
        {
            if (IsCurrent(operation))
            {
                _downloadProgress = www.downloadProgress;
                yield return null;
                continue;
            }

            www.Abort();
            _log.Debug("Download cancelled");
            yield break;
        }

        if (!IsCurrent(operation))
        {
            yield break;
        }

#pragma warning disable CS0618
        if (www.isNetworkError || www.isHttpError)
        {
            if (www.isNetworkError)
            {
                _lastError = $"Network error while downloading bundle.\n{www.error}";
            }
            else if (www.isHttpError)
            {
                _lastError = $"Server sent error response code while downloading bundle.\n({www.responseCode})";
            }

            _log.Error(_lastError);
            _newView = View.Error;
            yield break;
        }
#pragma warning restore CS0618

        byte[] data = www.downloadHandler.data;
        SiraLog log = _log;
        Task<BundleSaveResult> saveTask = Task.Run(() => SaveBundle(savePath, data, operation, log));
        _ = saveTask.ContinueWith(
            task =>
            {
                if (task.IsFaulted)
                {
                    log.Error($"Unexpected error saving bundle to [{savePath}]\n{task.Exception}");
                }
            },
            TaskScheduler.Default);
        while (!saveTask.IsCompleted)
        {
            if (!IsCurrent(operation))
            {
                yield break;
            }

            yield return null;
        }

        if (!IsCurrent(operation))
        {
            yield break;
        }

        Exception? error = saveTask.IsFaulted ? saveTask.Exception?.GetBaseException() : saveTask.Result.Error;
        if (error != null)
        {
            _lastError = $"Could not save the downloaded bundle.\n{error.Message}";
            _newView = View.Error;
            yield break;
        }

        if (!saveTask.Result.Saved)
        {
            yield break;
        }

        _downloadProgress = 1;
        _log.Debug($"Successfully downloaded bundle to [{savePath}]");
        _downloadFinished = true;
    }

    [UsedImplicitly]
    [UIAction("accept-click")]
    private void OnAcceptClick()
    {
        _config.AllowDownload = true;
        DownloadOperation? operation = _downloadOperation;
        if (_downloadPath != null && operation != null && IsCurrent(operation))
        {
            string path = _downloadPath;
            _downloadPath = null;
            _assetDownloader.StartCoroutine(
                DownloadAndSave(
                    path,
                    _downloadChecksum,
                    operation));
        }
    }

    [UsedImplicitly]
    private void OnEarlyDismiss()
    {
        CancelDownload();
    }

    private void CancelDownload()
    {
        _downloadOperation?.Cancel();
        _downloadOperation = null;
        StopDownloadWaiter();
    }

    private void StopDownloadWaiter()
    {
        if (_downloadWaiter != null)
        {
            _assetDownloader.StopCoroutine(_downloadWaiter);
            _downloadWaiter = null;
        }
    }

    [UsedImplicitly]
    private void OnShow()
    {
        StopDownloadWaiter();
        DownloadOperation? operation = _downloadOperation;
        if (operation == null || !IsCurrent(operation))
        {
            return;
        }

        if (!_downloadFinished)
        {
            if (_newView != View.Error)
            {
                _downloadWaiter = _assetDownloader.StartCoroutine(WaitForDownload(operation));
            }
        }
        else
        {
            Finished?.Invoke();
        }
    }

    [UIAction("#post-parse")]
    private void InitializeView()
    {
        _currentView = View.None;
        _tosGroup.gameObject.SetActive(false);
        _downloadingGroup.gameObject.SetActive(false);
        _error.gameObject.SetActive(false);

        Vector2 loadingBarSize = new(0, 8);

        // shamelessly stolen from songcore
        _loadingBar = new GameObject("Loading Bar").AddComponent<Image>();
        RectTransform barTransform = (RectTransform)_loadingBar.transform;
        barTransform.SetParent(_barGroup.transform, false);
        barTransform.sizeDelta = loadingBarSize;
        Texture2D? tex = Texture2D.whiteTexture;
        Sprite? sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
        _loadingBar.sprite = sprite;
        _loadingBar.type = Image.Type.Filled;
        _loadingBar.fillMethod = Image.FillMethod.Horizontal;
        _loadingBar.color = new Color(1, 1, 1, 0.5f);

        Image loadingBackg = new GameObject("Background").AddComponent<Image>();
        RectTransform loadingBackTransform = (RectTransform)loadingBackg.transform;
        loadingBackTransform.sizeDelta = loadingBarSize;
        loadingBackTransform.SetParent(_barGroup.transform, false);
        loadingBackg.color = new Color(0, 0, 0, 0.2f);
    }

    private void Update()
    {
        if (_loadingBar == null)
        {
            return;
        }

        if (_currentView != _newView)
        {
            _currentView = _newView;
            switch (_currentView)
            {
                case View.Tos:
                    _tosGroup.gameObject.SetActive(true);
                    _downloadingGroup.gameObject.SetActive(false);
                    _error.gameObject.SetActive(false);
                    break;

                case View.Downloading:
                    _tosGroup.gameObject.SetActive(false);
                    _downloadingGroup.gameObject.SetActive(true);
                    _error.gameObject.SetActive(false);
                    break;

                case View.Error:
                    _tosGroup.gameObject.SetActive(false);
                    _downloadingGroup.gameObject.SetActive(false);
                    _error.gameObject.SetActive(true);
                    _errorText.text = _lastError;
                    break;
            }
        }

        if (_currentView != View.Downloading)
        {
            return;
        }

        _loadingBar.fillAmount = _downloadProgress;
        float percentage = _downloadProgress * 100;
        _percentageText.text = $"{percentage:0.0}%";
    }

    private bool IsCurrent(DownloadOperation operation)
    {
        return ReferenceEquals(_downloadOperation, operation) && !operation.IsCancelled;
    }

    private IEnumerator WaitForDownload(DownloadOperation operation)
    {
        while (IsCurrent(operation) && !_downloadFinished && _newView != View.Error)
        {
            yield return null;
        }

        if (IsCurrent(operation))
        {
            _downloadWaiter = null;
            if (_downloadFinished)
            {
                Finished?.Invoke();
            }
        }
    }

    internal class AssetDownloader : MonoBehaviour;

    private sealed class BundleSaveResult
    {
        public BundleSaveResult(bool saved, Exception? error = null)
        {
            Saved = saved;
            Error = error;
        }

        public bool Saved { get; }

        public Exception? Error { get; }
    }

    private sealed class DownloadOperation
    {
        private readonly object _gate = new();
        private volatile bool _cancelled;

        public bool IsCancelled => _cancelled;

        public void Cancel()
        {
            lock (_gate)
            {
                _cancelled = true;
            }
        }

        public bool Commit(string temporaryPath, string path)
        {
            // Cancellation and the final rename share a commit point, without holding the gate during writing.
            lock (_gate)
            {
                if (_cancelled)
                {
                    return false;
                }

                if (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, null);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                return true;
            }
        }
    }

    [Serializable]
    private class RepoJson
    {
        // might be ugly, but damn is it fast
#pragma warning disable SA1401
#pragma warning disable SA1307
#pragma warning disable CS8618
        public string downloadUrl;
#pragma warning restore CS8618
#pragma warning restore SA1307
#pragma warning restore SA1401
    }
}
