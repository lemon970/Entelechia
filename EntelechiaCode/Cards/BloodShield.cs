using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodShield : EntelechiaCard
{
    public BloodShield() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        WithBlock(10);
        WithKeywords(EntelechiaKeywords.HighHealth, EntelechiaKeywords.LowHealth);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var highHealth = IsHighHealth();
        var lowHealth = IsLowHealth();
        await CommonActions.CardBlock(this, cardPlay);
        if (highHealth)
            await DrawCards(context, 1);
        else if (lowHealth)
            await PlayerCmd.GainEnergy(1, Owner);
    }
}
