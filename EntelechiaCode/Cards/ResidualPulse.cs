using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Entelechia.EntelechiaCode.Cards;

[Pool(typeof(TokenCardPool))]
public class ResidualPulse : EntelechiaCard
{
    protected override decimal HpCost => IsHighHealth() ? 3m : 0m;

    public ResidualPulse() : base(0, CardType.Skill, CardRarity.Token, TargetType.None)
    {
        WithBlock(0);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var highHealth = IsHighHealth();

        if (IsUpgraded)
            await CommonActions.CardBlock(this, cardPlay);

        if (highHealth)
        {
            if (!await TryPayHpCost(context, HpCost, cardPlay)) return;
            await PlayerCmd.GainEnergy(1m, Owner);
        }
        else
        {
            await DrawCards(context, 1m);
        }
    }
}
