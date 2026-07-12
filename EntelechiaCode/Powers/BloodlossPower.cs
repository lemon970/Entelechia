using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Powers;

// Debuff on any creature. Lose Amount HP at start of its side's turn.
public class BloodlossPower : EntelechiaPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (!creatures.Contains(Owner)) return;
        if (Amount <= 0) return;

        var beforeHp = Owner.CurrentHp;

        await TurnStateTracker.LoseHpTracking(null!, Owner, Amount, DamageProps.nonCardHpLoss);

        var actualLoss = Math.Max(beforeHp - Owner.CurrentHp, 0m);
        if (actualLoss > 0 && Owner.CurrentHp > 0 && !TurnStateTracker.IsPlayerCreature(Owner) && PlayerHasBloodClanCourt(combatState))
            await CommonActions.Apply<BloodHarvestPower>(Owner, (CardModel)null!, 1m, false);

        await ReduceTurnStack();
    }

    private static bool PlayerHasBloodClanCourt(ICombatState combatState)
    {
        return combatState.PlayerCreatures?.Any(creature =>
            creature.Powers?.Any(power => power is BloodClanCourtPower) == true) == true;
    }

    private async Task ReduceTurnStack()
    {
        if (Amount <= 1)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(null!, this, -1, Owner, null, false);
    }
}
