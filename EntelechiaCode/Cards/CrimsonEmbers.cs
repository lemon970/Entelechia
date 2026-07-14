using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Entelechia.EntelechiaCode.Cards;

// 赤色余温: Heal 6 HP. If you lost HP this turn, gain 6 block.
public class CrimsonEmbers : EntelechiaCard
{
    public CrimsonEmbers() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
    {
        WithBlock(6);
        WithHeal(6);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars.Heal.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var lowHealth = IsLowHealth();
        await TurnStateTracker.HealTracking(Owner.Creature, DynamicVars.Heal.BaseValue, true);
        if (lowHealth || TurnStateTracker.LostHpThisTurnFor(Owner.Creature))
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, default, cardPlay, false);
    }
}
