using BaseLib.Extensions;
using Entelechia.EntelechiaCode.Cards;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

internal static class AttackUpgradeExpectations
{
    private static UpgradeValueExpectation Damage(decimal baseValue, decimal upgradedValue)
        => UpgradeTestHarness.Value("damage", card => card.DynamicVars.Damage.BaseValue, baseValue, upgradedValue);

    private static UpgradeValueExpectation RuntimeDamage(decimal baseValue, decimal upgradedValue)
        => UpgradeTestHarness.Value("AttackBaseDamage", card => card.AttackBaseDamage, baseValue, upgradedValue);

    private static UpgradeValueExpectation Source(string name, params string[] requiredFragments)
        => UpgradeTestHarness.SourceContains(name, requiredFragments);

    private static UpgradeValueExpectation Cards(decimal baseValue, decimal upgradedValue)
        => UpgradeTestHarness.Value("cards", card => Read(() => card.DynamicVars.Cards.BaseValue), baseValue, upgradedValue);

    private static UpgradeValueExpectation Energy(decimal baseValue, decimal upgradedValue)
        => UpgradeTestHarness.Value("energy", card => Read(() => card.DynamicVars.Energy.BaseValue), baseValue, upgradedValue);

    private static UpgradeValueExpectation Heal(decimal baseValue, decimal upgradedValue)
        => UpgradeTestHarness.Value("heal", card => Read(() => card.DynamicVars.Heal.BaseValue), baseValue, upgradedValue);

    private static UpgradeValueExpectation Power<T>(decimal baseValue, decimal upgradedValue, string? name = null)
        where T : MegaCrit.Sts2.Core.Models.PowerModel
        => UpgradeTestHarness.Value(
            name ?? typeof(T).Name,
            card => Read(() => name is null
                ? card.DynamicVars.Power<T>().BaseValue
                : card.DynamicVars.Var<PowerVar<T>>(name).BaseValue),
            baseValue,
            upgradedValue);

    private static object? Read(Func<object?> read)
    {
        try
        {
            return read();
        }
        catch (Exception exception) when (exception is KeyNotFoundException or ArgumentException)
        {
            return null;
        }
    }

    internal static IReadOnlyList<CardUpgradeCase> Cases { get; } =
    [
        UpgradeTestHarness.Case<BloodBlade>(CardType.Attack, Damage(6, 9), RuntimeDamage(6, 9)),
        UpgradeTestHarness.Case<BloodClawSlash>(CardType.Attack, Damage(3, 4), RuntimeDamage(3, 4)),
        UpgradeTestHarness.Case<BloodDissect>(CardType.Attack, Damage(9, 12), RuntimeDamage(9, 12), Power<BloodHarvestPower>(3, 4, "ExtraDamage"), Source("ExtraDamage runtime", "DynamicVars.Var<PowerVar<BloodHarvestPower>>(\"ExtraDamage\").BaseValue", "new AttackCommand(extraDamage)")),
        UpgradeTestHarness.Case<BloodDrain>(CardType.Attack, Damage(8, 11), RuntimeDamage(8, 11), Power<BloodHarvestPower>(1, 2), Source("harvest runtime", "var harvestAmount = DynamicVars.Power<BloodHarvestPower>().BaseValue", "CommonActions.Apply<BloodHarvestPower>")),
        UpgradeTestHarness.Case<BloodFrenzy>(CardType.Attack, Damage(3, 4), RuntimeDamage(3, 4)),
        UpgradeTestHarness.Case<BloodSplash>(CardType.Attack, Damage(5, 7), RuntimeDamage(5, 7), Power<BloodHarvestPower>(1, 2), Source("harvest runtime", "CommonActions.Apply<BloodHarvestPower>", "DynamicVars.Power<BloodHarvestPower>().BaseValue")),
        UpgradeTestHarness.Case<BloodStorm>(CardType.Attack, Damage(16, 20), RuntimeDamage(16, 20), Power<BloodlossPower>(2, 3), Source("bloodloss runtime", "CommonActions.Apply<BloodlossPower>", "DynamicVars.Power<BloodlossPower>().BaseValue")),
        UpgradeTestHarness.Case<BloodStrike>(CardType.Attack, Damage(10, 14), RuntimeDamage(10, 14)),
        UpgradeTestHarness.Case<BloodSurge>(CardType.Attack, Damage(6, 9), RuntimeDamage(6, 9)),
        UpgradeTestHarness.Case<BloodSweep>(CardType.Attack, Damage(12, 16), RuntimeDamage(12, 16), Power<HeartCandlePower>(8, 12), Source("heart candle runtime", "HeartCandlePower.ApplyPercent", "DynamicVars.Power<HeartCandlePower>().BaseValue")),
        UpgradeTestHarness.Case<CandleScorch>(CardType.Attack, Damage(7, 9), RuntimeDamage(7, 9), Power<HeartCandlePower>(5, 8), Source("heart candle runtime", "HeartCandlePower.ApplyPercent", "DynamicVars.Power<HeartCandlePower>().BaseValue")),
        UpgradeTestHarness.Case<CounterSlash>(CardType.Attack, Damage(8, 10), RuntimeDamage(8, 10), Cards(0, 1), Energy(1, 1), Source("draw and energy runtime values", "PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner)", "DrawCards(context, DynamicVars.Cards.BaseValue)")),
        UpgradeTestHarness.Case<CrimsonLash>(CardType.Attack, Damage(7, 10), RuntimeDamage(7, 10), Power<BloodlossPower>(2, 3), Cards(1, 1), Source("bloodloss runtime", "DynamicVars.Power<BloodlossPower>().BaseValue", "CommonActions.Apply<BloodlossPower>")),
        UpgradeTestHarness.Case<CrimsonMerge>(CardType.Attack, Damage(8, 11), RuntimeDamage(8, 11), Power<HeartCandlePower>(4, 6), Cards(1, 1), Energy(1, 1), Source("heart candle runtime", "var hcAmount = DynamicVars.Power<HeartCandlePower>().BaseValue", "HeartCandlePower.ApplyPercent")),
        UpgradeTestHarness.Case<FarewellFinale>(CardType.Attack, Damage(7, 9), RuntimeDamage(7, 9)),
        UpgradeTestHarness.Case<Lacerate>(CardType.Attack, Damage(8, 11), RuntimeDamage(8, 11), Power<BloodlossPower>(3, 4), Power<BloodlossPower>(2, 3, "HpLoss"), Source("HpLoss runtime", "TurnStateTracker.LoseHpTracking", "DynamicVars.Var<PowerVar<BloodlossPower>>(\"HpLoss\").BaseValue")),
        UpgradeTestHarness.Case<RedCandleAll>(CardType.Attack, Damage(7, 9), RuntimeDamage(7, 9)),
        UpgradeTestHarness.Case<RoseThorn>(CardType.Attack, Damage(3, 5), RuntimeDamage(3, 5), Cards(1, 1)),
        UpgradeTestHarness.Case<RoseTrail>(CardType.Attack, Damage(4, 5), RuntimeDamage(4, 5), Power<BloodHarvestPower>(2, 3), Power<BloodlossPower>(1, 1), Source("bloodloss runtime", "CommonActions.Apply<BloodlossPower>", "DynamicVars.Power<BloodlossPower>().BaseValue")),
        UpgradeTestHarness.Case<SoulBloodDraw>(CardType.Attack, Damage(16, 21), RuntimeDamage(16, 21), Heal(6, 8), Cards(1, 1), Energy(1, 1), Source("heal draw and energy actions", "TurnStateTracker.HealTracking(Owner.Creature, DynamicVars.Heal.BaseValue", "PlayerCmd.GainEnergy(1, Owner)", "DrawCards(context, DynamicVars.Cards.BaseValue)")),
        UpgradeTestHarness.Case<SpiritAndDesireFarewell>(CardType.Attack, Damage(10, 12), RuntimeDamage(10, 12), Power<HeartCandlePower>(12, 18), Source("heart candle runtime", "HeartCandlePower.ApplyPercent", "DynamicVars.Power<HeartCandlePower>().BaseValue"))
    ];
}
