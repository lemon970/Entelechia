using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Powers;

#pragma warning disable STS001 // Localization is maintained separately from runtime code.
public class ResidualPulseConduitPower : EntelechiaPower
{
    private const int TriggersPerStack = 2;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int TriggerCountThisTurn { get; private set; }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
            TriggerCountThisTurn = 0;

        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner
            || props != DamageProps.cardHpLoss
            || result.UnblockedDamage <= 0m
            || cardSource?.Owner?.Creature != Owner
            || Amount <= 0m
            || TriggerCountThisTurn >= (int)Amount * TriggersPerStack
            || !TurnStateTracker.IsOwnerPlayerTurn(Owner))
            return;

        var player = Owner.Player;
        if (player == null) return;

        TriggerCountThisTurn++;
        if (Owner.CurrentHp > Owner.MaxHp * 0.5m)
        {
            await CardPileCmd.Draw(choiceContext, 2m, player, false);
            return;
        }

        await PlayerCmd.GainEnergy(1m, player);
        await CreatureCmd.GainBlock(Owner, 4m, default, null, false);
    }
}
#pragma warning restore STS001
