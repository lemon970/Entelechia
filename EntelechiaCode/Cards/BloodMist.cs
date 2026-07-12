using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodMist : EntelechiaCard
{
    public BloodMist() : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
        WithBlock(7);
        WithPower<BloodlossPower>(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars.Power<BloodlossPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        foreach (var target in this.GetTargets().Where(target => target.CurrentHp > 0))
            await CommonActions.Apply<BloodlossPower>(context, target, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);
    }
}
