# Legato library reference

This lists the small reusable APIs supplied by Legato. Transparent polyfills mirror the original game or dependency API and are omitted.

| Area | API | Purpose |
| --- | --- | --- |
| Beatmaps | `BeatmapLevelsModel.GetLoadedBeatmapLevels()` | Enumerate loaded levels |
| Beatmaps | `BeatmapLevelsModel.GetLevelByHash(hash, cancellationToken)` | Find a custom level by hash |
| Beatmaps | `SongCoreLoaderEvents.SubscribeToSongsLoaded(callback)` | Subscribe to SongCore's loaded event across event signature changes |
| Beatmaps | `SongCore.Loader.RefreshSongsAsync(fullRefresh, timeout, cancellationToken)` | Refresh SongCore and wait for completion with a timeout |
| Beatmaps | `BeatmapLevel.TryGetDifficultyDetails(key, out details)` | Read difficulty metadata |
| Beatmaps | `BeatmapMaxScoreCache.GetMaxScore(level, key)` | Compute and cache maximum score |
| Coroutines | `SharedCoroutineStarterAdapter.StartCoroutine(routine)` | Adapt the 1.29.0 game coroutine runner |
| Gameplay | `ColorSchemeFactory.Create(...)` | Build a color scheme with player fallbacks |
| Gameplay | `GameplayMetadataProvider.JumpDistance(initData)` | Read or reconstruct jump distance |
| Gameplay | `GameplayMetadataProvider.ArcVisibility(settings)` | Read arc visibility |
| Gameplay | `GameplayMetadataProvider.ControllerPoses()` | Read controller offsets |
| Events | `MovementDataEvents.Initializing` | Change jump speed, value type or value before movement data initializes |
| Events | `HeadsetEvents.HeadsetUnmounted` | Observe HMD unmounts |
| Events | `HeadsetUnmountedEventArgs.SuppressGameHandling` | Prevent the game from handling an HMD unmount |
| Events | `InputFocusEvents.InputFocusEvaluated` | Inspect or change the evaluated input focus state |
| Gameplay | `OverrideEnvironmentSettingsFactory.Create(...)` | Resolve environment overrides |
| Gameplay | `PlayerSpecificSettingsFactory.Create(...)` | Create player settings |
| Platform | `IPlatformUserProvider.GetUserInfo(cancellationToken)` | Get the current platform user |
| Platform | `IPlatformAuthenticationProvider.GetAuthToken()` | Get the platform access token |
| Platform | `IPlatformAuthenticationProvider.GetCrossPlatformAccessToken(cancellationToken)` | Get the cross-platform access token |
| Platform | `IPlatformFriendsProvider.GetFriendUserIds()` | Get platform friend IDs |
| Platform | `GamePlatformAdapter` | Adapt the active game platform service |
| Presentation | `BsmlParser.Instance` | Get the BSML parser singleton |
| Presentation | `CurvedTextFactory.Create(parent, text, position)` | Create curved BSML text |
| Presentation | `SpriteFactory.Create(image)` | Create a sprite from image bytes |
| Presentation | `SpriteFactory.Destroy(sprite)` | Destroy a sprite and its texture |
| Resources | `EmbeddedResources.Read(assembly, resource)` | Read an embedded resource |
| Rooms | `RoomSettings.Center` | Get the room center |
| Rooms | `RoomSettings.Rotation` | Get the room rotation |
| XR | `XrDeviceDiscovery.RuntimeName` | Get the OpenXR runtime name |
| XR | `XrDeviceDiscovery.LegacyHeadsetModel` | Get the legacy headset model |
| XR | `XrDeviceDiscovery.GetNodeDeviceName(node)` | Get an XR node device name |
| XR | `XrDeviceDiscovery.GetControllerDeviceName(hand)` | Get a controller device name |

## Optional adapters

These APIs compile into the consumer. Enable only the features whose references and native API contracts the consumer supports. Package revisions through `0.1.2-local.35` contain the following additions; a consumer pinned to an earlier revision has only the APIs in that revision.

| Property | APIs and scope |
| --- | --- |
| `LegatoEnableBSML` | `BsmlParser.Instance`, `CurvedTextFactory.Create` with position or explicit anchors/size, `BsmlTextCompatibility`, and disposable `MainMenuEvents.Subscribe` |
| `LegatoEnableBsmlSettings` | `BsmlSettingsMenu.WaitForReady`, `Add`, `Remove`; returns the original readiness task, without an extra asynchronous wrapper |
| `LegatoEnableBeatmapKeys` | `BeatmapKeyExtensions.CharacteristicSerializedName`; characteristic metadata across key shapes |
| `LegatoEnableBeatmapText` | `BeatmapTextData.GetVersion`, `Load`, `HasData`, `Error`, `ReadText`; caller retains cancellation and stale-request checks between awaits. Currently provided for the 1.45.3 profile, including 1.46.0 |
| `LegatoEnableGameplayAccess` | `StandardGameplayAccess`; unique paused-gameplay resolution and force-failure access |
| `LegatoEnableEnvironmentSettings` | `OverrideEnvironmentSettingsFactory.CreateEmpty`; select this alone when the other gameplay factories are unnecessary |
| `LegatoEnableMenuLayout` | `MenuScreenLayout.SetDefaultScale`; native menu-screen layout fields |
| `LegatoEnableTransitions` | `TransitionMethods`; native transition overload discovery. Consumers still own their IL patterns and feature policy |
| `LegatoEnableLevelListBadges` | `LevelListBadgeBindings.Initialize`, refresh/prune/clear methods and embedded Harmony hooks. Inject stopped/ready/application callbacks before `PatchAll`; the consumer retains badge content and ranking policy |
| `LegatoEnablePauseMenu` | `PauseMenuLayout.GetManager`, `FindCanvas`, `FindBackground`; native pause-panel layout access |
| `LegatoEnablePosePrediction` | `PosePredictionPatch`; enable/disable/dispose the native OpenXR patch with an injected refresh-rate tracker and log callbacks. Native ABI and patch bytes remain runtime-sensitive |
| `LegatoEnableLegacyTypeNames` | Native type aliases and `CharacteristicCompatibility`; aliases are opt-in to avoid consumer name collisions |
| `LegatoEnableUnityCompatibility` | `ObjectDiscovery`, `DisplayCompatibility`, `GameplayCompatibility`; object/display discovery and pause/menu API changes |
| `LegatoEnableCameraEffects` | `CameraMaterialEffects`, `MirrorCameraHooks`; renderer-specific execution with injected material, activity and mirror-mask policy. Dispose registrations with their owner; rendering still needs visual confirmation |
| `LegatoEnableGpuTiming` | `GpuFrameTiming` and native timing support; inject the owning component and diagnostics. The native timing DLL must be built and deployed separately when selected |
| `LegatoUseReferencePublicizeMetadata` | Translate existing `Reference.Publicize` metadata to explicit publicizer items/options for supported publicizer tasks |

Base polyfills include `BeatmapKeyFactory`, `GameModes` and difficulty-selection subscriptions. The results adapter supplies completion reads and invalidation; tracking adapters handle spectator camera pose tracking; XR discovery supplies consistent device descriptions. These are selected by the existing feature/reference rules, not by every optional property above.

`LegatoUsesNativeRenderTiming` is a selected-profile capability for native timing build/copy targets. `LegatoManifestFile` selects a static input manifest and generates an isolated target resource before manifest consumers run; do not combine it with a separate manifest generator. These build properties do not establish runtime compatibility.

Avoid broad feature selection solely to increase adoption counts. A mod with no compatibility work can remain version-neutral without adding Legato. A mod-specific native hook, hierarchy, external dependency or IL pattern can still require a future consumer change even after shared adapters are adopted.
