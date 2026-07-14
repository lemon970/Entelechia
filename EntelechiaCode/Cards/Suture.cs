using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class Suture : EntelechiaCard
{
    private decimal _conditionalBlock = 4m;
    private decimal ConditionalBlock => _conditionalBlock;

    public Suture() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        WithBlock(7);
        WithTip(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
        _conditionalBlock += 1m;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        if (IsLowHealth() || TurnStateTracker.HealedThisTurnFor(Owner.Creature))
            await CreatureCmd.GainBlock(Owner.Creature, ConditionalBlock, default, cardPlay, false);

        await TryExhaustAnotherCard(context);
    }
}
