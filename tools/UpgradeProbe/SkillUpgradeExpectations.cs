using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Entelechia.EntelechiaCode.Cards;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

internal static class SkillUpgradeExpectations
{
    private static UpgradeValueExpectation Block(decimal baseValue, decimal upgradedValue) =>
        UpgradeTestHarness.Value("block", card => card.DynamicVars.Block.BaseValue, baseValue, upgradedValue);

    private static UpgradeValueExpectation Cards(decimal baseValue, decimal upgradedValue) =>
        UpgradeTestHarness.Value("cards", card => card.DynamicVars.Cards.BaseValue, baseValue, upgradedValue);

    private static UpgradeValueExpectation Heal(decimal baseValue, decimal upgradedValue) =>
        UpgradeTestHarness.Value("heal", card => ReadDynamicValue(() => card.DynamicVars.Heal.BaseValue, "Heal"), baseValue, upgradedValue);

    private static UpgradeValueExpectation Power<T>(decimal baseValue, decimal upgradedValue)
        where T : PowerModel =>
        UpgradeTestHarness.Value(
            typeof(T).Name,
            card => ReadDynamicValue(() => card.DynamicVars.Power<T>().BaseValue, typeof(T).Name),
            baseValue,
            upgradedValue);

    private static object ReadDynamicValue(Func<decimal> read, string name)
    {
        try
        {
            return read();
        }
        catch (KeyNotFoundException)
        {
            return $"<missing {name}>";
        }
    }

    private static UpgradeValueExpectation Property(string name, object baseValue, object upgradedValue) =>
        UpgradeTestHarness.Value(name, card => ReadProperty(card, name), baseValue, upgradedValue);

    private static UpgradeValueExpectation Source(string name, params string[] requiredFragments) =>
        UpgradeTestHarness.SourceContains(name, requiredFragments);

    private static object? ReadProperty(EntelechiaCard card, string name)
    {
        for (var type = card.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.GetValue(card);
        }

        return $"<missing {name}>";
    }

    internal static IReadOnlyList<CardUpgradeCase> Cases { get; } =
    [
        UpgradeTestHarness.Case<Autophagy>(CardType.Skill, Property("HpCost", 4m, 3m), UpgradeTestHarness.Value("Exhaust keyword", card => card.Keywords.Contains(CardKeyword.Exhaust), false, false)),
        UpgradeTestHarness.Case<BloodBorrow>(CardType.Skill, Cards(3m, 4m), Source("health-dependent HpCost", "IsLowHealth() ? 3m : 4m")),
        UpgradeTestHarness.Case<BloodDebtSettlement>(CardType.Skill, Property("ExhaustsOnPlay", true, false)),
        UpgradeTestHarness.Case<BloodFragrance>(CardType.Skill, Power<BloodHarvestPower>(2m, 3m), Power<BloodlossPower>(2m, 3m)),
        UpgradeTestHarness.Case<BloodHaste>(CardType.Skill, Power<BloodSpeedPower>(1m, 2m), Property("HpCost", 3m, 3m)),
        UpgradeTestHarness.Case<BloodInfect>(CardType.Skill, Power<BloodlossPower>(4m, 6m)),
        UpgradeTestHarness.Case<EntelechiaBloodletting>(CardType.Skill, Power<BloodSpeedPower>(1m, 1m), Power<BloodlettingStrengthPower>(2m, 3m), Property("HpCost", 6m, 6m), UpgradeTestHarness.Value("energy cost", card => card.EnergyCost.GetAmountToSpend(), 0m, 0m)),
        UpgradeTestHarness.Case<BloodMend>(CardType.Skill, Cards(2m, 3m), Heal(5m, 7m)),
        UpgradeTestHarness.Case<BloodMist>(CardType.Skill, Block(7m, 9m), Power<BloodlossPower>(1m, 2m)),
        UpgradeTestHarness.Case<BloodOffering>(CardType.Skill, Cards(2m, 3m), Property("HpCost", 4m, 4m), UpgradeTestHarness.Value("Exhaust keyword", card => card.Keywords.Contains(CardKeyword.Exhaust), false, false)),
        UpgradeTestHarness.Case<BloodOverload>(CardType.Skill, Power<BloodSpeedPower>(2m, 3m), Property("HpCost", 4m, 4m), UpgradeTestHarness.Value("energy cost", card => card.EnergyCost.GetAmountToSpend(), 0m, 0m)),
        UpgradeTestHarness.Case<BloodPulse>(CardType.Skill, Cards(2m, 3m), Property("HpCost", 3m, 3m)),
        UpgradeTestHarness.Case<BloodRebuild>(
            CardType.Skill,
            Block(8m, 8m),
            UpgradeTestHarness.Value("energy cost", card => card.EnergyCost.GetAmountToSpend(), 2m, 1m)),
        UpgradeTestHarness.Case<BloodShield>(CardType.Skill, Block(10m, 13m)),
        UpgradeTestHarness.Case<BloodToCandle>(CardType.Skill, Property("CandlePercentPerGroup", 4m, 6m)),
        UpgradeTestHarness.Case<BloodVeil>(
            CardType.Skill,
            Block(6m, 9m),
            UpgradeTestHarness.Value("Defend tag", card => card.Tags.Contains(CardTag.Defend), true, true)),
        UpgradeTestHarness.Case<ClottingBackflow>(CardType.Skill, Cards(2m, 3m)),
        UpgradeTestHarness.Case<ClottingBarrier>(CardType.Skill, Block(8m, 11m), Power<ClottingBarrierPower>(2m, 3m)),
        UpgradeTestHarness.Case<CrimsonEmbers>(CardType.Skill, Block(6m, 8m), Heal(6m, 8m)),
        UpgradeTestHarness.Case<CrimsonSacrifice>(
            CardType.Skill,
            Cards(4m, 4m),
            Property("HpCost", 8m, 8m),
            UpgradeTestHarness.Value(
                "Exhaust keyword",
                card => card.Keywords.Contains(CardKeyword.Exhaust),
                true,
                false)),
        UpgradeTestHarness.Case<CrimsonShield>(CardType.Skill, Block(10m, 13m), Power<BloodlossPower>(1m, 1m)),
        UpgradeTestHarness.Case<DiscontinuousPulse>(CardType.Skill, Power<BloodlossPower>(2m, 3m)),
        UpgradeTestHarness.Case<HeartBrand>(CardType.Skill, Power<HeartCandlePower>(12m, 18m), Cards(1m, 1m), Property("HpCost", 3m, 3m), Property("ExistingCandlePercent", 6m, 9m)),
        UpgradeTestHarness.Case<HeartCandleRitual>(CardType.Skill, Power<HeartCandlePower>(18m, 27m)),
        UpgradeTestHarness.Case<ImmortalBloodline>(CardType.Skill, Power<ImmortalBloodlinePower>(4m, 0m)),
        UpgradeTestHarness.Case<ReviveCandle>(CardType.Skill, Property("ReviveRatio", 0.50m, 1.00m), UpgradeTestHarness.Value("energy cost", card => card.EnergyCost.GetAmountToSpend(), 0m, 0m)),
        UpgradeTestHarness.Case<ResidualPulse>(CardType.Skill, Block(0m, 2m)),
        UpgradeTestHarness.Case<ResidualPulseConduit>(CardType.Skill, Block(6m, 9m)),
        UpgradeTestHarness.Case<RoseStep>(CardType.Skill, Block(4m, 6m), Power<RoseStepPower>(2m, 3m)),
        UpgradeTestHarness.Case<SanguineRite>(CardType.Skill, Power<BloodHarvestPower>(2m, 3m), Power<BloodlossPower>(2m, 3m)),
        UpgradeTestHarness.Case<Suture>(CardType.Skill, Block(7m, 9m), Property("ConditionalBlock", 4m, 5m))
    ];
}
