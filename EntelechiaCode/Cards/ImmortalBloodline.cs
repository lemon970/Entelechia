using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class ImmortalBloodline : EntelechiaCard
{
    public ImmortalBloodline() : base(1, CardType.Skill, CardRarity.Rare, TargetType.None)
    {
        WithPower<ImmortalBloodlinePower>(4);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<ImmortalBloodlinePower>().UpgradeValueBy(-4);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var refundEnergy = IsLowHealth();
        await CommonActions.Apply<ImmortalBloodlinePower>(context, Owner.Creature, this, DynamicVars.Power<ImmortalBloodlinePower>().BaseValue, true);
        await CardCmd.Exhaust(context, this, false, false);
        if (refundEnergy)
            await PlayerCmd.GainEnergy(1, Owner);
    }
}
