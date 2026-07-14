using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Relics;

public class BloodDemonReplete : EntelechiaRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    [SavedProperty]
    public int LastRevivedFloor { get; set; } = -1;

    private bool _emberAppliedThisCombat;
    private int _emberBloodlineStacks;
    private decimal _emberHarvestAmount;

    public int CurrentFloor => Owner?.RunState?.TotalFloor ?? -1;

    public bool CanPreventDeath(Creature creature)
    {
        var currentFloor = CurrentFloor;
        return currentFloor >= 0
            && LastRevivedFloor != currentFloor
            && Owner?.Creature == creature
            && creature.CombatState?.PlayerCreatures?.Contains(creature) == true;
    }

    public override bool ShouldDie(Creature creature)
    {
        if (!CanPreventDeath(creature)) return true;

        LastRevivedFloor = CurrentFloor;
        return false;
    }

    internal decimal GetDamageMultiplier(Creature? target, ValueProp props, CardModel? cardSource)
    {
        var currentFloor = CurrentFloor;
        if (currentFloor < 0 || LastRevivedFloor != currentFloor) return 1m;
        if (target != Owner?.Creature) return 1m;
        var isAttack = EntelechiaDamage.IsAttackDamage(props, cardSource)
            || props == DamageProps.monsterMove;
        return isAttack ? 0.75m : 1m;
    }

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (Owner?.Creature == creature) return 0m;
        if (creature.Player?.Relics?.Contains(this) == true) return 0m;
        return amount;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        var eternalReplete = creature.Powers?.OfType<EternalRepletePower>().FirstOrDefault();
        var healRatio = eternalReplete?.HealRatio ?? 0.4m;

        await CreatureCmd.SetCurrentHp(creature, Math.Max(1, Math.Ceiling(creature.MaxHp * healRatio)));
        var vulnerable = creature.Powers?.FirstOrDefault(power => power is VulnerablePower);
        if (vulnerable != null)
            await PowerCmd.Remove(vulnerable);
        var weak = creature.Powers?.FirstOrDefault(power => power is WeakPower);
        if (weak != null)
            await PowerCmd.Remove(weak);

        var context = new BlockingPlayerChoiceContext();
        await CardPileCmd.Draw(context, 2m, Owner, false);
        await PlayerCmd.GainEnergy(2m, Owner);

        if (creature.CombatState?.Enemies != null)
            foreach (var enemy in creature.CombatState.Enemies.Where(enemy => enemy.CurrentHp > 0))
                await CommonActions.Apply<BloodHarvestPower>(context, enemy, null, 3m, false);

        _emberBloodlineStacks++;
        if (eternalReplete != null)
            _emberHarvestAmount = Math.Max(_emberHarvestAmount, eternalReplete.EmberHarvestAmount);
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext context,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side != CombatSide.Player) return;
        if (_emberAppliedThisCombat || _emberBloodlineStacks <= 0) return;
        if (LastRevivedFloor == CurrentFloor) return;
        if (Owner?.Creature == null || Owner.Creature.CombatState != combatState) return;

        _emberAppliedThisCombat = true;
        await CommonActions.Apply<EmberBloodlinePower>(
            context,
            Owner.Creature,
            null,
            _emberBloodlineStacks,
            false);

        var ember = Owner.Creature.Powers?.OfType<EmberBloodlinePower>().FirstOrDefault();
        if (ember != null)
            ember.HarvestAllEnemiesAmount = _emberHarvestAmount;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _emberAppliedThisCombat = false;
        return Task.CompletedTask;
    }

    public override Task AfterActEntered()
    {
        _emberAppliedThisCombat = false;
        _emberBloodlineStacks = 0;
        _emberHarvestAmount = 0m;
        return Task.CompletedTask;
    }
}
