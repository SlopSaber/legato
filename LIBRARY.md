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

These APIs compile into the consumer. Enable only the features whose references and native API contracts the consumer supports. Package revisions through `0.1.2-local.64` contain the following additions; a consumer pinned to an earlier revision has only the APIs in that revision.

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

## Additional optional adapters

| Property | APIs and behavior |
| --- | --- |
| `LegatoEnableObjectDiscovery` | Select `ObjectDiscovery` alone. `FindLoaded<T>` preserves loaded assets, inactive objects and native array order. First/sorted-active and unsorted-active queries preserve their distinct selection semantics |
| `LegatoEnableRendererLayers` | `RendererLayerMasks.Apply`; discover URP renderer data and add supplied visibility layers, grouping transparents with a supplied existing layer group. The consumer supplies masks and diagnostics |
| `LegatoEnableBsmlAudio` | `BsmlUiAudio.Manager`; resolve BSML's UI audio manager |
| `LegatoEnableBsmlTitle` | `BsmlMenuTitle.Get`; resolve the title view from BSML's current screen system |
| `LegatoEnableDetailArtwork` | `LevelDetailArtwork`; cached ref accessors for detail view, level bar and artwork image, preserving the consumer's component ownership |
| `LegatoEnableBeatmapDataCopy` | `BeatmapLevelDataCopy.CopyBasicData`; copy the appropriate native beatmap dictionary for the selected constructor era without changing the consumer's base class or key/value identities |

`BsmlSettingsMenu.Add` and `Remove` accept either the singleton or an explicitly supplied settings owner. Remove the same host that was registered. `PauseMenuLayout.InitializeLevelBar` initializes the native pause level bar only when requested, present and not already populated; it returns the initialized level for optional diagnostics.

Minimal beatmap-key selection also includes `BeatmapKeyFactory`. `CharacteristicName` preserves enum-name strings for wire formats; `CharacteristicSerializedName` uses the native serialized identifier. Rotational characteristic names can differ between these representations, so choose the API matching the existing contract.

For projects with explicit SDK imports, define profile output paths before `Sdk.targets` evaluates framework output paths. Keep existing manifest generators; static-manifest generation is for projects with an actual source JSON manifest. Independently inspect generated sidecars for historical targets as well as embedded manifests.

Recent optional APIs preserve the caller's existing behavior:

| Property | APIs and behavior |
| --- | --- |
| `LegatoEnableMenuButtons` | `BsmlMenuButtons.Register` and `Unregister` use the BSML singleton |
| `LegatoEnableGameModes` | Select `GameModes` without the base polyfills. `IsParty` compares the native enum on the 1.45.3 profile; older string profiles retain case-insensitive matching |
| `LegatoEnableLevelCharacteristics` | `BeatmapLevelCharacteristics.GetBeatSaber` selects characteristics for the Beat Saber gameplay type on the 1.45.3 profile |
| `LegatoEnableBeatmapKeys` | Also supplies `TryGetDifficultyBeatmapData`, preserving dictionary lookup success and missing-entry fallback; `Is360Degree` uses direct native comparisons |

`SpriteFactory.Create(image, pivot, pixelsPerUnit, meshType)` preserves explicit sprite geometry and returns null when image decoding fails. Its one-argument overload keeps the original BSML behavior.

`CompletionResultsAdapter.NegateMultipliedScore(ref results)` flips only the multiplied score's sign, including an already negative score. It preserves the modified score and maximum fields. This differs from `InvalidatePositiveScores`, which invalidates positive score fields; choose the operation matching the caller's score-submission policy.

The latest additions have consumer build evidence for 1.45.3 and 1.46.0. Earlier profile implementations remain source support, without new build certification from this batch. Mod-specific native patches, audio or shader assets, Addressables labels and external APIs still need their own review.

`LegatoEnableSaberDepth` supplies `SaberDepthOcclusion.Attach` and `LoadPreviewShader`. Attach the depth pass to its owning camera, supply the saber layer mask, displacement layer, an eligibility predicate and a one-time diagnostic callback. The shared implementation preserves the RenderObjects filters, screen-displacement checks, MSAA depth copy and pass ordering. The consumer owns camera policy and shader resources. Preview loading retains the caller's fallback shader and unloads the temporary bundle. Both operations preserve the earlier no-op/fallback path outside the 1.45.3 API profile; runtime rendering still needs visual confirmation.

`LegatoEnableNumericText` supplies `NumericTextFormatter.Publish` for one value or a left/right pair. The 1.45.3 profile retains the 128-character buffer, current culture, formatting fallback and unchanged-text suppression. Earlier profiles keep direct string formatting. Each displayed value group needs its own formatter instance. Consumers retain sampling, precision, labels and update cadence.

`LegatoEnableResultsCopy` supplies the internal abstract `CompletionResultsCopy` base. Its protected constructor copies the selected native completion-results fields through the appropriate native constructor, including modern score/max fields, missed cuts and start time. A consumer subclass retains its own metadata and remains assignable to native `LevelCompletionResults`; its immediate base type changes to the shared class. Use only where that inheritance change is compatible with the consumer contract.

`ObjectDiscovery.FindAnyActive<T>` preserves an unsorted arbitrary active-object query on current profiles. `FindIncludingInactiveUnsorted<T>` includes inactive scene objects without replacing the query with loaded-asset discovery. Older API paths retain their native ordering; newly added paths require consumer build evidence before historical compatibility is claimed.

Set `LegatoEnableReferenceShims=false` when a minimal consumer owns its complete reference list; this prevents automatic game-assembly additions and native type-name collisions. With automatic adapters disabled, completion-score reads/negation require `LegatoEnableResults=true` from local revision 59 onward. Earlier package revisions retain their original selection behavior. Reference-based default selection is unchanged.

`BeatmapLevelCharacteristics.HasDifficulty` preserves native key enumeration on modern profiles, including the caller's original gameplay-type scope, and native data lookup on older profiles. `LegatoEnableColorOverrideAlias` supplies the compile-time `LegatoColorOverrideType` alias for the native color-override enum; it introduces no runtime wrapper or public type-identity change.

`LegatoAliasBsmlReference=true` selects the `Bsml` assembly alias for BSML adapters and polyfills. Consumers must preserve that alias in the final compiler reference metadata when a publicizer replaces references. The anchored curved-text factory retains a native `CreateText` fallback for older BSML profiles.

### Build metadata without API adapters

Set `LegatoMetadataOnly=true` to validate the selected game profile and pass `LegatoTargetVersion` to an existing manifest generator through `GameVersion`. This mode adds no compatibility source, reference shims or publicizer inputs. Libraries can use profile validation without a plugin manifest.

For a static plugin manifest, also set `LegatoManifestFile` to the existing source JSON and preserve its resource name with `LegatoManifestResourceName` where needed. Legato writes a target-specific intermediate copy with the selected `gameVersion`; the source JSON, plugin identity, plugin version, dependencies and other fields remain unchanged. Existing generated manifests must keep their generator instead of selecting a second static manifest. A conflicting global `GameVersion` fails the build.

Changing the build target still requires selecting a supported Legato version and rebuilding/deploying each consumer. Build metadata integration does not establish runtime API compatibility or update dependency requirements automatically.

For an existing external library manifest, set `LegatoEmbedManifest=false` and `LegatoManifestSidecar=true`. The prepared manifest is copied beside the target DLL as `$(AssemblyName).manifest`; custom packaging can also read the prepared `ManifestPath`. This preserves library assemblies without embedding plugin metadata.
