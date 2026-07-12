using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;
using BaseLib.Extensions;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodDrain : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public BloodDrain() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8);
        WithPower<BloodHarvestPower>(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.Power<BloodHarvestPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { CurrentHp: > 0 } target) return;

        var lowHealth = IsLowHealth();
        var harvestAmount = DynamicVars.Power<BloodHarvestPower>().BaseValue;
        if (lowHealth)
            await CommonActions.Apply<BloodHarvestPower>(context, target, this, harvestAmount, true);

        await ExecuteCardAttack(context, cardPlay);
        if (target.CurrentHp <= 0) return;

        if (lowHealth) return;
        await CommonActions.Apply<BloodHarvestPower>(context, target, this, harvestAmount + 1m, true);
        await DrawCards(context, 1);
    }
}
