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
        WithPower<BloodlossPower>(5);
        WithPower<BloodHarvestPower>(2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<BloodlossPower>().UpgradeValueBy(2);
        DynamicVars.Power<BloodHarvestPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target is { CurrentHp: > 0 } target)
        {
            await CommonActions.Apply<BloodlossPower>(context, target, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);
            await CommonActions.Apply<BloodHarvestPower>(context, target, this, DynamicVars.Power<BloodHarvestPower>().BaseValue, true);
        }
    }
}
