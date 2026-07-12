using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class HeartCandleRitual : EntelechiaCard
{
    public HeartCandleRitual() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithPower<HeartCandlePower>(18);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<HeartCandlePower>().UpgradeValueBy(9);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
            await HeartCandlePower.ApplyPercent(context, cardPlay.Target, this, DynamicVars.Power<HeartCandlePower>().BaseValue, true);
        await CardCmd.Exhaust(context, this, false, false);
    }
}
