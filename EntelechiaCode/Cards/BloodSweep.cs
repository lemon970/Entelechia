using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodSweep : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public BloodSweep() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(12);
        WithPower<HeartCandlePower>(8);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
        DynamicVars.Power<HeartCandlePower>().UpgradeValueBy(4);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        var targets = this.GetTargets().ToList();
        var candleTargets = 0;
        await ExecuteCardAttack(context, cardPlay);
        foreach (var target in targets.Where(target => target.CurrentHp > 0))
        {
            if (target.Powers?.FirstOrDefault(p => p is BloodlossPower) != null)
            {
                await HeartCandlePower.ApplyPercent(context, target, this, DynamicVars.Power<HeartCandlePower>().BaseValue, true);
                candleTargets++;
            }
        }

        if (candleTargets >= 2)
        {
            if (lowHealth)
                await PlayerCmd.GainEnergy(1, Owner);
            else
                await DrawCards(context, 1);
        }
    }
}
