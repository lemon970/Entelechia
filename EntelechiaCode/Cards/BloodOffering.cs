using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodOffering : EntelechiaCard
{
    protected override decimal HpCost => 3m;

    public BloodOffering() : base(0, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        WithCards(2);
        // ponytail: Exhaust API not found in ConstructedCardModel, add later
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!await TryPayHpCost(context, HpCost, cardPlay)) return;
        await CardCmd.Exhaust(context, this, false, false);
        await DrawCards(context, DynamicVars.Cards.BaseValue);
    }
}
