using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Entelechia.EntelechiaCode.Powers;

// ponytail: approximate BloodHarvest trigger by watching for player HP gain (delta > 0)
// GainBlockInternal is public+void on Creature — no context needed
public class ClotInstinctPower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public bool TriggeredThisTurn { get; set; } = false;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return Task.CompletedTask;
        TriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    // ponytail: trigger via Harmony in CombatPatches (BloodHarvest postfix)
    // GainBlockInternal called there where player creature is known
}
