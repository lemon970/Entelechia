using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodFrenzy : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;

    public BloodFrenzy() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(3);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var refundEnergy = IsLowHealth();
        await ExecuteCardAttack(context, cardPlay, 8);
        if (refundEnergy)
            await PlayerCmd.GainEnergy(1, Owner);
    }
}
