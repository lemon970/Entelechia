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

    public CounterSlash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8);
        WithCards(0);
        WithEnergy(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        await ExecuteCardAttack(context, cardPlay);
        if (lowHealth || TurnStateTracker.LostHpThisTurn)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            if (DynamicVars.Cards.BaseValue > 0)
                await DrawCards(context, DynamicVars.Cards.BaseValue);
        }
    }
}
