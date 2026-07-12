using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class HeartBrand : EntelechiaCard
{
    private decimal ExistingCandlePercent => DynamicVars.Power<HeartCandlePower>().BaseValue / 2m;

    public HeartBrand() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<HeartCandlePower>(12);
        WithCards(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<HeartCandlePower>().UpgradeValueBy(6);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            var hadCandle = cardPlay.Target.Powers?.Any(power => power is HeartCandlePower) == true;
            var percent = hadCandle
                ? ExistingCandlePercent
                : DynamicVars.Power<HeartCandlePower>().BaseValue;
            await HeartCandlePower.ApplyPercent(context, cardPlay.Target, this, percent, true);

            if (!hadCandle)
            {
                var refund = Math.Min(cardPlay.Resources.EnergySpent, 1);
                if (refund > 0)
                    await PlayerCmd.GainEnergy(refund, Owner);
            }
        }
        await DrawCards(context, DynamicVars.Cards.BaseValue);
    }
}
