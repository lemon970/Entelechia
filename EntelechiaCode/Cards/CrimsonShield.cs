using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class CrimsonShield : EntelechiaCard
{
    public CrimsonShield() : base(1, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        WithBlock(10);
        WithPower<BloodlossPower>(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        await CommonActions.CardBlock(this, cardPlay);
        if (lowHealth)
            await CreatureCmd.GainBlock(Owner.Creature, 2, default, cardPlay, false);
        else
            await CommonActions.Apply<BloodlossPower>(context, Owner.Creature, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);
    }
}
