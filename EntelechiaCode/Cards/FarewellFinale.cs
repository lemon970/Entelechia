using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class FarewellFinale : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public FarewellFinale() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(7);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;
        int extraHits = 0;
        if (target.Powers?.FirstOrDefault(p => p is BloodHarvestPower) != null) extraHits++;
        if (target.Powers?.FirstOrDefault(p => p is BloodlossPower) != null) extraHits++;
        if (target.Powers?.FirstOrDefault(p => p is HeartCandlePower) != null) extraHits++;

        extraHits = Math.Min(extraHits, 2);
        for (int i = 0; i < 1 + extraHits; i++)
        {
            if (target.CurrentHp <= 0) break;
            await ExecuteCardAttack(context, cardPlay);
        }
    }
}
