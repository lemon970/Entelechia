using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class RedCandleAll : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;
    public RedCandleAll() : base(2, CardType.Attack, CardRarity.Rare, TargetType.None)
    {
        WithDamage(7);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var enemies = Owner.Creature.CombatState?.Enemies;
        if (enemies == null) return;
        foreach (var enemy in enemies.ToList().Where(enemy => enemy.CurrentHp > 0))
        {
            if (enemy.Powers?.Any(p => p is HeartCandlePower) != true) continue;
            await ExecuteAttack(
                context,
                new AttackCommand(BaseDamage).FromCardCompatibility(this, cardPlay).Targeting(enemy),
                cardPlay: cardPlay);
        }
    }
}
