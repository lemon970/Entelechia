using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodStrike : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public BloodStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(10);
        WithCards(1);
        WithHeal(3);
        WithKeywords(EntelechiaKeywords.HighHealth, EntelechiaKeywords.LowHealth);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var target = cardPlay.Target;

        await ExecuteCardAttack(context, cardPlay);

        if (IsHighHealth() && target.CurrentHp > 0)
            await DrawCards(context, DynamicVars.Cards.BaseValue);
        else if (IsLowHealth() && target.CurrentHp <= 0)
            await TurnStateTracker.HealTracking(Owner.Creature, DynamicVars.Heal.BaseValue, false);
    }
}
