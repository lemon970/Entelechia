using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodVeil : EntelechiaCard
{
    public BloodVeil() : base(1, CardType.Skill, CardRarity.Basic, TargetType.None)
    {
        WithBlock(6);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        await CommonActions.CardBlock(this, cardPlay);
        if (lowHealth)
            await CreatureCmd.GainBlock(Owner.Creature, 2, default, cardPlay, false);
    }
}
