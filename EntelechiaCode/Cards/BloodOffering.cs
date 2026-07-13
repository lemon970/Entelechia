using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodOffering : EntelechiaCard
{
    protected override decimal HpCost => 4m;

    public BloodOffering() : base(0, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        WithCards(2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!await TryPayHpCost(context, HpCost, cardPlay)) return;
        await DrawCards(context, DynamicVars.Cards.BaseValue);
    }
}
