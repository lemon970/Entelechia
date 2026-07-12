using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;
using BaseLib.Extensions;

namespace Entelechia.EntelechiaCode.Cards;

public class CrimsonMerge : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public CrimsonMerge() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8);
        WithPower<HeartCandlePower>(4);
        WithCards(1);
        WithEnergy(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.Power<HeartCandlePower>().UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var target = cardPlay.Target;
        var hcAmount = DynamicVars.Power<HeartCandlePower>().BaseValue;
        var lowHealth = IsLowHealth();
        var totalHarvestTriggers = 0;

        for (var i = 0; i < 2; i++)
        {
            if (target.CurrentHp <= 0) break;
            var triggersBefore = BloodHarvestPower.GetTriggerCountForCurrentCardPlay(this);
            await ExecuteCardAttack(context, cardPlay);

            var harvestTriggers = Math.Max(BloodHarvestPower.GetTriggerCountForCurrentCardPlay(this) - triggersBefore, 0);
            totalHarvestTriggers += harvestTriggers;
        }

        if (totalHarvestTriggers > 0 && target.CurrentHp > 0)
            await HeartCandlePower.ApplyPercent(context, target, this, totalHarvestTriggers * hcAmount, true);

        if (lowHealth && totalHarvestTriggers >= 1)
            await PlayerCmd.GainEnergy(1, Owner);
        else if (!lowHealth && totalHarvestTriggers >= 2)
            await DrawCards(context, DynamicVars.Cards.BaseValue);
    }

}
