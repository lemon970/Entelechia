using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class ClottingBarrier : EntelechiaCard
{
    public ClottingBarrier() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        WithBlock(8);
        WithPower<ClottingBarrierPower>(2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
        DynamicVars.Power<ClottingBarrierPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var block = DynamicVars.Block.BaseValue + (IsLowHealth() ? 3m : 0m);
        var power = DynamicVars.Power<ClottingBarrierPower>().BaseValue;
        await CreatureCmd.GainBlock(Owner.Creature, block, default, null, false);
        await CommonActions.Apply<ClottingBarrierPower>(context, Owner.Creature, this, power, true);
    }
}
