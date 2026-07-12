using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Entelechia.EntelechiaCode.Powers;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Entelechia.EntelechiaCode.Cards;

public class Lacerate : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public Lacerate() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8);
        WithPower<BloodlossPower>(3);
        WithPower<BloodlossPower>("HpLoss", 2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.Power<BloodlossPower>().UpgradeValueBy(1);
        DynamicVars.Var<PowerVar<BloodlossPower>>("HpLoss").UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { CurrentHp: > 0 } target) return;

        var lowHealth = IsLowHealth();
        await ExecuteCardAttack(context, cardPlay);
        if (target.CurrentHp <= 0) return;

        if (lowHealth)
            await TurnStateTracker.LoseHpTracking(context, target, DynamicVars.Var<PowerVar<BloodlossPower>>("HpLoss").BaseValue, DamageProps.nonCardHpLoss, Owner.Creature, this, cardPlay);
        if (target.CurrentHp > 0)
            await CommonActions.Apply<BloodlossPower>(context, target, this, DynamicVars.Power<BloodlossPower>().BaseValue, true);
    }
}
