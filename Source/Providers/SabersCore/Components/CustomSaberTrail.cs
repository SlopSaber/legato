using SaberComponents.Models;
using SabersCore.Models;
using SabersCore.Utilities.Common;
using SabersCore.Utilities.Extensions;
using UnityEngine;
using Zenject;
#if LEGATO_BEFORE_MULTIPLAYER
using Trail = Xft.XWeaponTrail;
#else
using Trail = SaberTrail;
#endif

namespace SabersCore.Components;

public class CustomSaberTrail : Trail
{
#if !LEGATO_BEFORE_MULTIPLAYER
    private readonly SaberMovementData customTrailMovementData = new();

    private TimeHelper timeHelper = null!;
#endif
    private Transform trailTop = null!;
    private Transform trailBottom = null!;
    private ITrailData trailData = null!;

    [Inject]
    public void Construct(
#if !LEGATO_BEFORE_MULTIPLAYER
        TimeHelper timeHelper,
#endif
        InitData initData)
    {
#if !LEGATO_BEFORE_MULTIPLAYER
        this.timeHelper = timeHelper;
#endif
        trailTop = initData.TrailTop;
        trailBottom = initData.TrailBottom;
        trailData = initData.TrailData;
        gameObject.layer = 12;
#if !LEGATO_BEFORE_MULTIPLAYER
        Legato.Models.SaberTrailAccess.SetMovementData(this, customTrailMovementData);
#endif
    }

    public float OverrideWidth { private get; set; } = 1f;
    public bool UseWidthOverride { private get; set; }
    
    public ITrailData TrailData => trailData;

    public void SetColorScheme(ColorScheme colorScheme) => SetColor(colorScheme);

    public void SetBoostColors(ColorScheme colorScheme, bool isBoostOn) =>
        UpdateBoostColors(colorScheme, isBoostOn);

    public void SetSpecificColor(Color color)
    {
        if (!trailData.UseTrailColor && trailData.ColorSchemeType is ColorSchemeType.LeftSaber or ColorSchemeType.RightSaber)
            SetColor(color);
    }

    public void SetColor(ColorScheme colorScheme)
    {
        if (trailData.UseTrailColor)
        {
            SetColor(trailData.CustomColor);
            return;
        }
        var color = colorScheme.GetColorByType(trailData.ColorSchemeType);
        SetColor(color);
    }
    
    public void UpdateBoostColors(ColorScheme colorScheme, bool isBoostOn)
    {
        if (trailData.UseTrailColor || !trailData.UseColorBoostEvents) return;
        var color = colorScheme.GetBoostColorByType(trailData.ColorSchemeType, isBoostOn);
        SetColor(color);
    }

    public void SetColor(Color color, SaberType saberType)
    {
        if (trailData.UseTrailColor) return;
        if ((saberType == SaberType.SaberA && trailData.ColorSchemeType == ColorSchemeType.LeftSaber)
            || (saberType == SaberType.SaberB && trailData.ColorSchemeType == ColorSchemeType.RightSaber))
        {
            SetColor(color);
        }
    }
    
    private void SetColor(Color color)
    {
        Legato.Models.SaberTrailAccess.SetColor(this, color * trailData.ColorMultiplier,
            MaterialProperties.Color);
    }
    
#if LEGATO_BEFORE_MULTIPLAYER
    private new void Start()
    {
        base.Start();
        Destroy(_trailRendererPrefab.gameObject);
        _trailRendererPrefab = null;
        _trailRenderer.enabled = false;
        _trailRenderer._meshRenderer.enabled = false;
        _trailRenderer.gameObject.SetActive(true);
        Legato.Models.SaberTrailAccess.SetColor(this, color, MaterialProperties.Color);
    }

    private new void OnDestroy()
    {
        if (_trailRendererPrefab != null) Destroy(_trailRendererPrefab.gameObject);
        base.OnDestroy();
    }
#else
#if !LEGATO_INJECTED_SABER_TRAIL
    private new void Awake()
    {
        // TrailFactory creates and assigns the renderer after component construction.
    }

    private void Start()
#else
    private new void Start()
#endif
    {
        // Ignored
    }
#endif

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;

#if LEGATO_BEFORE_MULTIPLAYER
        var topPos = trailData.TrailTopOffset;
        var bottomPos = trailData.TrailBottomOffset;
#else
        var topPos = trailTop.position;
        var bottomPos = trailBottom.position;
#endif

        if (UseWidthOverride)
        {
            TrailUtils.ApplyWidthMultiplier(trailData, OverrideWidth, ref topPos, ref bottomPos);
        }
        
#if LEGATO_BEFORE_MULTIPLAYER
        trailTop.localPosition = topPos;
        trailBottom.localPosition = bottomPos;
        _trailWidth = (trailTop.position - trailBottom.position).magnitude;
#else
        customTrailMovementData.AddNewData(
            topPos,
            bottomPos,
#if LEGATO_STATIC_TIME_HELPER
            TimeHelper.time);
#else
            timeHelper.Time);
#endif
#endif
    }
    
    public record InitData(Transform TrailTop, Transform TrailBottom, ITrailData TrailData);
}
