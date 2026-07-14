using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using HarmonyLib;
using Entelechia.EntelechiaCode.Cards;
using Entelechia.EntelechiaCode.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

var failures = new List<string>();
var requestedGroup = args
    .FirstOrDefault(argument => argument.StartsWith("--group=", StringComparison.OrdinalIgnoreCase))?
    .Split('=', 2)[1];
const string upgradedBloodSpeedToken =
    "{IfUpgraded:cond:=1?[green]{BloodSpeedPower}[/green]|{BloodSpeedPower:diff()}}";

var locManager = (LocManager)RuntimeHelpers.GetUninitializedObject(typeof(LocManager));
AccessTools.Method(typeof(LocManager), "LoadLocFormatters")!.Invoke(locManager, null);
var smartFormatter = AccessTools.Field(typeof(LocManager), "_smartFormatter").GetValue(locManager)!;
var smartFormatMethod = smartFormatter.GetType().GetMethod(
    "Format",
    [typeof(string), typeof(object[])])!;

string FormatBloodSpeedToken(UpgradeDisplay display, decimal value)
{
    var power = new PowerVar<BloodSpeedPower>(
        display == UpgradeDisplay.UpgradePreview ? 2m : value);
    if (display == UpgradeDisplay.UpgradePreview)
        power.UpgradeValueBy(1m);

    var variables = new Dictionary<string, object>
    {
        ["IfUpgraded"] = new IfUpgradedVar(display),
        ["BloodSpeedPower"] = power
    };
    return (string)smartFormatMethod.Invoke(
        smartFormatter,
        [upgradedBloodSpeedToken, new object[] { variables }])!;
}

string FormatUpgradeConditional(string template, UpgradeDisplay display)
{
    var variables = new Dictionary<string, object>
    {
        ["IfUpgraded"] = new IfUpgradedVar(display)
    };
    return (string)smartFormatMethod.Invoke(
        smartFormatter,
        [template, new object[] { variables }])!;
}

var upgradePatchType = typeof(ConstructedCardModel).Assembly.GetType(
    "BaseLib.Patches.Utils.UpgradeInternalPatch",
    throwOnError: true)!;
new Harmony("Entelechia.UpgradeProbe")
    .CreateClassProcessor(upgradePatchType)
    .Patch();
if (!args.Contains("--without-enchantment-upgrade-patch", StringComparer.Ordinal))
{
    new Harmony("Entelechia.UpgradeProbe.EnchantmentUpgrade")
        .CreateClassProcessor(typeof(Entelechia.EntelechiaCode.EnchantmentUpgradePatch))
        .Patch();
    new Harmony("Entelechia.UpgradeProbe.EnchantmentApplied")
        .CreateClassProcessor(typeof(Entelechia.EntelechiaCode.EnchantmentAppliedPatch))
        .Patch();
}

void Check(string name, decimal actual, decimal expected)
{
    if (actual != expected)
        failures.Add($"{name}: expected {expected}, got {actual}");
}

void CheckText(string name, string actual, string expected)
{
    if (!string.Equals(actual, expected, StringComparison.Ordinal))
        failures.Add($"{name}: expected '{expected}', got '{actual}'");
}

CheckText(
    "BloodOverload normal description power",
    FormatBloodSpeedToken(UpgradeDisplay.Normal, 2m),
    "2");
CheckText(
    "BloodOverload upgraded description power",
    FormatBloodSpeedToken(UpgradeDisplay.Upgraded, 3m),
    "[green]3[/green]");
CheckText(
    "BloodOverload upgrade preview power",
    FormatBloodSpeedToken(UpgradeDisplay.UpgradePreview, 3m),
    "[green]3[/green]");
CheckText(
    "upgrade-only literal normal branch",
    FormatUpgradeConditional(
        "{IfUpgraded:cond:=1?[green]2[/green]|3}",
        UpgradeDisplay.Normal),
    "3");
CheckText(
    "upgrade-only literal upgraded branch",
    FormatUpgradeConditional(
        "{IfUpgraded:cond:=1?[green]2[/green]|3}",
        UpgradeDisplay.Upgraded),
    "[green]2[/green]");
CheckText(
    "upgrade removes text",
    FormatUpgradeConditional(
        "{IfUpgraded:cond:>0?|Exhaust.}",
        UpgradeDisplay.Upgraded),
    string.Empty);
CheckText(
    "upgrade preview removes text",
    FormatUpgradeConditional(
        "{IfUpgraded:cond:>0?|Exhaust.}",
        UpgradeDisplay.UpgradePreview),
    string.Empty);
CheckText(
    "upgrade adds text",
    FormatUpgradeConditional(
        "{IfUpgraded:cond:=0?|[green]Draw 1 card.[/green]}",
        UpgradeDisplay.UpgradePreview),
    "[green]Draw 1 card.[/green]");

void CheckDescriptionVariable(string language, string key, string expectedToken)
{
    var path = Path.Combine(
        Directory.GetCurrentDirectory(),
        "Entelechia",
        "localization",
        language,
        "cards.json");
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var description = document.RootElement.GetProperty(key).GetString() ?? string.Empty;
    if (!description.Contains(expectedToken, StringComparison.Ordinal))
        failures.Add($"{language} {key}: missing {expectedToken}");
}

foreach (var language in new[] { "zhs", "eng" })
{
    foreach (var key in new[]
             {
                 "ENTELECHIA-BLOOD_VEIL.description",
                 "BLOOD_VEIL.description"
             })
        CheckDescriptionVariable(language, key, "{Block:diff()}");

    foreach (var key in new[]
             {
                 "ENTELECHIA-BLOOD_OVERLOAD.description",
                 "BLOOD_OVERLOAD.description"
             })
        CheckDescriptionVariable(language, key, upgradedBloodSpeedToken);
}

var expectedDiscontinuousPulseZhs = new Dictionary<string, string>
{
    ["ENTELECHIA-DISCONTINUOUS_PULSE.description"] = "你可以[gold]消耗[/gold]至多 1 张其他手牌。若如此做，抽 2 张牌。无论是否如此做，移除首个拥有萃血的存活敌人的 1 层萃血，再对其施加 {IfUpgraded:cond:=1?[green]{BloodlossPower}[/green]|{BloodlossPower:diff()}} 层失血。",
    ["ENTELECHIA-DISCONTINUOUS_PULSE.description+"] = "你可以[gold]消耗[/gold]至多 1 张其他手牌。若如此做，抽 2 张牌。无论是否如此做，移除首个拥有萃血的存活敌人的 1 层萃血，再对其施加 3 层失血。",
    ["DISCONTINUOUS_PULSE.description"] = "你可以[gold]消耗[/gold]至多 1 张其他手牌。若如此做，抽 2 张牌。无论是否如此做，移除首个拥有萃血的存活敌人的 1 层萃血，再对其施加 {IfUpgraded:cond:=1?[green]{BloodlossPower}[/green]|{BloodlossPower:diff()}} 层失血。",
    ["DISCONTINUOUS_PULSE.description+"] = "你可以[gold]消耗[/gold]至多 1 张其他手牌。若如此做，抽 2 张牌。无论是否如此做，移除首个拥有萃血的存活敌人的 1 层萃血，再对其施加 3 层失血。"
};
using (var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
           Directory.GetCurrentDirectory(), "Entelechia", "localization", "zhs", "cards.json"))))
{
    foreach (var (key, expected) in expectedDiscontinuousPulseZhs)
        CheckText($"zhs {key}", document.RootElement.GetProperty(key).GetString() ?? string.Empty, expected);
}

void CheckExhaustLocalization(string language)
{
    var path = Path.Combine(
        Directory.GetCurrentDirectory(), "Entelechia", "localization", language, "cards.json");
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var root = document.RootElement;
    var automaticKeywordCards = new[]
    {
        "BLOOD_DEBT_SETTLEMENT",
        "ENTELECHIA_BLOODLETTING",
        "BLOOD_OVERLOAD",
        "BLOOD_REBUILD",
        "CRIMSON_SACRIFICE",
        "HEART_CANDLE_RITUAL",
        "IMMORTAL_BLOODLINE",
        "RESIDUAL_PULSE",
        "REVIVE_CANDLE"
    };

    foreach (var cardId in automaticKeywordCards)
    {
        foreach (var prefix in new[] { string.Empty, "ENTELECHIA-" })
        foreach (var suffix in new[] { ".description", ".description+" })
        {
            var key = prefix + cardId + suffix;
            var description = root.GetProperty(key).GetString() ?? string.Empty;
            if (description.EndsWith("消耗。", StringComparison.Ordinal)
                || description.EndsWith("Exhaust.", StringComparison.Ordinal)
                || description.Contains("{IfUpgraded:cond:>0?|消耗。}", StringComparison.Ordinal)
                || description.Contains("{IfUpgraded:cond:>0?|Exhaust.}", StringComparison.Ordinal))
            {
                failures.Add($"{language} {key}: Exhaust must come from CardKeyword.Exhaust, not handwritten text");
            }
        }
    }

    var explicitToken = language == "zhs" ? "[gold]消耗[/gold]" : "[gold]Exhaust[/gold]";
    foreach (var cardId in new[] { "DISCONTINUOUS_PULSE", "SUTURE" })
    {
        foreach (var prefix in new[] { string.Empty, "ENTELECHIA-" })
        foreach (var suffix in new[] { ".description", ".description+" })
        {
            var key = prefix + cardId + suffix;
            var description = root.GetProperty(key).GetString() ?? string.Empty;
            if (!description.Contains(explicitToken, StringComparison.Ordinal))
                failures.Add($"{language} {key}: missing explicit conditional/other-card Exhaust token {explicitToken}");
        }
    }
}

void CheckHealthTimingLocalization(string language)
{
    var path = Path.Combine(
        Directory.GetCurrentDirectory(), "Entelechia", "localization", language, "cards.json");
    using var document = JsonDocument.Parse(File.ReadAllText(path));

    foreach (var property in document.RootElement.EnumerateObject())
    {
        if (!property.Name.Contains(".description", StringComparison.Ordinal))
            continue;

        var description = property.Value.GetString() ?? string.Empty;
        foreach (var forbidden in new[]
                 {
                     "按打出时的血量",
                     "若打出时生命不高于",
                     "Based on your Health when played",
                     "If played at 50% HP or less"
                 })
        {
            if (description.Contains(forbidden, StringComparison.Ordinal))
                failures.Add($"{language} {property.Name}: obsolete health timing phrase '{forbidden}'");
        }
    }
}

CheckExhaustLocalization("zhs");
CheckExhaustLocalization("eng");
CheckHealthTimingLocalization("zhs");
CheckHealthTimingLocalization("eng");

void CheckCardLocalizationAliases(string language)
{
    var path = Path.Combine(
        Directory.GetCurrentDirectory(),
        "Entelechia",
        "localization",
        language,
        "cards.json");
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var root = document.RootElement;
    var cardTypes = typeof(EntelechiaCard).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(EntelechiaCard).IsAssignableFrom(type))
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToArray();

    Check($"{language} concrete card localization count", cardTypes.Length, 60m);
    foreach (var cardType in cardTypes)
    {
        if (Activator.CreateInstance(cardType) is not EntelechiaCard card)
        {
            failures.Add($"{language} {cardType.Name}: could not construct card for localization audit");
            continue;
        }

        var rawId = card.Id.Entry;
        var separator = rawId.IndexOf('-', StringComparison.Ordinal);
        var unprefixedId = separator >= 0 ? rawId[(separator + 1)..] : rawId;
        var prefixedId = "ENTELECHIA-" + unprefixedId;
        foreach (var suffix in new[] { ".title", ".description", ".description+" })
        {
            var prefixedKey = prefixedId + suffix;
            var unprefixedKey = unprefixedId + suffix;
            if (!root.TryGetProperty(prefixedKey, out var prefixedValue))
            {
                failures.Add($"{language} localization missing {prefixedKey}");
                continue;
            }

            if (!root.TryGetProperty(unprefixedKey, out var unprefixedValue))
            {
                failures.Add($"{language} localization missing {unprefixedKey}");
                continue;
            }

            CheckText(
                $"{language} localization alias {unprefixedKey}",
                unprefixedValue.GetString() ?? string.Empty,
                prefixedValue.GetString() ?? string.Empty);
        }
    }
}

CheckCardLocalizationAliases("zhs");
CheckCardLocalizationAliases("eng");

void CheckCardRarityDistribution()
{
    var counts = typeof(EntelechiaCard).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(EntelechiaCard).IsAssignableFrom(type))
        .Select(type => (EntelechiaCard)Activator.CreateInstance(type)!)
        .GroupBy(card => card.Rarity.ToString())
        .ToDictionary(group => group.Key, group => (decimal)group.Count(), StringComparer.Ordinal);

    foreach (var (rarity, expected) in new Dictionary<string, decimal>
             {
                 ["Basic"] = 4m,
                 ["Common"] = 17m,
                 ["Uncommon"] = 26m,
                 ["Rare"] = 12m,
                 ["Token"] = 1m
             })
    {
        Check($"card rarity {rarity}", counts.GetValueOrDefault(rarity), expected);
    }
}

CheckCardRarityDistribution();

void CheckTemporaryStrengthLocalizationAliases(string language)
{
    var path = Path.Combine(
        Directory.GetCurrentDirectory(),
        "Entelechia",
        "localization",
        language,
        "powers.json");
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var root = document.RootElement;

    foreach (var id in new[]
             {
                 "BLOODLETTING_STRENGTH_POWER",
                 "BLOOD_DEBT_STRENGTH_POWER"
             })
    {
        foreach (var suffix in new[] { ".title", ".description", ".smartDescription" })
        {
            var prefixedKey = "ENTELECHIA-" + id + suffix;
            var unprefixedKey = id + suffix;
            if (!root.TryGetProperty(prefixedKey, out var prefixedValue))
            {
                failures.Add($"{language} localization missing {prefixedKey}");
                continue;
            }

            if (!root.TryGetProperty(unprefixedKey, out var unprefixedValue))
            {
                failures.Add($"{language} localization missing {unprefixedKey}");
                continue;
            }

            CheckText(
                $"{language} temporary strength alias {unprefixedKey}",
                unprefixedValue.GetString() ?? string.Empty,
                prefixedValue.GetString() ?? string.Empty);
        }
    }
}

CheckTemporaryStrengthLocalizationAliases("zhs");
CheckTemporaryStrengthLocalizationAliases("eng");

void CheckTemporaryStrengthPolarity(Type powerType)
{
    var power = RuntimeHelpers.GetUninitializedObject(powerType);
    var property = typeof(TemporaryStrengthPower).GetProperty(
        "IsPositive",
        BindingFlags.Instance | BindingFlags.NonPublic)!;
    if (property.GetValue(power) is not true)
        failures.Add($"{powerType.Name}: expected positive temporary Strength power");
}

CheckTemporaryStrengthPolarity(typeof(BloodlettingStrengthPower));
CheckTemporaryStrengthPolarity(typeof(BloodDebtStrengthPower));

string FormatCardDescription(
    string template,
    EntelechiaCard card,
    UpgradeDisplay display)
{
    var variables = new Dictionary<string, object>
    {
        ["IfUpgraded"] = new IfUpgradedVar(display)
    };
    foreach (var variable in card.DynamicVars)
        variables[variable.Key] = variable.Value;

    return (string)smartFormatMethod.Invoke(
        smartFormatter,
        [template, new object[] { variables }])!;
}

static string StripGreenTags(string value) => value
    .Replace("[green]", string.Empty, StringComparison.Ordinal)
    .Replace("[/green]", string.Empty, StringComparison.Ordinal);

static bool AllUpgradeChangesAreGreen(
    string normal,
    string renderedUpgrade,
    out string reason)
{
    const string openTag = "[green]";
    const string closeTag = "[/green]";
    var unchangedSegments = new List<string>();
    var cursor = 0;
    var greenSpanCount = 0;

    while (true)
    {
        var open = renderedUpgrade.IndexOf(openTag, cursor, StringComparison.Ordinal);
        if (open < 0)
        {
            unchangedSegments.Add(renderedUpgrade[cursor..]);
            break;
        }

        unchangedSegments.Add(renderedUpgrade[cursor..open]);
        var contentStart = open + openTag.Length;
        var close = renderedUpgrade.IndexOf(closeTag, contentStart, StringComparison.Ordinal);
        if (close < 0)
        {
            reason = "contains an unmatched [green] tag";
            return false;
        }

        if (close == contentStart)
        {
            reason = "contains an empty green span";
            return false;
        }

        greenSpanCount++;
        cursor = close + closeTag.Length;
    }

    if (greenSpanCount == 0)
    {
        reason = "contains no green span";
        return false;
    }

    if (!normal.StartsWith(unchangedSegments[0], StringComparison.Ordinal))
    {
        reason = "changes uncolored text at the start";
        return false;
    }

    cursor = unchangedSegments[0].Length;
    for (var index = 1; index < unchangedSegments.Count - 1; index++)
    {
        var segment = unchangedSegments[index];
        var found = normal.IndexOf(segment, cursor, StringComparison.Ordinal);
        if (found < 0)
        {
            reason = $"contains an uncolored changed segment '{segment}'";
            return false;
        }

        cursor = found + segment.Length;
    }

    var suffix = unchangedSegments[^1];
    var suffixStart = normal.LastIndexOf(suffix, StringComparison.Ordinal);
    if (suffixStart < cursor || suffixStart + suffix.Length != normal.Length)
    {
        reason = "changes uncolored text at the end";
        return false;
    }

    reason = string.Empty;
    return true;
}

void CheckRenderedUpgradeDescriptions(string language)
{
    var path = Path.Combine(
        Directory.GetCurrentDirectory(),
        "Entelechia",
        "localization",
        language,
        "cards.json");
    using var document = JsonDocument.Parse(File.ReadAllText(path));
    var root = document.RootElement;
    var cardTypes = typeof(EntelechiaCard).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(EntelechiaCard).IsAssignableFrom(type))
        .OrderBy(type => type.Name, StringComparer.Ordinal);

    foreach (var cardType in cardTypes)
    {
        var baseCard = (EntelechiaCard)Activator.CreateInstance(cardType)!;
        var upgradedCard = (EntelechiaCard)Activator.CreateInstance(cardType)!;
        var previewCard = (EntelechiaCard)Activator.CreateInstance(cardType)!;

        UpgradeTestHarness.MakeMutableForTest(upgradedCard);
        upgradedCard.UpgradeInternal();
        upgradedCard.FinalizeUpgradeInternal();

        UpgradeTestHarness.MakeMutableForTest(previewCard);
        previewCard.UpgradeInternal();

        var rawId = baseCard.Id.Entry;
        var separator = rawId.IndexOf('-', StringComparison.Ordinal);
        var unprefixedId = separator >= 0 ? rawId[(separator + 1)..] : rawId;
        var prefixedId = "ENTELECHIA-" + unprefixedId;
        foreach (var id in new[] { prefixedId, unprefixedId })
        {
            var template = root.GetProperty(id + ".description").GetString() ?? string.Empty;
            var expected = root.GetProperty(id + ".description+").GetString() ?? string.Empty;
            var normal = FormatCardDescription(template, baseCard, UpgradeDisplay.Normal);
            var upgraded = FormatCardDescription(template, upgradedCard, UpgradeDisplay.Upgraded);
            var preview = FormatCardDescription(template, previewCard, UpgradeDisplay.UpgradePreview);

            CheckText(
                $"{language} {id} finalized upgrade description",
                StripGreenTags(upgraded),
                expected);
            CheckText(
                $"{language} {id} upgrade preview description",
                StripGreenTags(preview),
                expected);

            var isRemovalOnlyUpgrade = unprefixedId is
                "BLOOD_DEBT_SETTLEMENT" or "CRIMSON_SACRIFICE" or "IMMORTAL_BLOODLINE";
            if (!isRemovalOnlyUpgrade
                && !string.Equals(normal, expected, StringComparison.Ordinal))
            {
                if (!AllUpgradeChangesAreGreen(normal, upgraded, out var upgradedReason))
                    failures.Add(
                        $"{language} {id} finalized upgrade green coverage: {upgradedReason}");
                if (!AllUpgradeChangesAreGreen(normal, preview, out var previewReason))
                    failures.Add(
                        $"{language} {id} upgrade preview green coverage: {previewReason}");
            }
        }
    }
}

CheckRenderedUpgradeDescriptions("zhs");
CheckRenderedUpgradeDescriptions("eng");

DeckFlowBoundaryExpectations.Run(failures);

EnchantmentUpgradeExpectations.Run(failures);

Sts2CompatibilityExpectations.Run(failures);

UpgradeTestHarness.Run(
    AttackUpgradeExpectations.Cases
        .Concat(SkillUpgradeExpectations.Cases)
        .Concat(PowerUpgradeExpectations.Cases),
    requestedGroup,
    failures);

if (failures.Count == 0)
{
    Console.WriteLine("Upgrade probe passed.");
    return 0;
}

Console.Error.WriteLine("Upgrade probe failed:");
foreach (var failure in failures)
    Console.Error.WriteLine($"- {failure}");
return 1;
