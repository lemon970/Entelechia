using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class SanguineRite : EntelechiaCard
{
    public SanguineRite() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
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
        var targets = this.GetTargets().Where(target => target.CurrentHp > 0).ToList();
        foreach (var target in targets)
        {
            await CommonActions.Apply<BloodHarvestPower>(context, target, this, DynamicVars.Power<BloodHarvestPower>().BaseValue, true);
            await CommonActions.Apply<BloodlossPower>(context, target, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);
        }

        if (IsHighHealth())
            await DrawCards(context, 1);
        else
            await CreatureCmd.GainBlock(Owner.Creature, Math.Min(targets.Count * 2, 6), default, cardPlay, false);
    }
}
