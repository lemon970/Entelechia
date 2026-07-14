using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodRebuild : EntelechiaCard
{
    public BloodRebuild() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithBlock(8);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        var creature = Owner.Creature;
        var target50 = Math.Max(creature.MaxHp / 2, 1m);

        await TurnStateTracker.SetCurrentHpTracking(creature, target50);

        foreach (var enemy in this.GetTargets().Where(enemy => enemy.CurrentHp > 0))
            await CommonActions.Apply<BloodHarvestPower>(context, enemy, this, 3, true);

        if (lowHealth)
        {
            await CommonActions.CardBlock(this, cardPlay);
            await PlayerCmd.GainEnergy(1, Owner);
        }
        else
        {
            await DrawCards(context, 2);
        }
    }
}
