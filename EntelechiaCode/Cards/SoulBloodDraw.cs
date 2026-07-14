using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class SoulBloodDraw : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public SoulBloodDraw() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(16);
        WithHeal(6);
        WithCards(1);
        WithEnergy(1);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Heal.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        var triggerCountBeforeAttack = BloodHarvestPower.GetTriggerCountForCurrentCardPlay(this);
        await ExecuteCardAttack(context, cardPlay);
        if (BloodHarvestPower.GetTriggerCountForCurrentCardPlay(this) > triggerCountBeforeAttack)
        {
            await TurnStateTracker.HealTracking(Owner.Creature, DynamicVars.Heal.BaseValue, true);
            if (lowHealth)
                await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            else
                await DrawCards(context, DynamicVars.Cards.BaseValue);
        }
    }
}
