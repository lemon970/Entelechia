using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodDebtSettlement : EntelechiaCard
{
    private bool ExhaustsOnPlay { get; set; } = true;

    public BloodDebtSettlement() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
        ExhaustsOnPlay = false;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var lowHealth = IsLowHealth();

        var harvest = cardPlay.Target.Powers?.FirstOrDefault(p => p is BloodHarvestPower);
        if (harvest != null && harvest.Amount > 0)
        {
            var stacks = lowHealth
                ? (int)harvest.Amount
                : Math.Min(3, (int)harvest.Amount);
            if (stacks >= harvest.Amount)
                await PowerCmd.Remove(harvest);
            else
                await PowerCmd.ModifyAmount(context, harvest, -stacks, Owner.Creature, this, false);

            var rawHeal = stacks * 3m;
            var hpBeforeHeal = Owner.Creature.CurrentHp;
            await TurnStateTracker.HealTracking(Owner.Creature, rawHeal, true);
            var actualHeal = Owner.Creature.CurrentHp - hpBeforeHeal;

            if (lowHealth)
            {
                if (stacks >= 2)
                    await PlayerCmd.GainEnergy(1, Owner);

                var overflowBlock = Math.Min(rawHeal - actualHeal, 12m);
                if (overflowBlock > 0)
                    await CreatureCmd.GainBlock(Owner.Creature, overflowBlock, default, cardPlay, false);
            }
            else
            {
                await DrawCards(context, 1);
            }
        }

        var bloodloss = cardPlay.Target.Powers?.FirstOrDefault(p => p is BloodlossPower);
        if (bloodloss != null && bloodloss.Amount > 0)
        {
            var toRemove = Math.Min(6, (int)bloodloss.Amount);
            if (toRemove >= (int)bloodloss.Amount)
                await PowerCmd.Remove(bloodloss);
            else
                await PowerCmd.ModifyAmount(context, bloodloss, -toRemove, Owner.Creature, this, false);
            var strengthGain = toRemove / 2;
            if (strengthGain > 0)
                await CommonActions.Apply<BloodDebtStrengthPower>(context, Owner.Creature, this, strengthGain, true);
        }

        if (ExhaustsOnPlay)
            await CardCmd.Exhaust(context, this, false, false);
    }
}
