using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Entelechia.EntelechiaCode.Relics;

public class BloodDemonReplete : EntelechiaRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    private bool _usedThisNode = false;
    private bool _emberAppliedThisCombat = false;
    private int _emberBloodlineStacks = 0;
    private bool _emberBloodlineHitsAllEnemies = false;

    public bool CanPreventDeath(Creature creature)
    {
        return !_usedThisNode
            && Owner?.Creature == creature
            && creature.CombatState?.PlayerCreatures?.Contains(creature) == true;
    }

    public override bool ShouldDie(Creature creature)
    {
        return !CanPreventDeath(creature);
    }

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (Owner?.Creature == creature) return 0m;
        if (creature.Player?.Relics?.Contains(this) == true) return 0m;
        return amount;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        _usedThisNode = true;

        var eternalReplete = creature.Powers?.OfType<EternalRepletePower>().FirstOrDefault();
        var healRatio = eternalReplete?.HealRatio ?? 0.4m;

        await CreatureCmd.SetCurrentHp(creature, Math.Max(1, Math.Ceiling(creature.MaxHp * healRatio)));
        var vulnerable = creature.Powers?.FirstOrDefault(power => power is VulnerablePower);
        if (vulnerable != null)
            await PowerCmd.Remove(vulnerable);
        var weak = creature.Powers?.FirstOrDefault(power => power is WeakPower);
        if (weak != null)
            await PowerCmd.Remove(weak);
        await CommonActions.Apply<CrimsonWardPower>(creature, (CardModel)null!, 3m, false);
        await CommonActions.Apply<BloodSpeedPower>(creature, (CardModel)null!, 2m, false);

        if (creature.CombatState?.Enemies != null)
            foreach (var enemy in creature.CombatState.Enemies.Where(enemy => enemy.CurrentHp > 0))
                await CommonActions.Apply<BloodHarvestPower>(enemy, (CardModel)null!, 3m, false);

        _emberBloodlineStacks++;
        if (eternalReplete != null)
            _emberBloodlineHitsAllEnemies = true;
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side != CombatSide.Player) return;
        if (_emberAppliedThisCombat || _emberBloodlineStacks <= 0) return;
        if (_usedThisNode) return;
        if (Owner?.Creature == null) return;
        if (Owner.Creature.CombatState != combatState) return;

        _emberAppliedThisCombat = true;
        await CommonActions.Apply<EmberBloodlinePower>(context, Owner.Creature, null, _emberBloodlineStacks, false);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side != CombatSide.Player) return;
        if (!_emberAppliedThisCombat || !_emberBloodlineHitsAllEnemies) return;
        if (Owner?.Creature == null || Owner.Creature.CombatState != combatState) return;

        if (combatState.Enemies == null) return;
        foreach (var enemy in combatState.Enemies.Where(enemy => enemy.CurrentHp > 0))
            await CommonActions.Apply<BloodHarvestPower>(enemy, (CardModel)null!, 2m, false);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _usedThisNode = false;
        _emberAppliedThisCombat = false;
        return Task.CompletedTask;
    }

    public override Task AfterActEntered()
    {
        _usedThisNode = false;
        _emberAppliedThisCombat = false;
        _emberBloodlineStacks = 0;
        _emberBloodlineHitsAllEnemies = false;
        return Task.CompletedTask;
    }
}
