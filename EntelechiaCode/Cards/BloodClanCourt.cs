using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodClanCourt : EntelechiaCard
{
    public BloodClanCourt() : base(2, CardType.Power, CardRarity.Rare, TargetType.None)
    {
        WithPower<BloodClanCourtPower>(2);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    public decimal RuntimePowerAmount => DynamicVars.Power<BloodClanCourtPower>().BaseValue;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var enemies = Owner.Creature.CombatState?.Enemies;
        if (enemies != null)
            foreach (var enemy in enemies.ToList().Where(enemy => enemy.CurrentHp > 0))
                await CommonActions.Apply<BloodlossPower>(context, enemy, this, RuntimePowerAmount, true);

        await CommonActions.Apply<BloodClanCourtPower>(context, Owner.Creature, this, RuntimePowerAmount, true);
    }
}
