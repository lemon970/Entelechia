using BaseLib.Utils;
using BaseLib.Extensions;
using Entelechia.EntelechiaCode;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class ReviveCandle : EntelechiaCard
{
    private decimal ReviveRatio => DynamicVars.Power<HeartCandlePower>().BaseValue / 100m;

    public ReviveCandle() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithPower<HeartCandlePower>(50);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<HeartCandlePower>().UpgradeValueBy(50);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            var stacks = HeartCandleLedger.PreviewRevivePool(cardPlay.Target, ReviveRatio);
            if (stacks > 0)
            {
                var before = cardPlay.Target.Powers?.FirstOrDefault(power => power is HeartCandlePower)?.Amount ?? 0m;
                await CommonActions.Apply<HeartCandlePower>(context, cardPlay.Target, this, stacks, true);
                var after = cardPlay.Target.Powers?.FirstOrDefault(power => power is HeartCandlePower)?.Amount ?? 0m;
                HeartCandleLedger.RecordRevived(cardPlay.Target, Math.Max(after - before, 0m));
            }
        }
        await CardCmd.Exhaust(context, this, false, false);
    }
}
