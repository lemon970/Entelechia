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
        Require(suture, "Suture optionally exhausts at most one other card", failures,
            "TryExhaustAnotherCard(context)");
        Forbid(suture, "Suture no longer draws cards", failures, "DrawCards(");

        var bloodInfect = ReadCardSource("BloodInfect", failures);
        Forbid(bloodInfect, "BloodInfect does not draw cards", failures, "DrawCards(");
        Forbid(bloodInfect, "BloodInfect no longer generates ResidualPulse", failures,
            "ResidualPulse.CreateInHand");
        Require(bloodInfect, "BloodInfect applies both debuffs", failures,
            "Apply<BloodlossPower>", "Apply<BloodHarvestPower>");

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

        Forbid(residualPulse, "ResidualPulse has no production helper", failures,
            "CreateInHand");

        var conduit = ReadCardSource("ResidualPulseConduit", failures);
        Require(conduit, "ResidualPulseConduit is a stacking Power card", failures,
            "CardType.Power", "WithPower<ResidualPulseConduitPower>(1)",
            "EnergyCost.UpgradeBy(-1)", "Apply<ResidualPulseConduitPower>");
        Forbid(conduit, "ResidualPulseConduit does not generate ResidualPulse", failures,
            "ResidualPulse.CreateInHand");

        var conduitPower = ReadSource("Powers", "ResidualPulseConduitPower", failures);
        Require(conduitPower, "ResidualPulseConduitPower stacks and grants two triggers per layer", failures,
            "PowerStackType.Counter", "TriggersPerStack = 2",
            "Amount <= 0m", "(int)Amount * TriggersPerStack");
        RequireInOrder(conduitPower, "ResidualPulseConduitPower locks the health branch at trigger time", failures,
            "TriggerCountThisTurn++", "Owner.CurrentHp > Owner.MaxHp * 0.5m", "Draw", "GainEnergy", "GainBlock");
        Require(conduitPower, "ResidualPulseConduitPower only responds to card HP loss", failures,
            "props != DamageProps.cardHpLoss", "cardSource?.Owner?.Creature != Owner");

        var replete = ReadSource("Relics", "BloodDemonReplete", failures);
        Require(replete, "BloodDemonReplete persists its floor quota", failures,
            "[SavedProperty]", "LastRevivedFloor", "Owner?.RunState?.TotalFloor ?? -1");
        Require(replete, "BloodDemonReplete grants immediate revive rewards", failures,
            "CardPileCmd.Draw(context, 2m", "PlayerCmd.GainEnergy(2m", "Apply<BloodHarvestPower>");
        Require(replete, "BloodDemonReplete reduces attack damage for the revived floor", failures,
            "LastRevivedFloor != currentFloor", "target != Owner?.Creature", "DamageProps.monsterMove", "0.75m");

        var puncture = ReadCardSource("BloodlinePuncture", failures);
        var punctureOnPlay = ExtractMethodBody(
            puncture, "OnPlay", "BloodlinePuncture", failures);
        RequireBloodlinePunctureFlow(punctureOnPlay, failures);

        var counterSlash = ReadCardSource("CounterSlash", failures);
        Require(counterSlash, "CounterSlash reads only its owner's HP-loss state", failures,
            "TurnStateTracker.LostHpThisTurnFor(Owner.Creature)");
        RequireRegex(counterSlash, "CounterSlash uses independent low-health and HP-loss branches", failures,
            @"if\s*\(\s*IsLowHealth\s*\(\s*\)\s*\)[\s\S]*if\s*\(\s*TurnStateTracker\.LostHpThisTurnFor");
        Forbid(counterSlash, "CounterSlash does not make its rewards mutually exclusive", failures,
            "else if", "else\n");

        var bloodStrike = ReadCardSource("BloodStrike", failures);
        Require(bloodStrike, "BloodStrike adds high-health draw and low-health kill sustain", failures,
            "IsHighHealth() && target.CurrentHp > 0",
            "DrawCards(context, DynamicVars.Cards.BaseValue)",
            "IsLowHealth() && target.CurrentHp <= 0",
            "HealTracking(Owner.Creature, DynamicVars.Heal.BaseValue");

        var bloodDissect = ReadCardSource("BloodDissect", failures);
        Require(bloodDissect, "BloodDissect removes up to three stacks and rewards removing all three", failures,
            "Math.Min(3, (int)harvest.Amount)",
            "if (toRemove == 3)",
            "GainEnergy(1, Owner)");

        var bloodToCandle = ReadCardSource("BloodToCandle", failures);
        RequireInOrder(bloodToCandle, "BloodToCandle applies one candle for each pair removed", failures,
            "while (removedGroups < 4)",
            "ModifyAmount(context, bloodloss, -2",
            "HeartCandlePower.ApplyPercent",
            "removedGroups++");

        var clottingBackflow = ReadCardSource("ClottingBackflow", failures);
        Require(clottingBackflow, "ClottingBackflow uses its upgraded removal limit in both health states", failures,
            "var removalLimit = (int)DynamicVars.Cards.BaseValue");
        Forbid(clottingBackflow, "ClottingBackflow no longer exhausts or advertises Exhaust", failures,
            "WithTip(CardKeyword.Exhaust)", "CardCmd.Exhaust(context, this", "CardKeyword.Exhaust");
        Forbid(clottingBackflow, "ClottingBackflow does not hard-code a low-health removal cap", failures,
            "lowHealth ? 2");

        foreach (var cardName in new[]
                 {
                     "BloodDebtSettlement",
                     "Bloodletting",
                     "BloodRebuild",
                     "HeartCandleRitual",
                     "ImmortalBloodline",
                     "ReviveCandle"
                 })
        {
            var source = ReadCardSource(cardName, failures);
            Require(source, $"{cardName} declares standard Exhaust metadata", failures,
                "WithKeyword(CardKeyword.Exhaust");
            Forbid(source, $"{cardName} does not manually Exhaust itself", failures,
                "CardCmd.Exhaust(context, this");
        }

        var bloodMend = ReadCardSource("BloodMend", failures);
        RequireInOrder(bloodMend, "BloodMend locks health, heals low, then draws", failures,
            "var lowHealth", "HealTracking", "var cards", "DrawCards(context, lowHealth ? cards - 1m : cards)");

        var bloodOverload = ReadCardSource("BloodOverload", failures);
        RequireInOrder(bloodOverload, "BloodOverload locks health before payment", failures,
            "var lowHealth", "TryPayHpCost", "Apply<BloodSpeedPower>", "if (lowHealth)");
        Require(bloodOverload, "BloodOverload bridges both health states", failures,
            "GainEnergy(1, Owner)", "DrawCards(context, 1)");

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
        Require(heartBrand, "HeartBrand grants one Energy for a target without Heart Candle", failures,
            "if (!hadCandle)", "GainEnergy(1, Owner)");
        Forbid(heartBrand, "HeartBrand no longer refunds the actual card cost", failures,
            "EnergySpent", "refund");

        var immortalBloodline = ReadCardSource("ImmortalBloodline", failures);
        RequireInOrder(immortalBloodline, "ImmortalBloodline checks current Health after applying its power", failures,
            "Apply<ImmortalBloodlinePower>", "if (IsLowHealth())", "GainEnergy(1, Owner)");
        Forbid(immortalBloodline, "ImmortalBloodline does not snapshot an Energy refund flag", failures,
            "var refundEnergy");

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
        Require(bloodDemonForm, "BloodDemonForm exposes and uses all displayed dynamic values", failures,
            "DynamicVars.Power<StrengthPower>().BaseValue",
            "WithPower<HeartCandlePower>",
            "WithPower<BloodSpeedPower>",
            "WithPower<BloodDemonFormPower>(\"MultiplierBonus\"");
        Forbid(bloodDemonForm, "BloodDemonForm no longer exposes per-turn Strength", failures,
            "TurnStrength");

        var bloodDemonFormPower = ReadSource("Powers", "BloodDemonFormPower", failures);
        Require(bloodDemonFormPower, "BloodDemonFormPower scales every per-stack effect with Amount", failures,
            "PowerStackType.Counter",
            "Amount * BloodSpeedPerStack");
        Require(bloodDemonFormPower, "BloodDemonFormPower applies the accumulated Heart Candle percentage", failures,
            "[SavedProperty]",
            "HeartCandlePercent { get; set; }",
            "HeartCandlePower.ApplyPercent(choiceContext, target, null, HeartCandlePercent");
        Forbid(bloodDemonFormPower, "BloodDemonFormPower no longer grants Strength each turn", failures,
            "Apply<StrengthPower>", "StrengthPerStack");
        Require(bloodDemonForm, "BloodDemonForm accumulates ten or twelve percent per played copy", failures,
            "WithPower<HeartCandlePower>(10)",
            "DynamicVars.Power<HeartCandlePower>().UpgradeValueBy(2)",
            "form.HeartCandlePercent += DynamicVars.Power<HeartCandlePower>().BaseValue");

        var heartCandlePower = ReadSource("Powers", "HeartCandlePower", failures);
        Require(heartCandlePower, "HeartCandlePower reads the shared per-stack Blood Demon multiplier", failures,
            "formStacks * BloodDemonFormPower.HeartCandleMultiplierBonusPerStack");

        var bloodClanCourt = ReadCardSource("BloodClanCourt", failures);
        Require(bloodClanCourt, "BloodClanCourt upgrade reduces energy cost", failures,
            "EnergyCost.UpgradeBy(-1)");
        Forbid(bloodClanCourt, "BloodClanCourt no longer draws on play", failures,
            "DrawCards(");
        Forbid(bloodClanCourt, "BloodClanCourt no longer owns a Cards dynamic value", failures,
            "WithCards(");

        ForbidRegex(bloodInfect, "BloodInfect has no generation loop", failures,
            @"\b(for|foreach|while)\s*\(");
    }

    private static string ReadCardSource(string cardName, List<string> failures)
        => ReadSource("Cards", cardName, failures);

    private static string ReadSource(string directory, string typeName, List<string> failures)
    {
        var path = Path.Combine(
            Directory.GetCurrentDirectory(), "EntelechiaCode", directory, typeName + ".cs");
        if (File.Exists(path))
            return File.ReadAllText(path);

        failures.Add($"{typeName} source file missing: {path}");
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
        params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            if (source.Contains(fragment, StringComparison.Ordinal))
                failures.Add($"{name}: found forbidden '{fragment}'");
        }
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
