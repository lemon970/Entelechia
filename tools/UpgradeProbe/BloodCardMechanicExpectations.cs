using System.Reflection;
using System.Runtime.CompilerServices;
using BaseLib.Extensions;
using Entelechia.EntelechiaCode.Cards;

internal static class BloodCardMechanicExpectations
{
    [ModuleInitializer]
    internal static void RunWhenRequested()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("ENTELECHIA_BLOOD_CARD_PROBE"),
                "1",
                StringComparison.Ordinal))
            return;

        var failures = new List<string>();
        CheckBloodClanCourt(failures);
        CheckBloodShield(failures);

        if (failures.Count == 0)
        {
            Console.WriteLine("Blood card mechanic probe passed.");
            Environment.Exit(0);
        }

        foreach (var failure in failures)
            Console.Error.WriteLine(failure);
        Environment.Exit(1);
    }

    private static void CheckBloodClanCourt(List<string> failures)
    {
        var card = new BloodClanCourt();
        UpgradeTestHarness.CheckValue(
            nameof(BloodClanCourt),
            "base energy cost",
            card.EnergyCost.GetAmountToSpend(),
            2m,
            failures);

        Upgrade(card);
        UpgradeTestHarness.CheckValue(
            nameof(BloodClanCourt),
            "upgraded energy cost",
            card.EnergyCost.GetAmountToSpend(),
            1m,
            failures);
        UpgradeTestHarness.CheckValue(
            nameof(BloodClanCourt),
            "upgraded runtime Bloodloss amount",
            card.RuntimePowerAmount,
            2m,
            failures);

        var source = ReadCardSource(nameof(BloodClanCourt));
        CheckSourceDoesNotContain(nameof(BloodClanCourt), source, "WithCards(", failures);
        CheckSourceDoesNotContain(nameof(BloodClanCourt), source, "DrawCards(", failures);

        var harvestSource = File.ReadAllText(Path.Combine(
            Directory.GetCurrentDirectory(),
            "EntelechiaCode",
            "Powers",
            "BloodHarvestPower.cs"));
        CheckSourceContains(nameof(BloodClanCourt), harvestSource, "power is BloodClanCourtPower", failures);
        CheckSourceContains(nameof(BloodClanCourt), harvestSource, "CourtHealPerHit = 4m", failures);
        CheckSourceContains(nameof(BloodClanCourt), harvestSource, "healPerHit * MaxTriggersHealedPerCardPlay", failures);
    }

    private static void CheckBloodShield(List<string> failures)
    {
        var card = new BloodShield();
        UpgradeTestHarness.CheckValue(
            nameof(BloodShield),
            "base block",
            card.DynamicVars.Block.BaseValue,
            10m,
            failures);
        UpgradeTestHarness.CheckValue(
            nameof(BloodShield),
            "High Health keyword",
            card.Keywords.Contains(EntelechiaKeywords.HighHealth),
            true,
            failures);
        UpgradeTestHarness.CheckValue(
            nameof(BloodShield),
            "Low Health keyword",
            card.Keywords.Contains(EntelechiaKeywords.LowHealth),
            true,
            failures);

        Upgrade(card);
        UpgradeTestHarness.CheckValue(
            nameof(BloodShield),
            "upgraded block",
            card.DynamicVars.Block.BaseValue,
            13m,
            failures);

        var source = ReadCardSource(nameof(BloodShield));
        CheckSourceContains(nameof(BloodShield), source, "var highHealth = IsHighHealth();", failures);
        CheckSourceContains(nameof(BloodShield), source, "var lowHealth = IsLowHealth();", failures);
        CheckSourceContains(nameof(BloodShield), source, "await DrawCards(context, 1);", failures);
        CheckSourceContains(nameof(BloodShield), source, "await PlayerCmd.GainEnergy(1, Owner);", failures);
    }

    private static void Upgrade(EntelechiaCard card)
    {
        UpgradeTestHarness.MakeMutableForTest(card);
        card.GetType()
            .GetMethod("OnUpgrade", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(card, null);
    }

    private static string ReadCardSource(string cardName) => File.ReadAllText(Path.Combine(
        Directory.GetCurrentDirectory(),
        "EntelechiaCode",
        "Cards",
        $"{cardName}.cs"));

    private static void CheckSourceContains(
        string cardName,
        string source,
        string expected,
        List<string> failures)
    {
        if (!source.Contains(expected, StringComparison.Ordinal))
            failures.Add($"{cardName} source: expected '{expected}'");
    }

    private static void CheckSourceDoesNotContain(
        string cardName,
        string source,
        string unexpected,
        List<string> failures)
    {
        if (source.Contains(unexpected, StringComparison.Ordinal))
            failures.Add($"{cardName} source: did not expect '{unexpected}'");
    }
}
