using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Powers;

public class CrimsonWardPower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? attacker, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner || amount <= 0 || !IsAttackDamage(props, cardSource)) return 1m;
        return 0.5m;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0) return;
        if (!IsAttackDamage(props, cardSource)) return;

        if (Amount <= 1)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(choiceContext, this, -1, dealer, cardSource, false);
    }

    private static bool IsAttackDamage(ValueProp props, CardModel? cardSource)
    {
        return EntelechiaDamage.IsAttackDamage(props, cardSource)
            || props == DamageProps.monsterMove;
    }
}
