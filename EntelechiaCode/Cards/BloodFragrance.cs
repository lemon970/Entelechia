using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodFragrance : EntelechiaCard
{
    public BloodFragrance() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithPower<BloodHarvestPower>(2);
        WithPower<BloodlossPower>(2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<BloodHarvestPower>().UpgradeValueBy(1);
        DynamicVars.Power<BloodlossPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { CurrentHp: > 0 } target) return;

        var lowHealth = IsLowHealth();
        var harvestAmount = DynamicVars.Power<BloodHarvestPower>().BaseValue + (lowHealth ? 1 : 0);
        await CommonActions.Apply<BloodHarvestPower>(context, target, this, harvestAmount, true);
        await CommonActions.Apply<BloodlossPower>(context, target, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);

        if (!lowHealth)
            await DrawCards(context, 1);
    }
}
