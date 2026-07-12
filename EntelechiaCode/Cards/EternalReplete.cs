using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class EternalReplete : EntelechiaCard
{
    public EternalReplete() : base(2, CardType.Power, CardRarity.Rare, TargetType.None)
    {
        WithPower<EternalRepletePower>(1);
    }

    public decimal RuntimePowerAmount => DynamicVars.Power<EternalRepletePower>().BaseValue;
    public decimal RuntimeHealRatio => EternalRepletePower.HealRatioFor(RuntimePowerAmount);

    protected override void OnUpgrade()
    {
        DynamicVars.Power<EternalRepletePower>().UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (IsLowHealth())
        {
            var enemies = Owner.Creature.CombatState?.Enemies;
            if (enemies != null)
                foreach (var enemy in enemies.ToList().Where(enemy => enemy.CurrentHp > 0))
                    await CommonActions.Apply<BloodHarvestPower>(context, enemy, this, 2, true);

            await CommonActions.Apply<BloodSpeedPower>(context, Owner.Creature, this, 1, true);
        }
        else
        {
            await DrawCards(context, 2);
        }

        await CommonActions.Apply<EternalRepletePower>(context, Owner.Creature, this, RuntimePowerAmount, true);
    }
}
