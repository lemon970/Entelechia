using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Entelechia.EntelechiaCode.Powers;

public class BloodDemonFormPower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _triggeredThisTurn;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        _triggeredThisTurn = false;
        await CommonActions.Apply<StrengthPower>(choiceContext, player.Creature, null, Amount, false);

        var target = Owner.CombatState?.Enemies?
            .FirstOrDefault(enemy => enemy.CurrentHp > 0 && enemy.Powers?.Any(power => power is HeartCandlePower) != true);
        if (target != null)
            await HeartCandlePower.ApplyPercent(choiceContext, target, null, Amount * 4m, false);
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta >= 0 || _triggeredThisTurn || !TurnStateTracker.IsOwnerPlayerTurn(Owner)) return;
        _triggeredThisTurn = true;
        await CommonActions.Apply<BloodSpeedPower>(Owner, (CardModel)null!, Amount, false);
    }
}
