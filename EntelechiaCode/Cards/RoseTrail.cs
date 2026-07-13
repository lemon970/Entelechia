using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;
using BaseLib.Extensions;

namespace Entelechia.EntelechiaCode.Cards;

public class RoseTrail : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public RoseTrail() : base(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(3);
        WithPower<BloodHarvestPower>(2);
        WithPower<BloodlossPower>(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
        DynamicVars.Power<BloodHarvestPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await ExecuteCardAttack(context, cardPlay, 2);
        if (cardPlay.Target is { CurrentHp: > 0 } target)
        {
            await CommonActions.Apply<BloodHarvestPower>(context, target, this, DynamicVars.Power<BloodHarvestPower>().BaseValue, true);
            await CommonActions.Apply<BloodlossPower>(context, target, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);
        }
    }
}
