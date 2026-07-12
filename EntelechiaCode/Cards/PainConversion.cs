using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class PainConversion : EntelechiaCard
{
    public PainConversion() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
    {
        WithPower<PainConversionPower>(1);
    }

    public decimal RuntimePowerAmount => DynamicVars.Power<PainConversionPower>().BaseValue;

    protected override void OnUpgrade()
    {
        DynamicVars.Power<PainConversionPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<PainConversionPower>(context, Owner.Creature, this, RuntimePowerAmount, true);
        if (IsHighHealth())
            await DrawCards(context, 1);
    }
}
