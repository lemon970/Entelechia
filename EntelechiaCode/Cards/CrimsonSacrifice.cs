using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Cards;

public class CrimsonSacrifice : EntelechiaCard
{
    protected override decimal HpCost => 8m;

    public CrimsonSacrifice() : base(1, CardType.Skill, CardRarity.Rare, TargetType.None)
    {
        WithCards(4);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!await TryPayHpCost(context, HpCost, cardPlay)) return;
        await DrawCards(context, DynamicVars.Cards.BaseValue);
    }
}
