using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class CandleEmber : EntelechiaCard
{
    public CandleEmber() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
    {
        WithPower<CandleEmberPower>(1);
    }

    public decimal RuntimePowerAmount => DynamicVars.Power<CandleEmberPower>().BaseValue;

    protected override void OnUpgrade()
    {
        DynamicVars.Power<CandleEmberPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<CandleEmberPower>(context, Owner.Creature, this, RuntimePowerAmount, true);

        if (IsHighHealth())
            await DrawCards(context, 1);
    }
}
