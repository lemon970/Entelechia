using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;
using System.Linq;

namespace Entelechia.EntelechiaCode.Cards;

public class DiscontinuousPulse : EntelechiaCard
{
    public DiscontinuousPulse() : base(0, CardType.Skill, CardRarity.Basic, TargetType.None)
    {
        WithPower<BloodlossPower>(2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<BloodlossPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (await TryExhaustAnotherCard(context))
            await DrawCards(context, 2m);

        var enemies = Owner.Creature.CombatState?.Enemies;
        if (enemies == null) return;

        var target = enemies.FirstOrDefault(e => e.CurrentHp > 0 && e.Powers?.Any(p => p is BloodHarvestPower && p.Amount > 0) == true);
        if (target == null) return;

        var bh = target.Powers!.First(p => p is BloodHarvestPower && p.Amount > 0);
        if (bh.Amount <= 1)
            await PowerCmd.Remove(bh);
        else
            await PowerCmd.ModifyAmount(context, bh, -1, Owner.Creature, this, false);
        await CommonActions.Apply<BloodlossPower>(context, target, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);
    }
}
