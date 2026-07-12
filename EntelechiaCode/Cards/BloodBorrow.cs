using System.Linq;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodBorrow : EntelechiaCard
{
    protected override decimal HpCost => IsLowHealth() ? 1m : 2m;

    public BloodBorrow() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        WithCards(3);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!await TryPayHpCost(context, HpCost, cardPlay)) return;
        var lowHealth = IsLowHealth();
        var cards = DynamicVars.Cards.BaseValue;
        var drawn = await DrawCards(context, lowHealth ? cards - 1m : cards);
        if (lowHealth)
            await PlayerCmd.GainEnergy(1, Owner);
        else if (drawn != null && drawn.Any(c => c.Type == CardType.Attack))
            await PlayerCmd.GainEnergy(1, Owner);
    }
}
