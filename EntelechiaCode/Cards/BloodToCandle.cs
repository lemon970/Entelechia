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

        var removedGroups = 0;
        while (removedGroups < 4)
        {
            var bloodloss = cardPlay.Target.Powers?.FirstOrDefault(p => p is BloodlossPower);
            if (bloodloss == null || bloodloss.Amount < 2) break;

            if (bloodloss.Amount <= 2)
                await PowerCmd.Remove(bloodloss);
            else
                await PowerCmd.ModifyAmount(context, bloodloss, -2, Owner.Creature, this, false);

            await HeartCandlePower.ApplyPercent(
                context,
                cardPlay.Target,
                this,
                CandlePercentPerGroup,
                true);
            removedGroups++;
        }

        if (removedGroups > 0 && IsLowHealth())
            await CreatureCmd.GainBlock(Owner.Creature, removedGroups * 2, default, cardPlay, false);
    }
}
