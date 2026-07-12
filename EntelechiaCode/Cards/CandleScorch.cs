using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;
using BaseLib.Extensions;

namespace Entelechia.EntelechiaCode.Cards;

public class CandleScorch : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public CandleScorch() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7);
        WithPower<HeartCandlePower>(5);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Power<HeartCandlePower>().UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
            await HeartCandlePower.ApplyPercent(context, cardPlay.Target, this, DynamicVars.Power<HeartCandlePower>().BaseValue, true);
        await ExecuteCardAttack(context, cardPlay);
    }
}
