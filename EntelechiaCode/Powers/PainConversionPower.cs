using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Powers;

public class PainConversionPower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public int TriggerCountThisTurn { get; set; }

    public bool RefundedEnergyThisTurn { get; set; }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            TriggerCountThisTurn = 0;
            RefundedEnergyThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta >= 0 || TriggerCountThisTurn >= Amount || !TurnStateTracker.IsOwnerPlayerTurn(Owner)) return;
        TriggerCountThisTurn++;
        await CommonActions.Apply<BloodlettingStrengthPower>(Owner, (CardModel)null!, 1, false);
        if (!RefundedEnergyThisTurn && Owner.CurrentHp <= Owner.MaxHp * 0.5m && Owner.Player != null)
        {
            RefundedEnergyThisTurn = true;
            await PlayerCmd.GainEnergy(1, Owner.Player);
        }
    }
}
