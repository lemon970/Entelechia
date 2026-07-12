using BaseLib.Utils;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Entelechia.EntelechiaCode.Powers;

namespace Entelechia.EntelechiaCode.Cards;

public class BloodFeast : EntelechiaCard
{
    public BloodFeast() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.None)
    {
        WithPower<BloodFeastPower>(1);
    }

    public int TriggerThreshold { get; private set; } = 3;

    protected override void OnUpgrade()
    {
        TriggerThreshold--;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await CommonActions.Apply<BloodFeastPower>(
            context,
            Owner.Creature,
            this,
            DynamicVars.Power<BloodFeastPower>().BaseValue,
            true);
        var feast = Owner.Creature.Powers?.OfType<BloodFeastPower>().FirstOrDefault();
        if (feast != null) feast.Threshold = Math.Min(feast.Threshold, TriggerThreshold);
    }
}
