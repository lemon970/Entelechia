using BaseLib.Utils;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class ClottingBackflow : EntelechiaCard
{
    public ClottingBackflow() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCards(2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        var harvest = target.Powers?.FirstOrDefault(power => power is BloodHarvestPower);
        var lowHealth = IsLowHealth();
        if (harvest == null || harvest.Amount <= 0) return;

        await CardCmd.Exhaust(context, this, false, false);

        var toRemove = Math.Min((int)DynamicVars.Cards.BaseValue, (int)harvest.Amount);
        if (toRemove >= harvest.Amount)
            await PowerCmd.Remove(harvest);
        else
            await PowerCmd.ModifyAmount(context, harvest, -toRemove, Owner.Creature, this, false);

        await DrawCards(context, toRemove);

        await PlayerCmd.GainEnergy(1, Owner);

        if (lowHealth)
            await TurnStateTracker.HealTracking(Owner.Creature, Math.Min(toRemove * 2m, 6m), true);
    }
}
