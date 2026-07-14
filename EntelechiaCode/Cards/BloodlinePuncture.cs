using BaseLib.Extensions;
using BaseLib.Utils;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodlinePuncture : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;

    public BloodlinePuncture() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6);
        WithCards(1);
        WithPower<BloodHarvestPower>(2);
        WithPower<BloodlossPower>(2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var highHealth = IsHighHealth();

        await ExecuteCardAttack(context, cardPlay);
        if (highHealth)
            await DrawCards(context, DynamicVars.Cards.BaseValue);

        if (cardPlay.Target != null && cardPlay.Target.CurrentHp > 0)
        {
            var target = cardPlay.Target;
            if (highHealth)
                await CommonActions.Apply<BloodHarvestPower>(context, target, this, DynamicVars.Power<BloodHarvestPower>().BaseValue, true);
            else
                await CommonActions.Apply<BloodlossPower>(context, target, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);
        }
    }
}
