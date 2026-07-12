using System.Reflection;
using System.Runtime.CompilerServices;
using BaseLib.Abstracts;
using Entelechia.EntelechiaCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

internal sealed record UpgradeValueExpectation(
    string Name,
    Func<EntelechiaCard, object?> Read,
    object? BaseValue,
    object? UpgradedValue);

internal sealed record CardUpgradeCase(
    Type ModelType,
    CardType CardType,
    IReadOnlyList<UpgradeValueExpectation> Values,
    Action<EntelechiaCard, EntelechiaCard, List<string>>? AdditionalChecks = null);

internal static class UpgradeTestHarness
{
    internal static UpgradeValueExpectation Value(
        string name,
        Func<EntelechiaCard, object?> read,
        object? baseValue,
        object? upgradedValue)
        => new(name, read, baseValue, upgradedValue);

    internal static UpgradeValueExpectation SourceContains(
        string name,
        params string[] requiredFragments)
        => Value(
            $"source {name}",
            card =>
            {
                var path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "EntelechiaCode",
                    "Cards",
                    $"{card.GetType().Name}.cs");
                var source = File.ReadAllText(path);
                return requiredFragments.All(fragment =>
                    source.Contains(fragment, StringComparison.Ordinal));
            },
            true,
            true);

    internal static CardUpgradeCase Case<T>(
        CardType cardType,
        params UpgradeValueExpectation[] values)
        where T : EntelechiaCard, new()
        => new(typeof(T), cardType, values);

    internal static CardUpgradeCase Case<T>(
        CardType cardType,
        Action<EntelechiaCard, EntelechiaCard, List<string>> additionalChecks,
        params UpgradeValueExpectation[] values)
        where T : EntelechiaCard, new()
        => new(typeof(T), cardType, values, additionalChecks);

    internal static void Run(
        IEnumerable<CardUpgradeCase> allCases,
        string? requestedGroup,
        List<string> failures)
    {
        var concreteTypes = typeof(EntelechiaCard).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(EntelechiaCard).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

        var cases = allCases.ToArray();
        var duplicateTypes = cases.GroupBy(c => c.ModelType).Where(g => g.Count() > 1);
        foreach (var duplicate in duplicateTypes)
            failures.Add($"upgrade matrix contains duplicate card type {duplicate.Key.Name}");

        var selectedType = ParseGroup(requestedGroup, failures);
        if (requestedGroup is not null && selectedType is null)
            return;

        var selectedConcreteTypes = concreteTypes
            .Where(t => selectedType is null || ReadCardType(t, failures) == selectedType)
            .ToArray();
        var selectedCases = cases
            .Where(c => selectedType is null || c.CardType == selectedType)
            .OrderBy(c => c.ModelType.Name, StringComparer.Ordinal)
            .ToArray();

        var expectedTypes = selectedConcreteTypes.ToHashSet();
        var coveredTypes = selectedCases.Select(c => c.ModelType).ToHashSet();
        foreach (var missing in expectedTypes.Except(coveredTypes).OrderBy(t => t.Name))
            failures.Add($"upgrade matrix missing {missing.Name}");
        foreach (var unexpected in coveredTypes.Except(expectedTypes).OrderBy(t => t.Name))
            failures.Add($"upgrade matrix has unexpected {unexpected.Name}");

        foreach (var testCase in selectedCases)
            RunCase(testCase, failures);
    }

    private static void RunCase(CardUpgradeCase testCase, List<string> failures)
    {
        if (Activator.CreateInstance(testCase.ModelType) is not EntelechiaCard baseCard)
        {
            failures.Add($"{testCase.ModelType.Name}: could not construct base card");
            return;
        }

        if (baseCard.Type != testCase.CardType)
            failures.Add(
                $"{testCase.ModelType.Name}: expected CardType.{testCase.CardType}, got {baseCard.Type}");

        if (Activator.CreateInstance(testCase.ModelType) is not EntelechiaCard upgradedCard)
        {
            failures.Add($"{testCase.ModelType.Name}: could not construct upgraded card");
            return;
        }

        MakeMutableForTest(upgradedCard);
        upgradedCard.UpgradeInternal();
        upgradedCard.FinalizeUpgradeInternal();
        if (!upgradedCard.IsUpgraded)
            failures.Add($"{testCase.ModelType.Name}: real upgrade path did not set IsUpgraded");

        foreach (var value in testCase.Values)
        {
            CheckValue(testCase.ModelType.Name, $"base {value.Name}", value.Read(baseCard), value.BaseValue, failures);
            CheckValue(
                testCase.ModelType.Name,
                $"upgraded {value.Name}",
                value.Read(upgradedCard),
                value.UpgradedValue,
                failures);
        }

        testCase.AdditionalChecks?.Invoke(baseCard, upgradedCard, failures);
    }

    private static CardType ReadCardType(Type type, List<string> failures)
    {
        if (Activator.CreateInstance(type) is EntelechiaCard card)
            return card.Type;

        failures.Add($"{type.Name}: could not construct card while discovering CardType");
        return default;
    }

    private static CardType? ParseGroup(string? group, List<string> failures)
    {
        if (group is null)
            return null;

        if (Enum.TryParse<CardType>(group, ignoreCase: true, out var parsed)
            && parsed is CardType.Attack or CardType.Skill or CardType.Power)
            return parsed;

        failures.Add($"unknown upgrade probe group '{group}' (expected attack, skill, or power)");
        return null;
    }

    internal static void MakeMutableForTest(AbstractModel model)
    {
        typeof(AbstractModel).GetField(
                "<IsMutable>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(model, true);
    }

    internal static void CheckValue(
        string cardName,
        string valueName,
        object? actual,
        object? expected,
        List<string> failures)
    {
        if (Equivalent(actual, expected))
            return;

        failures.Add($"{cardName} {valueName}: expected {Display(expected)}, got {Display(actual)}");
    }

    private static bool Equivalent(object? actual, object? expected)
    {
        if (actual is null || expected is null)
            return actual is null && expected is null;

        if (IsNumeric(actual) && IsNumeric(expected))
            return Convert.ToDecimal(actual) == Convert.ToDecimal(expected);

        return Equals(actual, expected);
    }

    private static bool IsNumeric(object value)
        => Type.GetTypeCode(value.GetType()) is
            TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or
            TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal or TypeCode.Double or
            TypeCode.Single;

    private static string Display(object? value) => value?.ToString() ?? "<null>";
}
