using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Powers;

// Caps HP at 1 this turn. Amount is the next-turn HP loss; upgraded cards apply Amount 0.
public class ImmortalBloodlinePower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return amount;
        var newHp = target.CurrentHp - amount;
        if (newHp < 1) return target.CurrentHp - 1;
        return amount;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (player.Creature != Owner) return;
        if (Amount > 0)
            await TurnStateTracker.LoseHpTracking(ctx, Owner, Amount, DamageProps.nonCardHpLoss);
        await PowerCmd.Remove(this);
    }
}
