using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class EternalReplete : EntelechiaCard
{
    public EternalReplete() : base(1, CardType.Power, CardRarity.Rare, TargetType.None)
    {
        WithPower<EternalRepletePower>(1);
        WithPower<EternalRepletePower>("EmberHarvest", 2);
    }

    public decimal RuntimePowerAmount => DynamicVars.Power<EternalRepletePower>().BaseValue;
    public decimal RuntimeHealRatio => IsUpgraded ? 0.55m : 0.50m;
    public decimal RuntimeEmberHarvestAmount =>
        DynamicVars.Var<PowerVar<EternalRepletePower>>("EmberHarvest").BaseValue;

    protected override void OnUpgrade()
    {
        DynamicVars.Var<PowerVar<EternalRepletePower>>("EmberHarvest").UpgradeValueBy(1);
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
        await CommonActions.Apply<EternalRepletePower>(context, Owner.Creature, this, RuntimePowerAmount, true);
        var power = Owner.Creature.Powers?.OfType<EternalRepletePower>().FirstOrDefault();
        if (power != null)
        {
            power.HealRatio = Math.Max(power.HealRatio, RuntimeHealRatio);
            power.EmberHarvestAmount = Math.Max(power.EmberHarvestAmount, RuntimeEmberHarvestAmount);
        }
    }
}
