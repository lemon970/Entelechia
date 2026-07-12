using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodPulse : EntelechiaCard
{
    public BloodPulse() : base(1, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        WithCards(2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        await DrawCards(context, DynamicVars.Cards.BaseValue);
        if (lowHealth || TurnStateTracker.LostHpThisTurn)
            await PlayerCmd.GainEnergy(1, Owner);
    }
}
