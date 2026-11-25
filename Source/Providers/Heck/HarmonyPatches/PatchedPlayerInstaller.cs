using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck.Animation;
using Heck.Deserialize;
using SiraUtil.Affinity;
using Zenject;
#if LEGATO_HECK_LEGACY_GAMEPLAY
using System;
using System.Reflection;
using System.Reflection.Emit;
#else
using Heck.HarmonyPatches.UntransformedData;
using Heck.ReLoad;
using SiraUtil.Logging;
#endif
using DeserializerManager = Heck.Deserialize.DeserializerManager;

namespace Heck.HarmonyPatches;

#if LEGATO_HECK_LEGACY_GAMEPLAY
internal class PatchedPlayerInstaller : IAffinity, IDisposable
#else
internal class PatchedPlayerInstaller : IAffinity
#endif
{
    /*private static readonly MethodInfo _getContainer = AccessTools.PropertyGetter(
        typeof(MonoInstallerBase),
        "Container");

    private static readonly MethodInfo _bindHeckMultiPlayer = AccessTools.Method(
        typeof(PatchedPlayerInstaller),
        nameof(BindHeckMultiPlayer));*/

    private readonly DeserializerManager _deserializerManager;

#if LEGATO_HECK_LEGACY_GAMEPLAY
    private static readonly MethodInfo _transformChart = AccessTools.Method(
        typeof(BeatDataTransformHelper), nameof(BeatDataTransformHelper.CreateTransformedBeatmapData));

    private static readonly FieldInfo _sceneSetupData = AccessTools.Field(typeof(GameplayCoreSceneSetup), "_sceneSetupData");
    private readonly CodeInstruction _bindChart;

    private PatchedPlayerInstaller(DeserializerManager deserializerManager)
    {
        _deserializerManager = deserializerManager;
        _bindChart = InstanceTranspilers.EmitInstanceDelegate<Action<BeatmapData, GameplayCoreSceneSetup>>(BindLegacyChart);
    }

    public void Dispose() => InstanceTranspilers.DisposeDelegate(_bindChart);

    private void BindLegacyChart(BeatmapData chart, GameplayCoreSceneSetup installer)
    {
        var setup = (GameplayCoreSceneSetupData)_sceneSetupData.GetValue(installer);
        BindHeckSinglePlayer(setup, installer.Container, chart);
    }

    [AffinityTranspiler]
    [AffinityPatch(typeof(GameplayCoreSceneSetup), nameof(GameplayCoreSceneSetup.InstallBindings))]
    private IEnumerable<CodeInstruction> CaptureChart(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (instruction.opcode == OpCodes.Call && Equals(instruction.operand, _transformChart))
            {
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(_bindChart.opcode, _bindChart.operand);
            }
        }
    }
#else
    private readonly SiraLog _log;

    private PatchedPlayerInstaller(SiraLog log, DeserializerManager deserializerManager)
    {
        _log = log;
        _deserializerManager = deserializerManager;
    }
#endif

    /*private static void BindHeckMultiPlayer(
        PlayerSpecificSettingsNetSerializable playerSpecificSettings,
        DiContainer container)
    {
        container.Bind<bool>().WithId(HeckController.LEFT_HANDED_ID).FromInstance(playerSpecificSettings.leftHanded);
    }*/

#if LEGATO_HECK_LEGACY_GAMEPLAY
    private void BindHeckSinglePlayer(GameplayCoreSceneSetupData sceneSetupData, DiContainer container, BeatmapData transformedBeatmapData)
#else
    private void BindHeckSinglePlayer(
        GameplayCoreSceneSetupData sceneSetupData,
        DiContainer container)
#endif
    {
        bool leftHanded = sceneSetupData.playerSpecificSettings.leftHanded;
        container.Bind<bool>().WithId(HeckController.LEFT_HANDED_ID).FromInstance(leftHanded);

        Dictionary<string, Track> beatmapTracks;
        HashSet<(object? Id, DeserializedData DeserializedData)> deserializedDatas;
#if LEGATO_HECK_LEGACY_GAMEPLAY
        if (transformedBeatmapData is CustomBeatmapData customBeatmapData)
        {
            BeatmapData untransformedBeatmapData = sceneSetupData.difficultyBeatmap.beatmapData;
#else
        if (sceneSetupData.transformedBeatmapData is CustomBeatmapData customBeatmapData)
        {
            IReadonlyBeatmapData untransformedBeatmapData;
            if (sceneSetupData is HeckGameplayCoreSceneSetupData hecked)
            {
                try
                {
                    untransformedBeatmapData = hecked.UntransformedBeatmapData;
                }
                catch (System.InvalidOperationException)
                {
                    _log.Warn("No untransformed beatmap data was captured; using transformed custom data.");
                    untransformedBeatmapData = customBeatmapData;
                }
            }
            else
            {
                _log.Debug("Failed to get untransformedBeatmapData, falling back");
                untransformedBeatmapData = customBeatmapData;
            }
#endif

#if !PRE_V1_37_1
            _deserializerManager.DeserializeBeatmapData(
                sceneSetupData.beatmapLevel,
#else
            _deserializerManager.DeserializeBeatmapData(
                sceneSetupData.difficultyBeatmap,
#endif
                customBeatmapData,
                untransformedBeatmapData,
                leftHanded,
                out beatmapTracks,
                out deserializedDatas);

#if !LEGATO_HECK_LEGACY_GAMEPLAY
            if (HeckController.DebugMode && sceneSetupData.practiceSettings != null)
            {
                container.BindInterfacesAndSelfTo<ReLoader>().AsSingle().NonLazy();
            }
#endif
        }
        else
        {
            deserializedDatas = _deserializerManager.EmptyDeserialize();
            beatmapTracks = new Dictionary<string, Track>();
        }

        container.Bind<Dictionary<string, Track>>().FromInstance(beatmapTracks).AsSingle();
        deserializedDatas.Do(
            n =>
                container
                    .BindInstance(n.DeserializedData)
                    .WithId(n.Id));
        container.BindInstance(deserializedDatas);
    }

#if !LEGATO_HECK_LEGACY_GAMEPLAY
    [AffinityPostfix]
    [AffinityPatch(typeof(GameplayCoreInstaller), nameof(GameplayCoreInstaller.InstallBindings))]
    private void GameplayCoreBinder(MonoInstallerBase __instance, GameplayCoreSceneSetupData ____sceneSetupData)
    {
        BindHeckSinglePlayer(____sceneSetupData, __instance.Container);
    }
#endif

    /*[AffinityTranspiler]
    [AffinityPatch(
        typeof(MultiplayerConnectedPlayerInstaller),
        nameof(MultiplayerConnectedPlayerInstaller.InstallBindings))]
    private IEnumerable<CodeInstruction> MultiplayerConnectedTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            /*
             * PlayerSpecificSettingsNetSerializable playerSpecificSettingsForUserId = this._playersSpecificSettingsAtGameStartModel.GetPlayerSpecificSettingsForUserId(this._connectedPlayer.userId);
             * ++ BindHeckMultiPlayer(playerSpecificSettingsForUserId, base.Container);
             */
            /*.MatchForward(false, new CodeMatch(OpCodes.Stloc_0))
            .Advance(1)
            .Insert(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, _getContainer),
                new CodeInstruction(OpCodes.Call, _bindHeckMultiPlayer))
            .InstructionEnumeration();
    }*/
}
