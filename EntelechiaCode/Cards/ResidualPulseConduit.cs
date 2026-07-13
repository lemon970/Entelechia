using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class ResidualPulseConduit : EntelechiaCard
{
    public ResidualPulseConduit() : base(1, CardType.Skill, CardRarity.Rare, TargetType.None)
    {
        WithBlock(6);
        WithTips(card => [HoverTipFactory.FromCard<ResidualPulse>(card.IsUpgraded)]);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        if (Owner.Creature.CombatState is CombatState combatState)
            await ResidualPulse.CreateInHand(Owner, combatState, IsUpgraded);
    }
}
