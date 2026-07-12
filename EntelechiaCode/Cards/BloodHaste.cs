using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodHaste : EntelechiaCard
{
    protected override decimal HpCost => 2m;

    public BloodHaste() : base(0, CardType.Skill, CardRarity.Common, TargetType.None)
    {
        WithPower<BloodSpeedPower>(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<BloodSpeedPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!await TryPayHpCost(context, HpCost, cardPlay)) return;
        await CardCmd.Exhaust(context, this, false, false);
        await CommonActions.Apply<BloodSpeedPower>(context, Owner.Creature, this, DynamicVars.Power<BloodSpeedPower>().BaseValue, true);
    }
}
