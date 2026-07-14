using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Entelechia.EntelechiaCode.Powers;

public class EmberBloodlinePower : EntelechiaPower
{
    private bool _baseRewardGranted;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public decimal HarvestAllEnemiesAmount { get; set; }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side != CombatSide.Player) return;
        if (!TurnStateTracker.IsPlayerCreature(Owner)) return;

        var context = new BlockingPlayerChoiceContext();
        var enemies = combatState.Enemies?.Where(enemy => enemy.CurrentHp > 0).ToList();
        if (!_baseRewardGranted)
        {
            _baseRewardGranted = true;
            await TurnStateTracker.HealTracking(Owner, 4, true);
            await CommonActions.Apply<BloodSpeedPower>(context, Owner, null, 1m, false);

            if (enemies is { Count: > 0 })
            {
                var stackCount = (int)Math.Max(Amount, 0m);
                for (var i = 0; i < stackCount; i++)
                {
                    var enemy = enemies[i % enemies.Count];
                    await CommonActions.Apply<BloodHarvestPower>(context, enemy, null, 2m, false);
                }
            }
        }

        if (HarvestAllEnemiesAmount > 0m)
        {
            if (enemies != null)
                foreach (var enemy in enemies)
                    await CommonActions.Apply<BloodHarvestPower>(
                        context,
                        enemy,
                        null,
                        HarvestAllEnemiesAmount,
                        false);
        }
        else
        {
            await PowerCmd.Remove(this);
        }
    }
}
