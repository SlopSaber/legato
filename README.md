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
| `1.29.0` | `1.29.0-1.29.1` |
| `1.37.1` | `1.37.1-1.37.2` |
| `1.38.0` | `1.38.0-1.39.1` |
| `1.40.0` | `1.40.0-1.40.8` |
| `1.42.0` | `1.42.0-1.44.1` |
| `1.45.1` | `1.45.1-1.45.2` |
| `1.45.3` | `1.45.3; 1.46.0` |

Local revisions through `0.1.2-local.67` include the 1.45.3 and 1.46.0 targets and the optional adapters listed in [LIBRARY.md](LIBRARY.md). The version table is a source-selection catalog; each consumer still needs a successful build against its actual game assemblies. It does not certify every adapter on every listed version.

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
