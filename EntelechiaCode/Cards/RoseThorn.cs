using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class RoseThorn : EntelechiaCard
{
    protected override decimal BaseDamage => DynamicVars.Damage.BaseValue;

    public RoseThorn() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(3);
        WithCards(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var hadHarvest = cardPlay.Target?.Powers?.Any(p => p is BloodHarvestPower && p.Amount > 0) == true;
        await ExecuteCardAttack(context, cardPlay);

        if (hadHarvest)
            await CommonActions.Draw(this, context);
    }
}
