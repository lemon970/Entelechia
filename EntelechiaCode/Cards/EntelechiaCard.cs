using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Entelechia.EntelechiaCode.Animation;
using Entelechia.EntelechiaCode.Character;
using Entelechia.EntelechiaCode.Extensions;
using Entelechia.EntelechiaCode.Powers;
using Entelechia.EntelechiaCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Cards;

[Pool(typeof(EntelechiaCardPool))]
public abstract class EntelechiaCard : ConstructedCardModel
{
    private static readonly HashSet<Type> HealthStateCardTypes =
    [
        typeof(BloodVeil),
        typeof(BloodMend),
        typeof(CrimsonLash),
        typeof(BloodFrenzy),
        typeof(BloodDrain),
        typeof(CrimsonShield),
        typeof(BloodStorm),
        typeof(Lacerate),
        typeof(BloodFragrance),
        typeof(BloodOverload),
        typeof(EntelechiaBloodletting),
        typeof(BloodBorrow),
        typeof(BloodDebtSettlement),
        typeof(CrimsonEmbers),
        typeof(BloodToCandle),
        typeof(CandleEmber),
        typeof(SoulBloodDraw),
        typeof(BloodSweep),
        typeof(CounterSlash),
        typeof(Suture),
        typeof(BloodlinePuncture),
        typeof(BloodRebuild),
        typeof(PainConversion),
        typeof(BloodDissect),
        typeof(CrimsonMerge),
        typeof(ClottingBarrier),
        typeof(ClottingBackflow),
        typeof(EternalReplete),
        typeof(ImmortalBloodline),
        typeof(SanguineRite)
    ];

    protected EntelechiaCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
        if (HealthStateCardTypes.Contains(GetType()))
            WithKeywords(EntelechiaKeywords.HighHealth, EntelechiaKeywords.LowHealth);
    }

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    // Each attack card overrides this with its WithDamage base value.
    // BaseLib.Var<T>() and DynamicVars.OfType<T>() both fail headless due to
    // runtime BaseLib version mismatch; a virtual property is the reliable fallback.
    protected virtual decimal BaseDamage => 0m;
    public decimal AttackBaseDamage => BaseDamage;
    protected virtual decimal HpCost => 0m;

    protected bool IsLowHealth()
    {
        var creature = Owner?.Creature;
        return creature != null && creature.CurrentHp <= creature.MaxHp * 0.5m;
    }

    protected bool IsHighHealth()
    {
        var creature = Owner?.Creature;
        return creature != null && creature.CurrentHp > creature.MaxHp * 0.5m;
    }

    private bool _blockAnimationRequested;

    public bool CanPayRequiredHpCost() => CanPayHpCost(HpCost);
    internal void ResetBlockAnimationRequest() => _blockAnimationRequested = false;
    internal void RequestBlockAnimationAfterSuccessfulGain() => _blockAnimationRequested = true;

    // ponytail: STS2 v0.108 changed AttackCommand.FromCard to 2-arg.
    // BaseLib 3.3.5 still calls 1-arg via CommonActions.CardAttack.
    protected AttackCommand CardAttack(CardPlay play, int hitCount = 1)
    {
        var cmd = new AttackCommand(BaseDamage).FromCardCompatibility(this, play);
        if (TargetType == MegaCrit.Sts2.Core.Entities.Cards.TargetType.AllEnemies)
            cmd = cmd.TargetingAllOpponents(Owner.Creature.CombatState!);
        else if (play.Target != null)
            cmd = cmd.Targeting(play.Target);

        return cmd.WithHitCount(hitCount);
    }

    protected async Task<AttackCommand> ExecuteCardAttack(PlayerChoiceContext context, CardPlay play, int hitCount = 1)
    {
        var variant = hitCount > 1
            ? EntelechiaAnimationVariant.MultiHit
            : EntelechiaAnimationVariant.Normal;
        return await ExecuteAttack(context, CardAttack(play, hitCount), variant, play);
    }

    protected async Task<AttackCommand> ExecuteAttack(
        PlayerChoiceContext context,
        AttackCommand attack,
        EntelechiaAnimationVariant variant = EntelechiaAnimationVariant.Normal,
        CardPlay? cardPlay = null)
    {
        attack = await attack.Execute(context);
        TryPlayAttackAnimation(attack.IsMultiTargeted
            ? EntelechiaAnimationVariant.Area
            : variant);

        foreach (var feast in Owner.Creature.Powers.OfType<BloodFeastPower>().ToList())
        {
            await feast.ReconcileCompletedAttack(context, attack);
            if (cardPlay?.Target != null)
                await feast.ReconcileKilledTarget(context, attack, cardPlay.Target);
        }
        return attack;
    }

    private void TryPlayAttackAnimation(EntelechiaAnimationVariant variant)
    {
        try
        {
            var visuals = Owner?.Creature?.GetCreatureNode()?.Visuals;
            if (visuals == null) return;

            if (EntelechiaCombatAnimationDriver.TryGet(visuals, out var driver))
                driver?.PlayAttack(variant);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Info(
                $"[AnimationDriver] PlayAttack skipped: {exception.GetType().Name}: {exception.Message}");
        }
    }

    internal void TryPlayCastAnimationAfterSuccessfulPlay()
    {
        var playBlock = _blockAnimationRequested;
        _blockAnimationRequested = false;

        if (HpCost > 0m) return;
        if (!playBlock && Type != CardType.Skill && Type != CardType.Power) return;
        if (!playBlock && (this is CrimsonEmbers or BloodDebtSettlement)) return;

        var variant = Type == CardType.Power
            ? EntelechiaAnimationVariant.Power
            : EntelechiaAnimationVariant.Normal;

        try
        {
            var visuals = Owner?.Creature?.GetCreatureNode()?.Visuals;
            if (visuals == null) return;

            if (EntelechiaCombatAnimationDriver.TryGet(visuals, out var driver))
            {
                if (playBlock)
                    driver?.PlayBlock();
                else
                    driver?.PlayCast(variant);
            }
        }
        catch (Exception exception)
        {
            MainFile.Logger.Info(
                $"[AnimationDriver] Post-play animation skipped: {exception.GetType().Name}: {exception.Message}");
        }
    }

    protected Task<IEnumerable<CardModel>> DrawCards(PlayerChoiceContext context, decimal count)
        => CardPileCmd.Draw(context, count, Owner, false);

    protected async Task<bool> TryExhaustAnotherCard(PlayerChoiceContext context)
    {
        var hasCandidate = Owner.PlayerCombatState?.Hand.Cards.Any(
            card => !ReferenceEquals(card, this)) == true;
        if (!hasCandidate) return false;

        var selected = (await CardSelectCmd.FromHand(
            context,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, 1),
            card => !ReferenceEquals(card, this),
            this)).FirstOrDefault();
        if (selected is null) return false;

        await CardCmd.Exhaust(context, selected);
        return true;
    }

    public new bool CanPlay()
    {
        return base.CanPlay() && CanPayRequiredHpCost();
    }

    public new bool CanPlay(out UnplayableReason reason, out AbstractModel? source)
    {
        if (!base.CanPlay(out reason, out source)) return false;
        if (CanPayRequiredHpCost()) return true;
        reason = UnplayableReason.BlockedByCardLogic;
        source = this;
        return false;
    }

    protected bool CanPayHpCost(decimal amount)
    {
        if (amount <= 0m) return true;

        var creature = Owner?.Creature;
        if (creature == null) return true;
        if (creature.CurrentHp - amount >= 1m) return true;
        if (creature.Powers?.Any(power => power is ImmortalBloodlinePower) == true) return true;
        if (creature.Player?.Relics?.OfType<BloodDemonReplete>().Any(relic => relic.CanPreventDeath(creature)) == true)
            return true;

        return false;
    }

    protected async Task<bool> TryPayHpCost(PlayerChoiceContext context, decimal amount, CardPlay cardPlay)
    {
        if (!CanPayHpCost(amount)) return false;
        await TurnStateTracker.LoseHpTracking(context, Owner.Creature, amount, DamageProps.cardHpLoss, Owner.Creature, this, cardPlay);
        TryPlayHpCostAnimation();
        return true;
    }

    private void TryPlayHpCostAnimation()
    {
        try
        {
            var visuals = Owner?.Creature?.GetCreatureNode()?.Visuals;
            if (visuals == null) return;

            if (EntelechiaCombatAnimationDriver.TryGet(visuals, out var driver))
                driver?.PlayHpCost();
        }
        catch (Exception exception)
        {
            MainFile.Logger.Info(
                $"[AnimationDriver] PlayHpCost skipped: {exception.GetType().Name}: {exception.Message}");
        }
    }
}
