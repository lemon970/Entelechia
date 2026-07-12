using MegaCrit.Sts2.Core.Commands;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodToCandle : EntelechiaCard
{
    private decimal CandlePercentPerGroup => DynamicVars.Power<HeartCandlePower>().BaseValue;

    public BloodToCandle() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithPower<HeartCandlePower>(4);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<HeartCandlePower>().UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var bloodloss = cardPlay.Target.Powers?.FirstOrDefault(p => p is BloodlossPower);
        if (bloodloss == null || bloodloss.Amount <= 0) return;

        var groups = Math.Min(4, (int)bloodloss.Amount / 2);
        var toRemove = groups * 2;
        if (toRemove <= 0) return;

        if (toRemove >= bloodloss.Amount)
            await PowerCmd.Remove(bloodloss);
        else
            await PowerCmd.ModifyAmount(context, bloodloss, -toRemove, Owner.Creature, this, false);

        var candleStacks = groups * CandlePercentPerGroup;
        await HeartCandlePower.ApplyPercent(context, cardPlay.Target, this, candleStacks, true);

        if (IsLowHealth())
            await CreatureCmd.GainBlock(Owner.Creature, Math.Min(groups * 2, 8), default, cardPlay, false);
    }
}
