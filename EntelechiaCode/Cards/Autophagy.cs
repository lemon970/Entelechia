using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Cards;

public class Autophagy : EntelechiaCard
{
    private decimal _hpCost = 3m;
    protected override decimal HpCost => _hpCost;

    public Autophagy() : base(0, CardType.Skill, CardRarity.Common, TargetType.None)
    {
    }

    protected override void OnUpgrade()
    {
        _hpCost -= 1m;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!await TryPayHpCost(context, HpCost, cardPlay)) return;
        await CardCmd.Exhaust(context, this, false, false);
        await PlayerCmd.GainEnergy(2, Owner);
    }
}
