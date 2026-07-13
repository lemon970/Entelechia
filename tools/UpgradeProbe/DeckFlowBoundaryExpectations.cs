using System.Text.RegularExpressions;

internal static class DeckFlowBoundaryExpectations
{
    internal static void Run(List<string> failures)
    {
        var entelechiaCard = ReadCardSource("EntelechiaCard", failures);
        var tryExhaust = ExtractMethodBody(
            entelechiaCard, "TryExhaustAnotherCard", "EntelechiaCard", failures);
        Require(entelechiaCard, "EntelechiaCard selector range", failures,
            "new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, 1)");
        RequireRegex(tryExhaust, "EntelechiaCard excludes itself by reference", failures,
            @"!\s*ReferenceEquals\s*\(\s*(?:card\s*,\s*this|this\s*,\s*card)\s*\)");
        RequireInOrder(tryExhaust, "TryExhaustAnotherCard flow", failures,
            "CardSelectCmd.FromHand", "selected", "null", "await CardCmd.Exhaust(context, selected", "return true");
        RequireRegex(tryExhaust, "TryExhaustAnotherCard null guard", failures,
            @"if\s*\(\s*selected\s+(?:is\s+null|==\s*null)\s*\)\s*return\s+false\s*;");
        Require(tryExhaust, "TryExhaustAnotherCard checks for candidates", failures,
            "Owner.PlayerCombatState?.Hand.Cards.Any", "if (!hasCandidate) return false;");
        RequireInOrder(tryExhaust, "TryExhaustAnotherCard skips empty selection", failures,
            "hasCandidate", "return false", "CardSelectCmd.FromHand");
        ForbidRegex(tryExhaust, "TryExhaustAnotherCard keeps same-name copies selectable", failures,
            @"\b\w*[Cc]ard\w*\s*(?:\.GetType\s*\(|\.Id\b|\bis\s+[A-Z])");

        var discontinuousPulse = ReadCardSource("DiscontinuousPulse", failures);
        Forbid(discontinuousPulse, "DiscontinuousPulse does not exhaust itself", failures,
            "CardCmd.Exhaust(context, this");
        Forbid(discontinuousPulse, "DiscontinuousPulse does not gain energy", failures,
            "GainEnergy(");
        RequireConditionalDraw(
            discontinuousPulse,
            "DiscontinuousPulse draws two only after another card is exhausted",
            2,
            failures);

        var suture = ReadCardSource("Suture", failures);
        RequireConditionalDraw(
            suture,
            "Suture draws one only after another card is exhausted",
            1,
            failures);

        var bloodInfect = ReadCardSource("BloodInfect", failures);
        Forbid(bloodInfect, "BloodInfect does not draw cards", failures, "DrawCards(");
        RequireCreateInHandOnceWithUpgradeState(bloodInfect, "BloodInfect", failures);

        var residualPulse = ReadCardSource("ResidualPulse", failures);
        var residualPulseOnPlay = ExtractMethodBody(
            residualPulse, "OnPlay", "ResidualPulse", failures);
        Require(residualPulse, "ResidualPulse is a token-pool card", failures,
            "[Pool(typeof(TokenCardPool))]", "CardRarity.Token", "CardKeyword.Exhaust");
        RequireRegex(residualPulse, "ResidualPulse health-dependent HpCost", failures,
            @"HpCost\s*=>\s*IsHighHealth\s*\(\s*\)\s*\?\s*3m\s*:\s*0m");
        RequireRegex(residualPulseOnPlay, "ResidualPulse snapshots health before actions", failures,
            @"^\s*var\s+highHealth\s*=\s*IsHighHealth\s*\(\s*\)\s*;");
        RequireInOrder(residualPulseOnPlay, "ResidualPulse upgrades block before payment", failures,
            "highHealth", "IsUpgraded", "CommonActions.CardBlock", "TryPayHpCost");
        RequireExclusiveHealthBranches(residualPulseOnPlay, failures);

        var createInHand = ExtractMethodBody(
            residualPulse, "CreateInHand", "ResidualPulse", failures);
        ForbidRegex(residualPulse, "ResidualPulse CreateInHand has no count parameter", failures,
            @"\bCreateInHand\s*\([^)]*\bcount\b[^)]*\)");
        RequireGeneratedCardFlow(createInHand, failures);

        var conduit = ReadCardSource("ResidualPulseConduit", failures);
        RequireCreateInHandOnceWithUpgradeState(conduit, "ResidualPulseConduit", failures);

        var puncture = ReadCardSource("BloodlinePuncture", failures);
        var punctureOnPlay = ExtractMethodBody(
            puncture, "OnPlay", "BloodlinePuncture", failures);
        RequireBloodlinePunctureFlow(punctureOnPlay, failures);

        var bloodMend = ReadCardSource("BloodMend", failures);
        Require(bloodMend, "BloodMend always draws its full amount", failures,
            "DrawCards(context, DynamicVars.Cards.BaseValue)");
        Forbid(bloodMend, "BloodMend no longer reduces low-health draw", failures,
            "DynamicVars.Cards.BaseValue - 1m");

        var bloodOverload = ReadCardSource("BloodOverload", failures);
        Require(bloodOverload, "BloodOverload always draws after payment", failures,
            "DrawCards(context, 1)");
        RequireInOrder(bloodOverload, "BloodOverload draw precedes conditional refund", failures,
            "Apply<BloodSpeedPower>", "DrawCards(context, 1)", "if (lowHealth)", "GainEnergy");

        var autophagy = ReadCardSource("Autophagy", failures);
        RequireInOrder(autophagy, "Autophagy pays before gaining energy", failures,
            "TryPayHpCost", "GainEnergy");
        Forbid(autophagy, "Autophagy no longer exhausts itself", failures,
            "CardCmd.Exhaust(context, this");

        var bloodOffering = ReadCardSource("BloodOffering", failures);
        RequireInOrder(bloodOffering, "BloodOffering pays before drawing", failures,
            "TryPayHpCost", "DrawCards");
        Forbid(bloodOffering, "BloodOffering no longer exhausts itself", failures,
            "CardCmd.Exhaust(context, this");

        var bloodPulse = ReadCardSource("BloodPulse", failures);
        RequireInOrder(bloodPulse, "BloodPulse pays, draws, then refunds energy", failures,
            "TryPayHpCost", "DrawCards", "GainEnergy");

        var heartBrand = ReadCardSource("HeartBrand", failures);
        RequireInOrder(heartBrand, "HeartBrand pays before applying candle", failures,
            "TryPayHpCost", "HeartCandlePower.ApplyPercent");

        var bloodStorm = ReadCardSource("BloodStorm", failures);
        RequireInOrder(bloodStorm, "BloodStorm snapshots, pays, then attacks", failures,
            "var lowHealth", "TryPayHpCost", "ExecuteCardAttack");

        var bloodletting = ReadCardSource("Bloodletting", failures);
        Require(bloodletting, "Bloodletting always draws after payment", failures,
            "DrawCards(context, 1)");
        ForbidRegex(bloodletting, "Bloodletting draw is not health-gated", failures,
            @"if\s*\(\s*!?lowHealth\s*\)\s*(?:\{\s*)?await\s+DrawCards");

        var painConversion = ReadCardSource("PainConversion", failures);
        Require(painConversion, "PainConversion always draws on play", failures,
            "DrawCards(context, 1)");
        ForbidRegex(painConversion, "PainConversion draw is not health-gated", failures,
            @"if\s*\(\s*IsHighHealth\s*\(\s*\)\s*\)\s*(?:\{\s*)?await\s+DrawCards");

        var reviveCandle = ReadCardSource("ReviveCandle", failures);
        RequireRegex(reviveCandle, "ReviveCandle draws when no revive quota exists", failures,
            @"if\s*\(\s*stacks\s*>\s*0\s*\)[\s\S]*else\s*(?:\{\s*)?await\s+DrawCards\s*\(\s*context\s*,\s*1m?\s*\)");

        var eternalReplete = ReadCardSource("EternalReplete", failures);
        Require(eternalReplete, "EternalReplete applies one power layer", failures,
            "WithPower<EternalRepletePower>(1)", "RuntimePowerAmount");
        Require(eternalReplete, "EternalReplete keeps the strongest applied heal ratio", failures,
            "Math.Max(power.HealRatio, RuntimeHealRatio)");
        Forbid(eternalReplete, "EternalReplete upgrade does not add a power layer", failures,
            "Power<EternalRepletePower>().UpgradeValueBy");

        var bloodDemonForm = ReadCardSource("BloodDemonForm", failures);
        Require(bloodDemonForm, "BloodDemonForm uses its immediate Strength dynamic value", failures,
            "DynamicVars.Power<StrengthPower>().BaseValue");

        var bloodClanCourt = ReadCardSource("BloodClanCourt", failures);
        RequireRegex(bloodClanCourt, "BloodClanCourt upgrade draw uses its Cards dynamic value", failures,
            @"if\s*\([^)]*DynamicVars\.Cards\.BaseValue[^)]*\)[\s\S]*DrawCards\s*\(\s*context\s*,\s*DynamicVars\.Cards\.BaseValue\s*\)");

        foreach (var (name, source) in new[]
                 {
                     ("BloodInfect", bloodInfect),
                     ("ResidualPulseConduit", conduit)
                 })
        {
            ForbidRegex(source, $"{name} has no two-card generation count", failures,
                @"\bcount\s*[:=]\s*2m?\b");
            ForbidRegex(source, $"{name} has no generation loop", failures,
                @"\b(for|foreach|while)\s*\(");
        }
    }

    private static string ReadCardSource(string cardName, List<string> failures)
    {
        var path = Path.Combine(
            Directory.GetCurrentDirectory(), "EntelechiaCode", "Cards", cardName + ".cs");
        if (File.Exists(path))
            return File.ReadAllText(path);

        failures.Add($"{cardName} source file missing: {path}");
        return string.Empty;
    }

    private static void Require(
        string source,
        string name,
        List<string> failures,
        params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            if (!source.Contains(fragment, StringComparison.Ordinal))
                failures.Add($"{name}: missing '{fragment}'");
        }
    }

    private static void Forbid(
        string source,
        string name,
        List<string> failures,
        string fragment)
    {
        if (source.Contains(fragment, StringComparison.Ordinal))
            failures.Add($"{name}: found forbidden '{fragment}'");
    }

    private static void RequireCount(
        string source,
        string name,
        List<string> failures,
        string fragment,
        int expected)
    {
        var actual = Regex.Matches(source, Regex.Escape(fragment)).Count;
        if (actual != expected)
            failures.Add($"{name}: expected {expected} occurrence(s), got {actual}");
    }

    private static void RequireConditionalDraw(
        string source,
        string name,
        int count,
        List<string> failures)
    {
        var pattern = $@"if\s*\(\s*await\s+TryExhaustAnotherCard\s*\(\s*context\s*\)\s*\)\s*(?:\{{\s*)?await\s+DrawCards\s*\(\s*context\s*,\s*{count}m?\s*\)";
        if (!Regex.IsMatch(source, pattern, RegexOptions.CultureInvariant))
            failures.Add($"{name}: missing guarded DrawCards(context, {count})");

        RequireCount(source, name + " draw count", failures, "DrawCards(", 1);
    }

    private static void RequireCreateInHandOnceWithUpgradeState(
        string source,
        string cardName,
        List<string> failures)
    {
        RequireCount(source, $"{cardName} creates one ResidualPulse", failures,
            "ResidualPulse.CreateInHand", 1);
        RequireRegex(source, $"{cardName} forwards upgrade state", failures,
            @"ResidualPulse\.CreateInHand\s*\([^;]*\bIsUpgraded\b[^;]*\)");
    }

    private static void RequireExclusiveHealthBranches(
        string onPlay,
        List<string> failures)
    {
        var match = Regex.Match(
            onPlay,
            @"if\s*\(\s*highHealth\s*\)\s*\{(?<high>[\s\S]*?)\}\s*else\s*\{(?<low>[\s\S]*?)\}",
            RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            failures.Add("ResidualPulse: missing explicit highHealth/else branches");
            return;
        }

        var high = match.Groups["high"].Value;
        var low = match.Groups["low"].Value;
        RequireRegex(high, "ResidualPulse high-health payment", failures,
            @"TryPayHpCost\s*\(\s*context\s*,\s*HpCost\s*,");
        Forbid(high, "ResidualPulse high-health branch does not draw", failures, "DrawCards(");
        RequireRegex(low, "ResidualPulse low-health draw", failures,
            @"DrawCards\s*\(\s*context\s*,\s*1m?\s*\)");
        Forbid(low, "ResidualPulse low-health branch does not pay HP", failures, "TryPayHpCost(");
        Forbid(low, "ResidualPulse low-health branch does not gain energy", failures, "GainEnergy(");
    }

    private static void RequireGeneratedCardFlow(string createInHand, List<string> failures)
    {
        RequireRegex(createInHand, "ResidualPulse generation combat-ending guard", failures,
            @"if\s*\(\s*CombatManager\.Instance\.IsOverOrEnding\s*\)\s*(?:\{\s*)?return\s+null\s*;(?:\s*\})?[\s\S]*combatState\.CreateCard\s*<\s*ResidualPulse\s*>\s*\(\s*owner\s*\)");
        RequireRegexCount(createInHand, "ResidualPulse creates one card", failures,
            @"combatState\.CreateCard\s*<\s*ResidualPulse\s*>\s*\(\s*owner\s*\)", 1);
        RequireCount(createInHand, "ResidualPulse registers generated cards once", failures,
            "AddGeneratedCardsToCombat", 1);
        RequireInOrder(createInHand, "ResidualPulse generated-card flow", failures,
            "CreateCard<ResidualPulse>", "if", "upgraded", "Upgrade", "AddGeneratedCardsToCombat", "Hand");
        RequireRegex(createInHand, "ResidualPulse generation targets Hand", failures,
            @"AddGeneratedCardsToCombat\s*\([^;]*\bHand\b[^;]*\)");
        ForbidRegex(createInHand, "ResidualPulse generation has no count overload", failures,
            @"\bcount\b");
        ForbidRegex(createInHand, "ResidualPulse generation has no loop", failures,
            @"\b(for|foreach|while)\s*\(");
    }

    private static void RequireBloodlinePunctureFlow(string onPlay, List<string> failures)
    {
        RequireRegex(onPlay, "BloodlinePuncture snapshots health", failures,
            @"^\s*var\s+highHealth\s*=\s*IsHighHealth\s*\(\s*\)\s*;");
        RequireRegex(onPlay, "BloodlinePuncture attack then draw then live-target guard", failures,
            @"ExecuteCardAttack[\s\S]*DrawCards[\s\S]*if\s*\([^)]*CurrentHp\s*>\s*0[^)]*\)[\s\S]*if\s*\(\s*highHealth\s*\)");
        RequireRegex(onPlay, "BloodlinePuncture chooses one debuff from locked health", failures,
            @"if\s*\(\s*highHealth\s*\)[\s\S]*Apply<BloodHarvestPower>[\s\S]*else[\s\S]*Apply<BloodlossPower>");
        RequireCount(onPlay, "BloodlinePuncture attacks once", failures, "ExecuteCardAttack", 1);
        RequireCount(onPlay, "BloodlinePuncture draws once", failures, "DrawCards(", 1);
        RequireCount(onPlay, "BloodlinePuncture applies BloodHarvest once", failures,
            "Apply<BloodHarvestPower>", 1);
        RequireCount(onPlay, "BloodlinePuncture applies Bloodloss once", failures,
            "Apply<BloodlossPower>", 1);
    }

    private static string ExtractMethodBody(
        string source,
        string methodName,
        string owner,
        List<string> failures)
    {
        var signature = Regex.Match(
            source,
            $@"\b{Regex.Escape(methodName)}\s*\([^)]*\)\s*(?:=>[^;]*;|\{{)",
            RegexOptions.CultureInvariant);
        if (!signature.Success || source[signature.Index + signature.Length - 1] != '{')
        {
            failures.Add($"{owner}: method {methodName} with block body missing");
            return string.Empty;
        }

        var openBrace = signature.Index + signature.Length - 1;
        var depth = 1;
        for (var index = openBrace + 1; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            if (source[index] != '}' || --depth != 0) continue;
            return source[(openBrace + 1)..index];
        }

        failures.Add($"{owner}: method {methodName} has no closing brace");
        return string.Empty;
    }

    private static void RequireInOrder(
        string source,
        string name,
        List<string> failures,
        params string[] fragments)
    {
        var cursor = 0;
        foreach (var fragment in fragments)
        {
            var index = source.IndexOf(fragment, cursor, StringComparison.Ordinal);
            if (index < 0)
            {
                failures.Add($"{name}: missing or out of order '{fragment}'");
                return;
            }

            cursor = index + fragment.Length;
        }
    }

    private static void ForbidRegex(
        string source,
        string name,
        List<string> failures,
        string pattern)
    {
        if (Regex.IsMatch(source, pattern, RegexOptions.CultureInvariant))
            failures.Add($"{name}: matched forbidden pattern '{pattern}'");
    }

    private static void RequireRegex(
        string source,
        string name,
        List<string> failures,
        string pattern)
    {
        if (!Regex.IsMatch(source, pattern, RegexOptions.CultureInvariant))
            failures.Add($"{name}: missing required pattern '{pattern}'");
    }

    private static void RequireRegexCount(
        string source,
        string name,
        List<string> failures,
        string pattern,
        int expected)
    {
        var actual = Regex.Matches(source, pattern, RegexOptions.CultureInvariant).Count;
        if (actual != expected)
            failures.Add($"{name}: expected {expected} occurrence(s), got {actual}");
    }
}
