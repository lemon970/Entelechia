using Godot;
using Entelechia.EntelechiaCode;
using System.Runtime.CompilerServices;

namespace Entelechia.EntelechiaCode.Animation;

public sealed class EntelechiaCombatAnimationDriver
{
    public const string NodeName = "AnimationDriver";
    private const float IdleLiftDuration = 0.66f;
    private const float IdleSettleDuration = 0.67f;
    private const float IdleReturnDuration = 0.67f;
    private const float AttackWindupDuration = 0.12f;
    private const float AttackStrikeDuration = 0.14f;
    private const float AttackHoldDuration = 0.06f;
    private const float AttackReturnDuration = 0.28f;
    private const float CastLiftDuration = 0.18f;
    private const float CastPulseDuration = 0.14f;
    private const float CastReturnDuration = 0.28f;
    private const float BlockSettleDuration = 0.10f;
    private const float BlockReboundDuration = 0.08f;
    private const float BlockReturnDuration = 0.16f;
    private const float HurtRecoilDuration = 0.07f;
    private const float HurtSnapDuration = 0.06f;
    private const float HurtReturnDuration = 0.13f;
    private const float HpCostContractDuration = 0.12f;
    private const float HpCostPulseDuration = 0.10f;
    private const float HpCostReturnDuration = 0.20f;
    private const float HealLiftDuration = 0.14f;
    private const float HealHoldDuration = 0.10f;
    private const float HealReturnDuration = 0.22f;
    public const float DeathDuration = 1.10f;
    private const float DeathBloodRiseDuration = 0.28f;
    private const float DeathBloodFadeDuration = 0.55f;
    private const float DeathBloodFadeDelay = 0.45f;

    private static readonly ConditionalWeakTable<Node, EntelechiaCombatAnimationDriver> Drivers = new();

    private readonly Sprite2D _body;
    private readonly Sprite2D _slashArc;
    private readonly Sprite2D _impactBurst;
    private readonly Sprite2D _bloodDrops;
    private readonly Vector2 _baseBodyPosition;
    private readonly Vector2 _baseBodyScale;
    private readonly float _baseBodyRotation;
    private readonly Color _baseBodyModulate;
    private readonly bool _baseBodyVisible;
    private readonly int _baseBodyZIndex;
    private Tween? _activeTween;
    private bool _dead;
    private bool _cleanedUp;

    public EntelechiaCombatAnimationDriver(
        Sprite2D body,
        Sprite2D slashArc,
        Sprite2D impactBurst,
        Sprite2D bloodDrops)
    {
        _body = body;
        _slashArc = slashArc;
        _impactBurst = impactBurst;
        _bloodDrops = bloodDrops;
        _baseBodyPosition = body.Position;
        _baseBodyScale = body.Scale;
        _baseBodyRotation = body.Rotation;
        _baseBodyModulate = body.Modulate;
        _baseBodyVisible = body.Visible;
        _baseBodyZIndex = body.ZIndex;
    }

    public static void Register(Node key, EntelechiaCombatAnimationDriver driver)
    {
        Drivers.Remove(key);
        Drivers.Add(key, driver);
    }

    public static bool TryGet(Node key, out EntelechiaCombatAnimationDriver? driver)
    {
        return Drivers.TryGetValue(key, out driver);
    }


    public void PlayIdle()
    {
        if (_dead) return;

        ResetVisuals();

        MainFile.Logger.Info("[AnimationDriver] PlayIdle start");

        var tween = _body.CreateTween();
        _activeTween = tween;
        tween.SetLoops(0);
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(0f, -2f), IdleLiftDuration);
        tween.Parallel().TweenProperty(
            _body,
            "scale",
            new Vector2(_baseBodyScale.X * 1.002f, _baseBodyScale.Y * 0.998f),
            IdleLiftDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(0f, 1f), IdleSettleDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale, IdleSettleDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition, IdleReturnDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale, IdleReturnDuration);
    }

    public void PlayAttack(EntelechiaAnimationVariant variant = EntelechiaAnimationVariant.Normal)
    {
        if (_dead) return;

        ResetVisuals();

        MainFile.Logger.Info($"[AnimationDriver] PlayAttack start variant={variant}");

        var tween = _body.CreateTween();
        _activeTween = tween;
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(-14f, -4f), AttackWindupDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale * 0.995f, AttackWindupDuration);

        tween.TweenCallback(Callable.From(() => PrepareSlashArc(variant)));

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(72f, 8f), AttackStrikeDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale * 1.06f, AttackStrikeDuration);
        tween.Parallel().TweenProperty(_body, "rotation", _baseBodyRotation + Mathf.DegToRad(2.5f), AttackStrikeDuration);
        tween.Parallel().TweenProperty(_slashArc, "modulate", new Color(1f, 1f, 1f, 0.85f), AttackStrikeDuration * 0.5f);

        tween.TweenInterval(AttackHoldDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition, AttackReturnDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale, AttackReturnDuration);
        tween.Parallel().TweenProperty(_body, "rotation", _baseBodyRotation, AttackReturnDuration);
        tween.Parallel().TweenProperty(_slashArc, "modulate", new Color(1f, 1f, 1f, 0f), AttackReturnDuration);

        tween.TweenCallback(Callable.From(FinishEffectAndResumeIdle));
    }

    public void PlayCast(EntelechiaAnimationVariant variant = EntelechiaAnimationVariant.Normal)
    {
        if (_dead) return;

        ResetVisuals();

        MainFile.Logger.Info($"[AnimationDriver] PlayCast start variant={variant}");

        var tint = variant switch
        {
            EntelechiaAnimationVariant.Blood => new Color(1f, 0.72f, 0.80f, 1f),
            EntelechiaAnimationVariant.Power => new Color(1f, 0.78f, 0.92f, 1f),
            _ => new Color(1f, 0.82f, 0.88f, 1f)
        };
        var impactAlpha = variant switch
        {
            EntelechiaAnimationVariant.Power => 0.45f,
            EntelechiaAnimationVariant.Blood => 0.42f,
            _ => 0.35f
        };
        var impactScale = variant switch
        {
            EntelechiaAnimationVariant.Power => 0.27f,
            EntelechiaAnimationVariant.Blood => 0.25f,
            _ => 0.22f
        };

        PrepareImpactBurst(variant, impactScale);

        var tween = _body.CreateTween();
        _activeTween = tween;
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(0f, -7f), CastLiftDuration);
        tween.Parallel().TweenProperty(_body, "modulate", tint, CastLiftDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", impactAlpha * 0.55f, CastLiftDuration);

        tween.TweenProperty(_body, "scale", _baseBodyScale * 1.018f, CastPulseDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * impactScale, CastPulseDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", impactAlpha, CastPulseDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition, CastReturnDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale, CastReturnDuration);
        tween.Parallel().TweenProperty(_body, "modulate", _baseBodyModulate, CastReturnDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * impactScale * 1.18f, CastReturnDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", 0f, CastReturnDuration);

        tween.TweenCallback(Callable.From(FinishEffectAndResumeIdle));
    }

    public void PlayBlock()
    {
        if (_dead) return;

        ResetVisuals();

        MainFile.Logger.Info("[AnimationDriver] PlayBlock start");

        PrepareBlockBurst();

        var tween = _body.CreateTween();
        _activeTween = tween;
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(-7f, 2f), BlockSettleDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale * 0.993f, BlockSettleDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * 0.16f, BlockSettleDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", 0.22f, BlockSettleDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(2f, -1f), BlockReboundDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale, BlockReboundDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * 0.18f, BlockReboundDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", 0.12f, BlockReboundDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition, BlockReturnDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale, BlockReturnDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * 0.20f, BlockReturnDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", 0f, BlockReturnDuration);

        tween.TweenCallback(Callable.From(FinishEffectAndResumeIdle));
    }

    public void PlayHurt()
    {
        if (_dead) return;

        ResetVisuals();
        MainFile.Logger.Info("[AnimationDriver] PlayHurt start");
        PrepareHurtBurst();

        var hitTint = new Color(1f, 0.54f, 0.62f, _baseBodyModulate.A);
        var recoveryTint = new Color(1f, 0.82f, 0.86f, _baseBodyModulate.A);
        var tween = _body.CreateTween();
        _activeTween = tween;
        tween.SetTrans(Tween.TransitionType.Quad);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(-14f, -2f), HurtRecoilDuration);
        tween.Parallel().TweenProperty(_body, "modulate", hitTint, HurtRecoilDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * 0.20f, HurtRecoilDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", 0.55f, HurtRecoilDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(4f, 1f), HurtSnapDuration);
        tween.Parallel().TweenProperty(_body, "modulate", recoveryTint, HurtSnapDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * 0.22f, HurtSnapDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", 0.28f, HurtSnapDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition, HurtReturnDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale, HurtReturnDuration);
        tween.Parallel().TweenProperty(_body, "modulate", _baseBodyModulate, HurtReturnDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * 0.25f, HurtReturnDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", 0f, HurtReturnDuration);

        tween.TweenCallback(Callable.From(FinishEffectAndResumeIdle));
    }

    public void PlayHpCost()
    {
        if (_dead) return;

        ResetVisuals();
        MainFile.Logger.Info("[AnimationDriver] PlayHpCost start");
        PrepareBloodDrops();

        var bloodTint = new Color(0.78f, 0.42f, 0.48f, _baseBodyModulate.A);
        var tween = _body.CreateTween();
        _activeTween = tween;
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(0f, 4f), HpCostContractDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale * 0.988f, HpCostContractDuration);
        tween.Parallel().TweenProperty(_body, "modulate", bloodTint, HpCostContractDuration);
        tween.Parallel().TweenProperty(_bloodDrops, "scale", Vector2.One * 0.20f, HpCostContractDuration);
        tween.Parallel().TweenProperty(_bloodDrops, "modulate:a", 0.45f, HpCostContractDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(0f, -2f), HpCostPulseDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale * 1.008f, HpCostPulseDuration);
        tween.Parallel().TweenProperty(_body, "modulate", new Color(0.92f, 0.66f, 0.70f, _baseBodyModulate.A), HpCostPulseDuration);
        tween.Parallel().TweenProperty(_bloodDrops, "position", _baseBodyPosition + new Vector2(18f, -43f), HpCostPulseDuration);
        tween.Parallel().TweenProperty(_bloodDrops, "modulate:a", 0.30f, HpCostPulseDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition, HpCostReturnDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale, HpCostReturnDuration);
        tween.Parallel().TweenProperty(_body, "modulate", _baseBodyModulate, HpCostReturnDuration);
        tween.Parallel().TweenProperty(_bloodDrops, "position", _baseBodyPosition + new Vector2(18f, -31f), HpCostReturnDuration);
        tween.Parallel().TweenProperty(_bloodDrops, "modulate:a", 0f, HpCostReturnDuration);

        tween.TweenCallback(Callable.From(FinishEffectAndResumeIdle));
    }

    public void PlayHeal()
    {
        if (_dead) return;

        ResetVisuals();
        MainFile.Logger.Info("[AnimationDriver] PlayHeal start");
        PrepareHealBurst();

        var calmTint = new Color(1f, 0.90f, 0.94f, _baseBodyModulate.A);
        var tween = _body.CreateTween();
        _activeTween = tween;
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(0f, -5f), HealLiftDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale * 1.010f, HealLiftDuration);
        tween.Parallel().TweenProperty(_body, "modulate", calmTint, HealLiftDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * 0.18f, HealLiftDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", 0.28f, HealLiftDuration);

        tween.TweenInterval(HealHoldDuration);

        tween.TweenProperty(_body, "position", _baseBodyPosition, HealReturnDuration);
        tween.Parallel().TweenProperty(_body, "scale", _baseBodyScale, HealReturnDuration);
        tween.Parallel().TweenProperty(_body, "modulate", _baseBodyModulate, HealReturnDuration);
        tween.Parallel().TweenProperty(_impactBurst, "scale", Vector2.One * 0.22f, HealReturnDuration);
        tween.Parallel().TweenProperty(_impactBurst, "modulate:a", 0f, HealReturnDuration);

        tween.TweenCallback(Callable.From(FinishEffectAndResumeIdle));
    }
    public void PlayDeath()
    {
        if (_dead) return;

        ResetVisuals();
        _dead = true;

        MainFile.Logger.Info("[AnimationDriver] PlayDeath start");
        PrepareBloodDrops();

        var tween = _body.CreateTween();
        _activeTween = tween;
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);

        tween.TweenProperty(_body, "position", _baseBodyPosition + new Vector2(0f, 28f), DeathDuration);
        tween.Parallel().TweenProperty(
            _body,
            "rotation",
            _baseBodyRotation + Mathf.DegToRad(3f),
            DeathDuration);
        tween.Parallel().TweenProperty(_body, "modulate:a", 0f, DeathDuration);
        tween.Parallel().TweenProperty(
            _bloodDrops,
            "position",
            _baseBodyPosition + new Vector2(18f, -25f),
            DeathDuration);
        tween.Parallel().TweenProperty(_bloodDrops, "scale", Vector2.One * 0.26f, DeathBloodRiseDuration);
        tween.Parallel().TweenProperty(_bloodDrops, "modulate:a", 0.50f, DeathBloodRiseDuration);
        tween.Parallel()
            .TweenProperty(_bloodDrops, "modulate:a", 0f, DeathBloodFadeDuration)
            .SetDelay(DeathBloodFadeDelay);

        tween.TweenCallback(Callable.From(FinishDeath));
    }

    public void CleanupForCombatEnd()
    {
        if (_cleanedUp) return;

        _cleanedUp = true;
        _dead = true;
        _activeTween?.Kill();
        _activeTween = null;

        _body.Visible = false;
        _body.Position = _baseBodyPosition;
        _body.Scale = _baseBodyScale;
        _body.Rotation = _baseBodyRotation;
        _body.Modulate = new Color(
            _baseBodyModulate.R,
            _baseBodyModulate.G,
            _baseBodyModulate.B,
            0f);
        _body.ZIndex = _baseBodyZIndex;
        _body.TopLevel = false;

        ResetVfx(_slashArc);
        ResetVfx(_impactBurst);
        ResetVfx(_bloodDrops);

        MainFile.Logger.Info("[AnimationDriver] CleanupForCombatEnd");
    }

    public void ResetVisuals()
    {
        _activeTween?.Kill();
        _activeTween = null;

        _body.Visible = _baseBodyVisible;
        _body.Position = _baseBodyPosition;
        _body.Scale = _baseBodyScale;
        _body.Rotation = _baseBodyRotation;
        _body.Modulate = _baseBodyModulate;

        _body.ZIndex = _baseBodyZIndex;
        _body.TopLevel = false;
        ResetVfx(_slashArc);
        ResetVfx(_impactBurst);
        ResetVfx(_bloodDrops);
    }

    public void Dispose()
    {
        CleanupForCombatEnd();
    }

    public string DescribeState()
    {
        return $"dead={_dead}, bodyBasePos={_baseBodyPosition}, bodyBaseScale={_baseBodyScale}, " +
               $"bodyBaseRotation={_baseBodyRotation}, bodyBaseModulate={_baseBodyModulate}";
    }

    private static void ResetVfx(Sprite2D sprite)
    {
        sprite.Visible = false;
        sprite.Modulate = new Color(sprite.Modulate.R, sprite.Modulate.G, sprite.Modulate.B, 0f);
        sprite.Position = Vector2.Zero;
        sprite.Scale = Vector2.One;
        sprite.Rotation = 0f;
        sprite.ZIndex = 0;
        sprite.TopLevel = false;
    }

    private void PrepareSlashArc(EntelechiaAnimationVariant variant)
    {
        var scale = variant == EntelechiaAnimationVariant.Area ? 0.38f : 0.30f;

        _slashArc.Visible = true;
        _slashArc.Position = _baseBodyPosition + new Vector2(108f, -106f);
        _slashArc.Scale = Vector2.One * scale;
        _slashArc.Rotation = Mathf.DegToRad(-12f);
        _slashArc.Modulate = new Color(1f, 1f, 1f, 0f);
    }

    private void PrepareImpactBurst(EntelechiaAnimationVariant variant, float peakScale)
    {
        var color = variant switch
        {
            EntelechiaAnimationVariant.Blood => new Color(1f, 0.42f, 0.55f, 0f),
            EntelechiaAnimationVariant.Power => new Color(1f, 0.70f, 0.90f, 0f),
            _ => new Color(1f, 0.76f, 0.84f, 0f)
        };

        _impactBurst.Visible = true;
        _impactBurst.Position = _baseBodyPosition + new Vector2(15f, -85f);
        _impactBurst.Scale = Vector2.One * peakScale * 0.72f;
        _impactBurst.Rotation = 0f;
        _impactBurst.Modulate = color;
    }

    private void PrepareBlockBurst()
    {
        _impactBurst.Visible = true;
        _impactBurst.Position = _baseBodyPosition + new Vector2(0f, -65f);
        _impactBurst.Scale = Vector2.One * 0.12f;
        _impactBurst.Rotation = 0f;
        _impactBurst.Modulate = new Color(0.88f, 0.94f, 1f, 0f);
    }

    private void PrepareHurtBurst()
    {
        _impactBurst.Visible = true;
        _impactBurst.Position = _baseBodyPosition + new Vector2(-5f, -90f);
        _impactBurst.Scale = Vector2.One * 0.14f;
        _impactBurst.Rotation = 0f;
        _impactBurst.Modulate = new Color(1f, 0.30f, 0.38f, 0f);
    }

    private void PrepareBloodDrops()
    {
        _bloodDrops.Visible = true;
        _bloodDrops.Position = _baseBodyPosition + new Vector2(18f, -55f);
        _bloodDrops.Scale = Vector2.One * 0.14f;
        _bloodDrops.Rotation = 0f;
        _bloodDrops.Modulate = new Color(0.72f, 0.08f, 0.16f, 0f);
    }

    private void PrepareHealBurst()
    {
        _impactBurst.Visible = true;
        _impactBurst.Position = _baseBodyPosition + new Vector2(0f, -80f);
        _impactBurst.Scale = Vector2.One * 0.12f;
        _impactBurst.Rotation = 0f;
        _impactBurst.Modulate = new Color(1f, 0.82f, 0.90f, 0f);
    }

    private void FinishDeath()
    {
        _activeTween = null;
        ResetVfx(_slashArc);
        ResetVfx(_impactBurst);
        ResetVfx(_bloodDrops);
        _body.Visible = false;
    }

    private void FinishEffectAndResumeIdle()
    {
        _activeTween = null;
        ResetVisuals();
        PlayIdle();
    }
}
