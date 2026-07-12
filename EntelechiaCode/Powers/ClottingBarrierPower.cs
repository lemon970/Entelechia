using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Entelechia.EntelechiaCode.Powers;

// ponytail: block-on-BloodHarvest logic via Harmony patch in CombatPatches
public class ClottingBarrierPower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (!TurnStateTracker.IsPlayerCreature(Owner)) return;
        if (side == CombatSide.Player) return;
        await PowerCmd.Remove(this);
    }
}
