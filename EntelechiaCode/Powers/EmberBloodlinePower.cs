using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Entelechia.EntelechiaCode.Powers;

public class EmberBloodlinePower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side != CombatSide.Player) return;
        if (!TurnStateTracker.IsPlayerCreature(Owner)) return;

        await TurnStateTracker.HealTracking(Owner, 4, true);
        await CommonActions.Apply<BloodSpeedPower>(Owner, (CardModel)null!, 1m, false);

        var enemies = combatState.Enemies?.Where(enemy => enemy.CurrentHp > 0).ToList();
        if (enemies == null || enemies.Count == 0) return;

        var stackCount = (int)Math.Max(Amount, 0m);
        for (var i = 0; i < stackCount; i++)
        {
            var enemy = enemies[i % enemies.Count];
            await CommonActions.Apply<BloodHarvestPower>(enemy, (CardModel)null!, 2m, false);
        }

        await PowerCmd.Remove(this);
    }
}
