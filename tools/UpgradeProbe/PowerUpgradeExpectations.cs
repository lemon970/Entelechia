using System.Reflection;
using BaseLib.Extensions;
using Entelechia.EntelechiaCode.Cards;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;

internal static class PowerUpgradeExpectations
{
    internal static IReadOnlyList<CardUpgradeCase> Cases { get; } =
    [
        UpgradeTestHarness.Case<BloodClanCourt>(
            CardType.Power,
            UpgradeTestHarness.Value("energy cost", EnergyCost, 3m, 2m),
            UpgradeTestHarness.Value("runtime Bloodloss amount", RuntimeValue("RuntimePowerAmount"), 2m, 2m)),
        UpgradeTestHarness.Case<BloodDemonForm>(
            CardType.Power,
            UpgradeTestHarness.Value("energy cost", EnergyCost, 3m, 2m),
            UpgradeTestHarness.Value("runtime power amount", RuntimeValue("RuntimePowerAmount"), 1m, 1m)),
        UpgradeTestHarness.Case<BloodFeast>(
            CardType.Power,
            UpgradeTestHarness.Value("power amount", Power<BloodFeastPower>, 1m, 1m),
            UpgradeTestHarness.Value("trigger threshold", RuntimeValue("TriggerThreshold"), 3, 2)),
        UpgradeTestHarness.Case<CandleEmber>(
            CardType.Power,
            UpgradeTestHarness.Value("power amount", Power<CandleEmberPower>, 1m, 2m),
            UpgradeTestHarness.Value("runtime power amount", RuntimeValue("RuntimePowerAmount"), 1m, 2m)),
        UpgradeTestHarness.Case<ClotInstinct>(
            CardType.Power,
            UpgradeTestHarness.Value("power amount", Power<ClotInstinctPower>, 4m, 6m),
            UpgradeTestHarness.Value("runtime block amount", RuntimeValue("RuntimePowerAmount"), 4m, 6m)),
        UpgradeTestHarness.Case<EternalReplete>(
            CardType.Power,
            UpgradeTestHarness.Value("power amount", Power<EternalRepletePower>, 1m, 2m),
            UpgradeTestHarness.Value("runtime heal ratio", RuntimeValue("RuntimeHealRatio"), 0.50m, 0.55m)),
        UpgradeTestHarness.Case<PainConversion>(
            CardType.Power,
            UpgradeTestHarness.Value("power amount", Power<PainConversionPower>, 1m, 2m),
            UpgradeTestHarness.Value("runtime triggers per turn", RuntimeValue("RuntimePowerAmount"), 1m, 2m))
    ];

    private static object EnergyCost(EntelechiaCard card) => card.EnergyCost.GetAmountToSpend();

    private static object? Power<TPower>(EntelechiaCard card)
        where TPower : BaseLib.Abstracts.CustomPowerModel
    {
        try
        {
            return card.DynamicVars.Power<TPower>().BaseValue;
        }
        catch (KeyNotFoundException)
        {
            return $"missing {typeof(TPower).Name} dynamic variable";
        }
    }

    private static Func<EntelechiaCard, object?> RuntimeValue(string propertyName)
        => card => card.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?
            .GetValue(card);
}
