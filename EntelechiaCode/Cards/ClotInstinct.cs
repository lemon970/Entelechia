using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class ClotInstinct : EntelechiaCard
{
    public ClotInstinct() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
    {
        WithPower<ClotInstinctPower>(4);
    }

    public decimal RuntimePowerAmount => DynamicVars.Power<ClotInstinctPower>().BaseValue;

    protected override void OnUpgrade()
    {
        DynamicVars.Power<ClotInstinctPower>().UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<ClotInstinctPower>(context, Owner.Creature, this, RuntimePowerAmount, true);
    }
}
