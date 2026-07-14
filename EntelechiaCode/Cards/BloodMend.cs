using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodMend : EntelechiaCard
{
    public BloodMend() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        WithCards(2);
        WithHeal(5);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars.Heal.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        if (lowHealth)
            await TurnStateTracker.HealTracking(Owner.Creature, DynamicVars.Heal.BaseValue, true);

        var cards = DynamicVars.Cards.BaseValue;
        await DrawCards(context, lowHealth ? cards - 1m : cards);
    }
}
