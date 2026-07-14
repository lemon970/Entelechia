using BaseLib.Extensions;
using BaseLib.Utils;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class ResidualPulseConduit : EntelechiaCard
{
    public ResidualPulseConduit() : base(1, CardType.Power, CardRarity.Rare, TargetType.None)
    {
        WithPower<ResidualPulseConduitPower>(1);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<ResidualPulseConduitPower>(
            context,
            Owner.Creature,
            this,
            DynamicVars.Power<ResidualPulseConduitPower>().BaseValue,
            true);
    }
}
