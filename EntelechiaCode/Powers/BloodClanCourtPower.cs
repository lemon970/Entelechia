using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Entelechia.EntelechiaCode.Powers;

// Turn-start Bloodloss aura.
public class BloodClanCourtPower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        var enemies = Owner.CombatState?.Enemies;
        if (enemies == null) return;
        foreach (var enemy in enemies.Where(enemy => enemy.CurrentHp > 0))
            await CommonActions.Apply<BloodlossPower>(ctx, enemy, null, Amount, false);
    }
}
