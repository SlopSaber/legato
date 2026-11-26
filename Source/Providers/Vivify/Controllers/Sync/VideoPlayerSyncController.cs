using System;
using System.Collections;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.Video;
using Zenject;
#if LEGATO_SYNC_CONTROLLER_AUDIO_STATE
using AudioState = AudioTimeSyncController;
#else
using AudioState = IAudioTimeSource;
#endif

namespace Vivify.Controllers.Sync;

[RequireComponent(typeof(VideoPlayer))]
internal class VideoPlayerSyncController : MonoBehaviour, ISync
{
    private AudioTimeSyncController _audioTimeSyncController = null!;
    private SiraLog _log = null!;

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
    private AudioState.State? _lastState;
#endif
    private bool _seeking;
    private float _startTime;
    private VideoPlayer _videoPlayer = null!;

    private float SongTime => _audioTimeSyncController.songTime - _startTime;

    public void SetStartTime(float time)
    {
        _startTime = time;
    }

    private void Awake()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
        _videoPlayer.errorReceived += OnErrorRecieved;
        _videoPlayer.prepareCompleted += OnPrepareCompleted;
        _videoPlayer.skipOnDrop = false;
    }

    [Inject]
    [UsedImplicitly]
    private void Construct(
        SiraLog log,
        float startTime,
        AudioTimeSyncController audioTimeSyncController)
    {
        _log = log;
        _startTime = startTime;
        _audioTimeSyncController = audioTimeSyncController;
#if !LEGATO_VIVIFY_LEGACY_GAMEPLAY
        audioTimeSyncController.stateChangedEvent += OnStateChange;
#endif
    }

    private void OnDestroy()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_videoPlayer != null)
        {
            _videoPlayer.errorReceived -= OnErrorRecieved;
            _videoPlayer.prepareCompleted -= OnPrepareCompleted;
        }

#if !LEGATO_VIVIFY_LEGACY_GAMEPLAY
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_audioTimeSyncController != null)
        {
            _audioTimeSyncController.stateChangedEvent -= OnStateChange;
        }
#endif
    }

    private void OnEnable()
    {
        StartCoroutine(Prepare());
    }

    private void OnErrorRecieved(VideoPlayer _, string error)
    {
        _log.Error(error);
    }

    private void OnPrepareCompleted(VideoPlayer _)
    {
        OnStateChange();
    }

    private void OnSeekCompleted(VideoPlayer _)
    {
        _videoPlayer.seekCompleted -= OnSeekCompleted;
        StartCoroutine(SeekCompleteDelay());
    }

    private void OnStateChange()
    {
        if (!_videoPlayer.isPrepared)
        {
            return;
        }

        AudioState.State state = _audioTimeSyncController.state;
#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        _lastState = state;
#endif
        switch (state)
        {
            case AudioState.State.Playing:
                ResyncTime();
                _videoPlayer.Play();
                break;

            case AudioState.State.Paused:
                _videoPlayer.Pause();
                break;

            case AudioState.State.Stopped:
                _videoPlayer.Stop();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerator Prepare()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        yield return new WaitUntil(() => _videoPlayer != null && _videoPlayer.isActiveAndEnabled);
        _videoPlayer.Prepare();
    }

    private void ResyncTime()
    {
        if (_seeking)
        {
            return;
        }

        _seeking = true;
        _videoPlayer.playbackSpeed = _audioTimeSyncController.timeScale;
        _videoPlayer.seekCompleted += OnSeekCompleted;
        _videoPlayer.time = SongTime;
    }

    private IEnumerator SeekCompleteDelay()
    {
        yield return new WaitForEndOfFrame();
        _seeking = false;
    }

    private void Update()
    {
        if (!_videoPlayer.isPrepared)
        {
            return;
        }

#if LEGATO_VIVIFY_LEGACY_GAMEPLAY
        if (_lastState != _audioTimeSyncController.state)
        {
            OnStateChange();
        }

        if (_audioTimeSyncController.state != AudioState.State.Playing)
        {
            return;
        }
#endif

        if (Math.Abs(_videoPlayer.time - SongTime) > 0.2)
        {
            ResyncTime();
        }
    }
}
