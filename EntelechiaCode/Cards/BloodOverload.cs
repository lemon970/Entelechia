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

public class BloodOverload : EntelechiaCard
{
    protected override decimal HpCost => 4m;

    public BloodOverload() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        WithPower<BloodSpeedPower>(2);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<BloodSpeedPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        if (!await TryPayHpCost(context, HpCost, cardPlay)) return;

        await CommonActions.Apply<BloodSpeedPower>(
            context,
            Owner.Creature,
            this,
            DynamicVars.Power<BloodSpeedPower>().BaseValue,
            true);

        if (lowHealth)
            await PlayerCmd.GainEnergy(1, Owner);
        else
            await DrawCards(context, 1);
    }
}
