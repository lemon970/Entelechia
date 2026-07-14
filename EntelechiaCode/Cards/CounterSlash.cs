using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class CounterSlash : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;

    public CounterSlash() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8);
        WithCards(1);
        WithEnergy(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await ExecuteCardAttack(context, cardPlay);

        if (IsLowHealth())
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        if (TurnStateTracker.LostHpThisTurnFor(Owner.Creature))
            await DrawCards(context, DynamicVars.Cards.BaseValue);
    }
}
