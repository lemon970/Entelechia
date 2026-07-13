using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodDemonForm : EntelechiaCard
{
    public BloodDemonForm() : base(2, CardType.Power, CardRarity.Rare, TargetType.None)
    {
        WithPower<BloodDemonFormPower>(1);
        WithPower<StrengthPower>(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<StrengthPower>().UpgradeValueBy(1);
    }

    public decimal RuntimePowerAmount => DynamicVars.Power<BloodDemonFormPower>().BaseValue;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<StrengthPower>(context, Owner.Creature, this, DynamicVars.Power<StrengthPower>().BaseValue, true);
        await CommonActions.Apply<BloodDemonFormPower>(context, Owner.Creature, this, RuntimePowerAmount, true);
    }
}
