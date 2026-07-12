using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class SpiritAndDesireFarewell : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public SpiritAndDesireFarewell() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(10);
        WithPower<HeartCandlePower>(12);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Power<HeartCandlePower>().UpgradeValueBy(6);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var targets = this.GetTargets().Where(enemy => enemy.CurrentHp > 0).ToList();
        foreach (var enemy in targets.Where(enemy => enemy.CurrentHp > 0))
            await ExecuteAttack(context, new AttackCommand(BaseDamage).FromCard(this, cardPlay).Targeting(enemy).WithHitCount(2));
        foreach (var enemy in targets.Where(enemy => enemy.CurrentHp > 0))
            await HeartCandlePower.ApplyPercent(context, enemy, this, DynamicVars.Power<HeartCandlePower>().BaseValue, true);
    }
}
