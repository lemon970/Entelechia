using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodBlade : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public BloodBlade() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(6);
        WithPower<BloodHarvestPower>(1);
        WithTags(CardTag.Strike);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // ponytail: AttackCommand needs explicit execution
        await ExecuteCardAttack(context, cardPlay);
        if (cardPlay.Target is { CurrentHp: > 0 } target)
            await CommonActions.Apply<BloodHarvestPower>(
                context,
                target,
                this,
                DynamicVars.Power<BloodHarvestPower>().BaseValue,
                true);
    }
}
