using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodInfect : EntelechiaCard
{
    public BloodInfect() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<BloodlossPower>(4);
        WithCards(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<BloodlossPower>().UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
            await CommonActions.Apply<BloodlossPower>(context, cardPlay.Target, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);
        await DrawCards(context, DynamicVars.Cards.BaseValue);
    }
}
