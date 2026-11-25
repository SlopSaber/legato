# [Legato](https://en.wikipedia.org/wiki/Legato)

[![NuGet](https://img.shields.io/nuget/v/LegatoBS)](https://www.nuget.org/packages/LegatoBS)

A compatibility library for Beat Saber PC mods

This originally started out inside [ScoreSaber's PC Mod](https://github.com/ScoreSaber/pc-mod) and we decided to extract it into a re-usable library as we may need it for future projects or others may also stand to benefit. For us Legato keeps most game version differences out of the mod itself helping us focus on what's important

## Install

```sh
dotnet tool install --global LegatoBS.Tool

legato install src/MyMod.csproj \
  --source-version 1.29.0 \
  --target-version 1.44.1
```

Something like [ScoreSaber](https://github.com/ScoreSaber/pc-mod) uses the latest game as its source version then changes the target version for each build

The project path can be left out when the repo has one C# project. Versions can also be read from `GameVersion` or a `manifest.json` beside the project

Use `--source <path-or-url>` for a local package feed or `--no-restore` when the game references aren't on the current machine

The same setup can be added by hand:

```xml
<PropertyGroup>
  <LegatoSourceVersion>1.29.0</LegatoSourceVersion>
  <LegatoTargetVersion>1.44.1</LegatoTargetVersion>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="LegatoBS" Version="0.1.1" PrivateAssets="all" />
</ItemGroup>
```

## Versions

| Profile | Game versions |
| --- | --- |
| `0.10.1` | `0.10.1-0.10.2p1` |
| `0.11.2` | `0.11.0-0.13.2` |
| `1.0.0` | `1.0.0-1.5.0` |
| `1.11.1` | `1.6.0-1.11.1` |
| `1.12.1` | `1.12.1-1.28.0` |
| `1.29.0` | `1.29.0-1.29.1` |
| `1.29.4` | `1.29.4-1.30.2` |
| `1.34.2` | `1.31.0-1.34.2` |
| `1.37.1` | `1.34.5-1.37.5` |
| `1.38.0` | `1.38.0-1.39.1` |
| `1.40.0` | `1.40.0-1.40.13` |
| `1.42.0` | `1.41.1-1.44.1` |
| `1.45.1` | `1.44.2-1.45.2` |
| `1.45.3` | `1.45.3; 1.46.0` |

Set `LegatoTargetVersion` to the exact installed PC release, including any `p1` suffix. The ranges summarize the 120 entries in [the version catalog](Build/Legato.Versions.props), covering the original PC releases archived in BSCode from 0.10.1 through 1.46.0. Each consumer needs a build against the selected game assemblies and the dependencies for its enabled adapters. See [LIBRARY.md](LIBRARY.md) for adapter requirements.

Profiles before 1.29.0 select a smaller core for audio playback state, Unity web requests, and system polyfills. Starting at 1.0.0 the core includes difficulty keys, preview levels, and gameplay scene data. The 1.6.0 and newer historical targets also include selection subscriptions. The cover API uses `GetCoverTextureAsync` from 1.0.0 through 1.11.1 and `GetCoverSpriteAsync` from 1.12.1.

For 0.10.x, reference the game's .NET 2.0/3.5 assemblies. Use Unity coroutines to await web requests and Legato's `RequestStatus` to read the result. BSML integration for 0.11.2 and 1.11.1 is deferred.

Legato source is embedded into each plugin. Updating this package alone does not update installed plugins: rebuild and redeploy each consumer. Private fields, Harmony targets, IL patterns, native hooks, dependency APIs and shader bundles can still need changes. Confirm behavior in the game after building.

## How it works

Legato compiles its source into the mod, it picks files from the source version, target version and the project's game or mod references. Support for BSML, LeaderboardCore, SiraUtil, SongCore and other mods is included when their references exist

The project needs C# 10 or later. The [library reference](LIBRARY.md) lists the adapters available to mod code

## Porting an old mod

For a mod such as SongBrowser for example which was written for 1.29.0 and hypothetically being updated to 1.44.1:

1. Run `legato install` with `1.29.0` as the source and `1.44.1` as the target
2. Update the game references, mod dependencies and manifest
3. Build the existing source and fix anything Legato doesn't cover
4. Test the build against the target

## Building source asset bundles

`LegatoBuildAssetBundles` runs a consumer-owned static Unity exporter with the selected profile's editor. It checks the asset project's editor version, executable version and resulting UnityFS header, and records the output hash beside the build log. It never deploys assets or builds a game player. Existing builds do not start Unity unless this target is requested or `LegatoBuildAssets=true` is set.

```xml
<PropertyGroup>
  <LegatoUnityEditorRoot>D:/Software/Programs/Unity</LegatoUnityEditorRoot>
</PropertyGroup>
<ItemGroup>
  <LegatoAssetProject Include="AssetsProject2019" Profile="1.29.0"
      ExecuteMethod="BundleExporter.Build"
      OutputFile="$(MSBuildProjectDirectory)/Assets/2019/model.bundle" />
  <LegatoAssetProject Include="AssetsProject6000" Profile="1.45.3"
      ExecuteMethod="BundleExporter.Build"
      OutputFile="$(MSBuildProjectDirectory)/Assets/6000/model.bundle" />
</ItemGroup>
```

Run the mod's normal output build with `-t:LegatoBuildAssetBundles` and its explicit game/Legato target properties. To generate assets during the managed build, pass `-p:LegatoBuildAssets=true`. Exporters must write the declared output file. Use absolute output paths. Select the generated file with the existing `LegatoProfileResource` items while retaining its logical resource name.

The default editors are 2019.4.28f1 for game 1.29.0/1.29.1 and 6000.3.19f1 for game 1.46.0. Other targets require an explicit `UnityVersion` on the asset project or `LegatoAssetUnityVersion`. `UNITY_EDITOR_ROOT` can supply the editor directory. Editors must already be installed and licensed. Do not point this target at a recovered game project.

Keep one maintained source asset set with shared files or an export step feeding compatible asset projects. Project settings, packages, shader variants and script bindings must support each compiler and rendering pipeline. This runner does not downgrade a Unity project, decompile binary bundles, reconstruct missing shader source or implement unavailable engine features. Newer-only shaders still need an older-pipeline variant; that compatibility can be shared rather than copied into each mod. No legacy bundle or renderer compatibility is implied by successful packaging.

### Backporting shader sources

`LegatoBuildPortableShaders` builds one maintained shader source set for the built-in 1.29.0/1.29.1 renderer and
the 1.46.0 URP renderer. It creates isolated, owned asset projects for the actual target compilers. Authoring projects
and shader sources are never downgraded or rewritten. Use `LegatoBuildShaders=true` to run it during a managed build.

```xml
<LegatoPortableShaderSource Include="ShaderAssets/Assets" BundleName="my-shaders"
    OutputDirectory="$(MSBuildProjectDirectory)/Assets/ShaderBundle/$(LegatoTargetVersion)"
    PackageManifestPath="$(MSBuildProjectDirectory)/ShaderAssets/Packages/manifest.json" />
```

The maintained Unity 6 package manifest supplies the compatible URP dependencies for the modern asset project.
The older generated project does not install URP. Source GUIDs and relative shader/include paths are preserved.
The shared exporter compiles every shader for D3D11, retains instanced stereo variants and rejects compiler errors.
Bundle engine headers, output freshness and hashes are checked by the shared asset builder. Select the matching
bundle when embedding; a successfully built Unity 6 bundle cannot be loaded by Unity 2019.

Legato maps the URP `Core.hlsl` and fullscreen `Blit.hlsl` include paths to its shared shader library. The built-in
backend provides object/world/view/clip transforms, normals, camera/screen coordinates, 2D and eye-indexed array
sampling, linear depth, full-screen triangles and the blit input contract. URP keeps its native implementations.
On the built-in target, forward passes become `ForwardBase`; supported unlit `DepthOnly` geometry passes become
`ShadowCaster` passes so Unity can draw them into its camera depth texture. This does not add lit shadow bias or
light evaluation. The pipeline tag is removed only from generated copies. Unmapped pipeline includes and pass
types fail the build; unsupported shader functions fail compilation. This is a source API adapter, not a converter
for arbitrary compiled shaders, Shader Graphs, URP Lit, deferred lighting or renderer features.

Enable `LegatoEnablePortableRendering=true` to compile `Legato.Rendering.CameraTexturePass` into a consumer.
The caller registers an active predicate and depth texture/availability property names, optionally an output
color texture name, caller-owned material array and downsample divisor. Dispose the registration before destroying
its materials. The consumer must apply its assembly's Harmony patches for the modern renderer hook. Native Unity
rendering references and Harmony are required; Legato adds the selected legacy VR module or modern render-pipeline
references. The built-in implementation requests camera depth additively and leaves that shared camera flag enabled
on disposal, because another effect can depend on the same flag.

The same registration publishes native scene depth and an optional material chain over opaque scene color before
transparents: RenderGraph on Unity 6, camera command buffers on Unity 2019. Eye-array descriptors and per-eye copying
are retained; intermediate MSAA color resolves into single-sample targets. Published color is valid through that
camera's transparent rendering. Command buffers are cached until target dimensions/layout change; temporary GPU
targets are released after rendering. Depth availability resets at camera completion. This path does not include
transparent scenery, UI-layer exclusion, arbitrary `ScriptableRenderPass` implementations or post-processing parity.
Those features need an explicit shared backend rather than an opaque-only approximation. Builds establish compiler
compatibility; headset eyes, image orientation, native bloom and rendering order still require runtime confirmation.

## Selecting a small adapter set

For a consumer that needs only named adapters, set `LegatoEnableAutoAdapters` and `LegatoEnablePolyfills` to `false`, then enable the required features. This avoids compiling unrelated compatibility code. Existing consumers can keep automatic reference-based selection.

Set `LegatoReferenceRoot` to the selected game directory and pass `LegatoTargetVersion` explicitly. Keep per-target output paths so a historical build cannot overwrite the selected target. A static manifest can opt into `LegatoManifestFile`; the generated resource changes `gameVersion` while preserving the original manifest. Projects with their own manifest generator should retain that generator.

Optional features include gameplay ownership, environment settings, menu layout/events, beatmap keys/text, native pose prediction, transition discovery, level-list badge bindings, pause-menu layout, BSML settings, Unity API compatibility, camera effects and GPU timing. Each feature requires the game or dependency references used by its source. See the library reference for property names and ownership requirements.

Set `LegatoAliasBsmlReference=true` when the BSML assembly reference uses the `Bsml` alias. Shared BSML adapters and polyfills then resolve that explicit reference, keeping publicized embedded helper types out of the global reference scope. The default remains unaliased.

### Build metadata without API adapters

Set `LegatoMetadataOnly=true` to validate the selected game profile and pass `LegatoTargetVersion` to an existing manifest generator through `GameVersion`. This mode adds no compatibility source, reference shims or publicizer inputs. Libraries can use profile validation without a plugin manifest.

For a static plugin manifest, also set `LegatoManifestFile` to the existing source JSON and preserve its resource name with `LegatoManifestResourceName` where needed. Legato writes a target-specific intermediate copy with the selected `gameVersion`; the source JSON, plugin identity, plugin version, dependencies and other fields remain unchanged. Existing generated manifests must keep their generator instead of selecting a second static manifest. A conflicting global `GameVersion` fails the build.

Changing the build target still requires selecting a supported Legato version and rebuilding/deploying each consumer. Build metadata integration does not establish runtime API compatibility or update dependency requirements automatically.

For an existing external library manifest, set `LegatoEmbedManifest=false` and `LegatoManifestSidecar=true`. The prepared manifest is copied beside the target DLL as `$(AssemblyName).manifest`; custom packaging can also read the prepared `ManifestPath`. This preserves library assemblies without embedding plugin metadata.

Saber model compatibility: opt into `LegatoEnableSaberModels` for native resource discovery, trail field access, UV mapping and foreground material ownership. Consumers keep settings, sample generation, injection and Harmony ownership. Supply publicized native saber/trail references and BGLib.UnityExtension. `ForegroundMaterials.Apply` accepts a clone-name suffix and preserves the previous material ownership and render-queue policy.

Native suite providers can be embedded in their original plugin assemblies through explicit module flags. See LIBRARY.md for ownership, profile selection and rebuild requirements.

`LegatoEnableTweaks55Providers` selects maintained native telemetry, haptics and overlay-focus patches. Enable native type aliases, legacy type names and native beatmap aliases. Legacy analytics suppression targets only Oculus analytics logging; privacy-page access remains native. Controller focus handling retains native subscription ownership and only affects controller objects. Current OpenVR pose caching and session guards are preserved. Original sources and mod settings remain consumer-owned. Licenses and provenance are packaged under `ThirdParty/Tweaks55`; runtime confirmation remains necessary.

`DifficultySelection.Level` reads the selected native preview from level navigation. It accompanies the statistics/selection module and does not resolve a synthetic legacy key. Tweaks55 providers also select native health completion, score overloads and wall colour entry points; current metadata coverage is 35 legacy and 29 modern hook contracts.

Beatmap-key helpers expose native characteristic sorting order. Counter gameplay supplies the scene note-jump beat offset, and `CompletionResultsAdapter.SoloLevel` extracts the real native level from solo completion arguments. These retain native legacy difficulties and current beatmap keys without adding synthetic game types.

`LegatoEnablePracticePlayback` supplies native spawn-ahead timing and the existing native audio-manager type alias. Consumers retain speed, pitch correction, callback reset and seek ownership. Native type references omit the newer audio assembly on legacy profiles.

`LegatoEnableCustomLevelMedia` resolves audio and cover paths from real legacy custom-level metadata or current file-system preview media. Enable native beatmap aliases. Cached native accessors preserve current private media fields, and unsupported preview wrappers return no path. Consumers retain playback, file policy and cancellation ownership.

`LegatoEnableEnvironmentAccess` selects loaded legacy environment assets or the current addressable list, reads the original gameplay environment and resolves the native environment for a real key. Enable native type/beatmap aliases. It fails explicitly if the legacy environment list is unavailable; environment selection policy stays consumer-owned.

Environment access also exposes native scene names and list order. `LegatoEnableEnvironmentSceneLoading` loads built-in legacy scenes or current addressable scenes and releases owned addressable handles on unload. Call on the Unity main thread, supply a live coroutine host, and retain unload ownership in the consumer. Legacy duplicate scenes are distinguished by their handles.

`LegatoEnableSceneNames` supplies the ordered native scene names from a level transition setup, selecting legacy scene descriptors or current names. Enable native type aliases. Consumers retain scene loading and unload ownership.

`LegatoEnableBetterSongListProviders` selects native browser search, selection, menu restoration, table callbacks and played-map records while retaining maintained processing/cancellation code. Enable native type/beatmap aliases, beatmap keys, object discovery and custom-level folder access. Original files and settings stay consumer-owned; licensing and source provenance are packaged separately.

`LegatoEnableRuntimeServices` supplies injected coroutine start/stop access and native audio-start state. Legacy profiles use the game's persistent coroutine host; current profiles use its native coroutine service. `LegatoEnableTableSources` supplies a table data-source base with the native cell-size contract. Saber resources select current addressable assets or real loaded legacy model/trail assets, preferring prefab assets to scene instances; legacy menu asset availability still needs runtime confirmation.
