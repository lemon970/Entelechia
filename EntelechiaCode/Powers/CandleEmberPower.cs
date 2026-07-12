using Entelechia.EntelechiaCode;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Powers;

public class CandleEmberPower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public bool TriggeredThisTurn { get; private set; }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
            TriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public async Task TryHealLowHealth()
    {
        if (TriggeredThisTurn || Owner.CurrentHp > Owner.MaxHp * 0.5m) return;

        TriggeredThisTurn = true;
        await TurnStateTracker.HealTracking(Owner, Amount, true);
    }

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // HeartCandlePower applies this when Heart Candle actually causes HP loss.
        return Task.CompletedTask;
    }
}
