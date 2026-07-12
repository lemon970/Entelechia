using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodDissect : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public BloodDissect() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(9);
        WithPower<BloodHarvestPower>("ExtraDamage", 3);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.Var<PowerVar<BloodHarvestPower>>("ExtraDamage").UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var target = cardPlay.Target;
        var lowHealth = IsLowHealth();

        await ExecuteCardAttack(context, cardPlay);
        if (target.CurrentHp <= 0) return;

        var harvest = target.Powers?.FirstOrDefault(p => p is BloodHarvestPower);
        if (harvest == null || harvest.Amount <= 0) return;

        var toRemove = lowHealth
            ? (int)harvest.Amount
            : Math.Min(2, (int)harvest.Amount);
        if (toRemove >= harvest.Amount)
            await PowerCmd.Remove(harvest);
        else
            await PowerCmd.ModifyAmount(context, harvest, -toRemove, Owner.Creature, this, false);

        await BloodHarvestPower.RunWithoutTriggerForCard(this, async () =>
        {
            for (var i = 0; i < toRemove; i++)
            {
                if (target.CurrentHp <= 0) break;
                var extraDamage = DynamicVars.Var<PowerVar<BloodHarvestPower>>("ExtraDamage").BaseValue;
                await ExecuteAttack(context, new AttackCommand(extraDamage).FromCard(this, cardPlay).Targeting(target));
            }
        });

        if (lowHealth && toRemove >= 2)
            await PlayerCmd.GainEnergy(1, Owner);
        else if (!lowHealth && toRemove == 2)
            await DrawCards(context, 1);
    }
}
