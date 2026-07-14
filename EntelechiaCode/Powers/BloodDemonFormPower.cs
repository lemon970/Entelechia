using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Entelechia.EntelechiaCode.Powers;

public class BloodDemonFormPower : EntelechiaPower
{
    public const int BloodSpeedPerStack = 1;
    public const decimal HeartCandleMultiplierBonusPerStack = 0.5m;
    public const int HeartCandleMultiplierBonusPercentPerStack = 50;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    [SavedProperty]
    public decimal HeartCandlePercent { get; set; }

    private bool _triggeredThisTurn;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        _triggeredThisTurn = false;

        var target = Owner.CombatState?.Enemies?
            .FirstOrDefault(enemy => enemy.CurrentHp > 0 && enemy.Powers?.Any(power => power is HeartCandlePower) != true);
        if (target != null)
            await HeartCandlePower.ApplyPercent(choiceContext, target, null, HeartCandlePercent, false);
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta >= 0 || _triggeredThisTurn || !TurnStateTracker.IsOwnerPlayerTurn(Owner)) return;
        _triggeredThisTurn = true;
        await CommonActions.Apply<BloodSpeedPower>(Owner, (CardModel)null!, Amount * BloodSpeedPerStack, false);
    }
}
