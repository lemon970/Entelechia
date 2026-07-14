using BaseLib.Abstracts;
using BaseLib.Utils;
using Entelechia.EntelechiaCode;
using Entelechia.EntelechiaCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading;

namespace Entelechia.EntelechiaCode.Powers;

// Debuff on enemy. Amount is the remaining Heart Candle HP pool.
public class HeartCandlePower : EntelechiaPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private const decimal MonsterMultiplier = 2.0m;
    private const decimal EliteMultiplier = 2.5m;
    private const decimal BossMultiplier = 3.0m;
    private const decimal RedCandleAllBonus = 0.75m;

    private static readonly AsyncLocal<bool> ResolvingHeartCandle = new();

    public static async Task ApplyPercent(PlayerChoiceContext context, Creature target, CardModel? source, decimal percent, bool showEffect)
    {
        var pool = Math.Ceiling(Math.Max(target.CurrentHp, 0m) * percent / 100m);
        if (pool <= 0) return;

        var before = target.Powers?.OfType<HeartCandlePower>().FirstOrDefault()?.Amount ?? 0m;
        await CommonActions.Apply<HeartCandlePower>(context, target, source, pool, showEffect);
        var after = target.Powers?.OfType<HeartCandlePower>().FirstOrDefault()?.Amount ?? 0m;
        HeartCandleLedger.RecordNaturalGenerated(target, Math.Max(after - before, 0m));
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (ResolvingHeartCandle.Value) return;
        if (target != Owner || Owner.CurrentHp <= 0) return;
        if (dealer == null || !TurnStateTracker.IsPlayerCreature(dealer)) return;
        if (cardSource == null || !EntelechiaDamage.IsAttackDamage(props, cardSource)) return;
        if (Amount <= 0 || result.UnblockedDamage <= 0) return;

        var multiplier = GetMultiplier(dealer, cardSource, Owner);
        var theoreticalDamage = Math.Ceiling(result.UnblockedDamage * multiplier);
        var plannedDamage = Math.Min(Math.Min(theoreticalDamage, Amount), Owner.CurrentHp);
        if (plannedDamage <= 0) return;

        ResolvingHeartCandle.Value = true;
        try
        {
            var beforeHp = Owner.CurrentHp;
            await TurnStateTracker.LoseHpTracking(choiceContext, Owner, plannedDamage, DamageProps.nonCardHpLoss, dealer);
            var actualDamage = Math.Max(beforeHp - Owner.CurrentHp, 0m);
            if (actualDamage <= 0) return;

            HeartCandleLedger.RecordConsumed(Owner, actualDamage);

            var ember = dealer.Powers?.OfType<CandleEmberPower>().FirstOrDefault();
            if (ember != null && ember.Amount > 0 && Owner.CurrentHp > 0)
            {
                await CommonActions.Apply<BloodlossPower>(choiceContext, Owner, null, ember.Amount, false);
                await ember.TryHealLowHealth();
            }

            if (Amount <= actualDamage)
                await PowerCmd.Remove(this);
            else
                await PowerCmd.ModifyAmount(choiceContext, this, -actualDamage, dealer, cardSource, false);
        }
        finally
        {
            ResolvingHeartCandle.Value = false;
        }
    }

    private static decimal GetMultiplier(Creature dealer, CardModel cardSource, Creature target)
    {
        var baseMultiplier = target.CombatState?.Encounter?.RoomType switch
        {
            RoomType.Elite => EliteMultiplier,
            RoomType.Boss => BossMultiplier,
            _ => MonsterMultiplier,
        };

        var formStacks = dealer.Powers?.OfType<BloodDemonFormPower>().FirstOrDefault()?.Amount ?? 0m;
        var formBonus = formStacks * BloodDemonFormPower.HeartCandleMultiplierBonusPerStack;
        var redCandleBonus = cardSource is RedCandleAll ? RedCandleAllBonus : 0m;
        return baseMultiplier + formBonus + redCandleBonus;
    }
}
