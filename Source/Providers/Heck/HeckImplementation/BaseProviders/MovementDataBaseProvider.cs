using Heck.BaseProvider;
using JetBrains.Annotations;
using Zenject;
#if LEGATO_EARLY_STANDARD_MAPS
using IPA.Utilities;
#endif
#if LEGATO_HECK_LEGACY_GAMEPLAY
using System.Reflection;
using HarmonyLib;
#endif

namespace Heck.BaseProviders;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
internal class MovementDataBaseProvider : IBaseProvider
{
    internal float[] NoteJumpMovementSpeed { get; set; } = new float[1];

    internal float[] NoteJumpStartBeatOffset { get; set; } = new float[1];

    internal float[] JumpDistance { get; set; } = new float[1];

    internal float[] PlayerHeight { get; set; } = new float[1];
}

#if LEGATO_EARLY_STANDARD_MAPS
internal sealed class MovementDataGetter : ITickable
{
    private readonly MovementDataBaseProvider _values;
    private readonly BeatmapObjectSpawnController _spawner;
    private readonly MainSettingsModel _settings;

    [UsedImplicitly]
    private MovementDataGetter(MovementDataBaseProvider values, BeatmapObjectSpawnController spawner, MainSettingsModel settings)
    {
        _values = values;
        _spawner = spawner;
        _settings = settings;
    }

    public void Tick()
    {
        _values.NoteJumpMovementSpeed[0] = FieldAccessor<BeatmapObjectSpawnController, float>.Get(_spawner, "_noteJumpMovementSpeed");
        _values.JumpDistance[0] = FieldAccessor<BeatmapObjectSpawnController, float>.Get(_spawner, "_jumpDistance");
        _values.PlayerHeight[0] = _settings.playerHeight;
    }
}
#else
internal class MovementDataGetter : IInitializable, ITickable
{
#if LEGATO_HECK_LEGACY_GAMEPLAY
    private static readonly FieldInfo _movementField = AccessTools.Field(typeof(BeatmapObjectSpawnController), "_beatmapObjectSpawnMovementData");
    private static readonly AccessTools.FieldRef<BeatmapObjectSpawnMovementData, float> _speedField = AccessTools.FieldRefAccess<BeatmapObjectSpawnMovementData, float>("_noteJumpMovementSpeed");
    private static readonly AccessTools.FieldRef<BeatmapObjectSpawnMovementData, float> _distanceField = AccessTools.FieldRefAccess<BeatmapObjectSpawnMovementData, float>("_jumpDistance");
#endif
    private readonly MovementDataBaseProvider _movementDataBaseProvider;
    private readonly PlayerSpecificSettings _playerSpecificSettings;
    private readonly PlayerHeightDetector? _playerHeightDetector;
    private readonly bool _automaticPlayerHeight;

#if LEGATO_HECK_LEGACY_GAMEPLAY
    private readonly BeatmapObjectSpawnMovementData _beatmapObjectSpawnMovementData;
    private readonly BeatmapObjectSpawnController.InitData _initData;
#elif !PRE_V1_40_8
    private readonly IVariableMovementDataProvider _variableMovementDataProvider;
    private readonly BeatmapObjectSpawnController.InitData _initData;
#else
    private readonly BeatmapObjectSpawnMovementData _beatmapObjectSpawnMovementData;
#endif

    [UsedImplicitly]
    private MovementDataGetter(
        MovementDataBaseProvider movementDataBaseProvider,
        GameplayCoreSceneSetupData gameplayCoreSceneSetupData,
        [InjectOptional] PlayerHeightDetector? playerHeightDetector,
#if LEGATO_HECK_LEGACY_GAMEPLAY
        BeatmapObjectSpawnController beatmapObjectSpawnController,
        BeatmapObjectSpawnController.InitData initData)
#elif !PRE_V1_40_8
        IVariableMovementDataProvider variableMovementDataProvider,
        BeatmapObjectSpawnController.InitData initData)
#else
        IBeatmapObjectSpawnController beatmapObjectSpawnController)
#endif
    {
        _movementDataBaseProvider = movementDataBaseProvider;
        _playerSpecificSettings = gameplayCoreSceneSetupData.playerSpecificSettings;
        _playerHeightDetector = playerHeightDetector;
        _automaticPlayerHeight = _playerSpecificSettings.automaticPlayerHeight;
#if LEGATO_HECK_LEGACY_GAMEPLAY
        _beatmapObjectSpawnMovementData = (BeatmapObjectSpawnMovementData)_movementField.GetValue(beatmapObjectSpawnController);
        _initData = initData;
#elif !PRE_V1_40_8
        _variableMovementDataProvider = variableMovementDataProvider;
        _initData = initData;
#else
        _beatmapObjectSpawnMovementData = beatmapObjectSpawnController.beatmapObjectSpawnMovementData;
#endif
    }

    public void Initialize()
    {
        if (!_automaticPlayerHeight)
        {
            _movementDataBaseProvider.PlayerHeight[0] = _playerSpecificSettings.playerHeight;
        }
    }

    public void Tick()
    {
#if LEGATO_HECK_LEGACY_GAMEPLAY
        _movementDataBaseProvider.NoteJumpMovementSpeed[0] = _speedField(_beatmapObjectSpawnMovementData);
        _movementDataBaseProvider.NoteJumpStartBeatOffset[0] = _initData.noteJumpStartBeatOffset;
        _movementDataBaseProvider.JumpDistance[0] = _distanceField(_beatmapObjectSpawnMovementData);
#elif !PRE_V1_40_8
        _movementDataBaseProvider.NoteJumpMovementSpeed[0] = _variableMovementDataProvider.noteJumpSpeed;
        _movementDataBaseProvider.NoteJumpStartBeatOffset[0] = _initData.noteJumpValue;
        _movementDataBaseProvider.JumpDistance[0] = _variableMovementDataProvider.jumpDistance;
#else
        _movementDataBaseProvider.NoteJumpMovementSpeed[0] = _beatmapObjectSpawnMovementData.noteJumpMovementSpeed;
        _movementDataBaseProvider.NoteJumpStartBeatOffset[0] = _beatmapObjectSpawnMovementData._noteJumpStartBeatOffset;
        _movementDataBaseProvider.JumpDistance[0] = _beatmapObjectSpawnMovementData.jumpDistance;
#endif

        if (_automaticPlayerHeight)
        {
            _movementDataBaseProvider.PlayerHeight[0] = _playerHeightDetector!.playerHeight;
        }
    }
}
#endif
