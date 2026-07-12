using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;
using BaseLib.Extensions;

namespace Entelechia.EntelechiaCode.Cards;

public class CrimsonLash : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public CrimsonLash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7);
        WithPower<BloodlossPower>(2);
        WithCards(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.Power<BloodlossPower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { CurrentHp: > 0 } target) return;

        var lowHealth = IsLowHealth();
        await ExecuteCardAttack(context, cardPlay);
        if (target.CurrentHp <= 0) return;

        var bloodloss = DynamicVars.Power<BloodlossPower>().BaseValue + (lowHealth ? 1m : 0m);
        await CommonActions.Apply<BloodlossPower>(context, target, this, bloodloss, true);
        if (!lowHealth)
            await DrawCards(context, DynamicVars.Cards.BaseValue);
    }
}
