# Entelechia Full Card Upgrade Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make all 57 Entelechia cards upgrade to their intended values and mechanics, with card text, upgrade preview, saved upgraded instances, and runtime effects reading the same upgraded state.

**Architecture:** `ConstructedCardModel.OnUpgrade()` is the single owner of every numeric upgrade. Constructor calls declare base dynamic variables only; `OnUpgrade()` applies deltas through `UpgradeValueBy`, cost changes through `EnergyCost.UpgradeBy`, and keyword changes through card keyword APIs. Runtime actions and localization read those same dynamic variables; `IsUpgraded` remains only for non-numeric branch changes that cannot be represented by a dynamic variable.

**Tech Stack:** C#/.NET 9, Godot 4.5.1, Slay the Spire 2 v0.108 APIs, BaseLib 3.3.5, Harmony, SmartFormat, JSON localization.

---

## Invariants

1. `WithDamage`, `WithBlock`, `WithCards`, and `WithPower<T>` constructor overloads define base values only for this migration. Do not put upgrade totals in their second argument.
2. Each upgraded number has exactly one owner. Never combine a constructor upgrade argument with `OnUpgrade()` for the same variable.
3. `OnPlay()` and helper properties read `DynamicVars`; no `IsUpgraded ? upgradedTotal : baseTotal` duplicate numeric source is allowed.
4. `.description` is the runtime localization key. Every changing number in it must be represented by a dynamic variable formatter or an explicit upgrade-aware formatter. `.description+` is retained only as an audit target.
5. Every concrete card must be instantiated, upgraded through `UpgradeInternal()` plus `FinalizeUpgradeInternal()`, and checked against its intended final state.
6. Existing gameplay design values come from current `zhs/cards.json` normal and upgraded descriptions unless current production code establishes a non-numeric upgrade that the descriptions already state.

## File Ownership

### Attack worker: 21 files

`BloodBlade.cs`, `BloodClawSlash.cs`, `BloodDissect.cs`, `BloodDrain.cs`, `BloodFrenzy.cs`, `BloodSplash.cs`, `BloodStorm.cs`, `BloodStrike.cs`, `BloodSurge.cs`, `BloodSweep.cs`, `CandleScorch.cs`, `CounterSlash.cs`, `CrimsonLash.cs`, `CrimsonMerge.cs`, `FarewellFinale.cs`, `Lacerate.cs`, `RedCandleAll.cs`, `RoseThorn.cs`, `RoseTrail.cs`, `SoulBloodDraw.cs`, `SpiritAndDesireFarewell.cs`.

### Skill worker: 29 files

`Autophagy.cs`, `BloodBorrow.cs`, `BloodDebtSettlement.cs`, `BloodFragrance.cs`, `BloodHaste.cs`, `BloodInfect.cs`, `Bloodletting.cs`, `BloodMend.cs`, `BloodMist.cs`, `BloodOffering.cs`, `BloodOverload.cs`, `BloodPulse.cs`, `BloodRebuild.cs`, `BloodShield.cs`, `BloodToCandle.cs`, `BloodVeil.cs`, `ClottingBackflow.cs`, `ClottingBarrier.cs`, `CrimsonEmbers.cs`, `CrimsonSacrifice.cs`, `CrimsonShield.cs`, `DiscontinuousPulse.cs`, `HeartBrand.cs`, `HeartCandleRitual.cs`, `ImmortalBloodline.cs`, `ReviveCandle.cs`, `RoseStep.cs`, `SanguineRite.cs`, `Suture.cs`.

### Power worker: 7 files

`BloodClanCourt.cs`, `BloodDemonForm.cs`, `BloodFeast.cs`, `CandleEmber.cs`, `ClotInstinct.cs`, `EternalReplete.cs`, `PainConversion.cs`.

### Main-agent-only shared files

`tools/UpgradeProbe/**`, `Entelechia/localization/zhs/cards.json`, `Entelechia/localization/eng/cards.json`, this plan, and final review records. Workers must not edit these files.

## Task 1: Build the authoritative upgrade matrix and RED probe

**Files:**
- Modify: `tools/UpgradeProbe/Program.cs`
- Create: `tools/UpgradeProbe/CardUpgradeExpectations.cs`

- [ ] Enumerate every non-abstract `EntelechiaCard` type and fail unless exactly 57 types are covered by the expectation table.
- [ ] Record expected base and upgraded values for damage, block, cards, energy, and each `PowerVar<T>` from the current card descriptions.
- [ ] Add explicit assertions for cost upgrades, exhaust removal, and custom non-numeric branches.
- [ ] Assert that runtime helper values used by attacks follow their upgraded `DamageVar`.
- [ ] Run `dotnet build tools/UpgradeProbe/UpgradeProbe.csproj -c Release --no-restore -p:BuildProjectReferences=false`.
- [ ] Run `dotnet run --project tools/UpgradeProbe/UpgradeProbe.csproj -c Release --no-build` and preserve the expected per-card failures before production edits.

## Task 2: Repair attack upgrades

**Files:**
- Modify only the 21 attack files listed under File Ownership.

- [ ] Replace constructor upgrade-total arguments with base-only declarations.
- [ ] Add or complete `OnUpgrade()` with the intended delta for every changing dynamic variable.
- [ ] Replace manual upgraded numeric branches with reads from the same `DynamicVars` used by display.
- [ ] Preserve hit counts, targets, conditional branches, HP costs, and card names.
- [ ] Self-review every attack against its normal and upgraded Chinese descriptions.

## Task 3: Repair skill upgrades

**Files:**
- Modify only the 29 skill files listed under File Ownership.

- [ ] Migrate damage-independent variables and conditional draw/heal/block calculations to one dynamic-variable source.
- [ ] Keep custom upgrade mechanics such as HP-cost reduction, exhaust removal, and percentage changes explicit and testable.
- [ ] Ensure every upgraded runtime branch matches the existing upgraded description.
- [ ] Preserve costs and keywords unless the upgraded description changes them.

## Task 4: Repair power-card upgrades

**Files:**
- Modify only the 7 power-card files listed under File Ownership.

- [ ] Verify cost upgrades use a `-1` delta exactly once.
- [ ] Represent stack amount or trigger-count upgrades through power variables or an explicit `OnUpgrade()` branch.
- [ ] Ensure played power amounts and constructed power models consume the upgraded source.
- [ ] Preserve power stacking and combat hooks.

## Task 5: Repair shared localization

**Files:**
- Modify: `Entelechia/localization/zhs/cards.json`
- Modify: `Entelechia/localization/eng/cards.json`

- [ ] Replace hard-coded changing numbers in every runtime `.description` with the correct dynamic formatter.
- [ ] Apply changes to both prefixed and unprefixed aliases.
- [ ] Keep all four active aliases byte-consistent between `zhs` and `eng` where the project currently intentionally shares Chinese text.
- [ ] Preserve the `BloodOverload` finalized-upgrade green formatter.
- [ ] Parse both JSON files and assert all 57 card IDs have title, description, and description+ entries.

## Task 6: Integration and release verification

**Files:**
- Modify: `tools/UpgradeProbe/Program.cs`
- Create: `generated_art/review/CARD_UPGRADE_FULL_FIX_20260711.md`

- [ ] Review each worker diff for file-scope compliance and single-source numeric behavior.
- [ ] Run the full upgrade probe and require zero failures.
- [ ] Run `dotnet publish .\Entelechia.csproj -c Release --tl:off` after the game process releases the installed DLL.
- [ ] Verify built and installed DLL hashes match.
- [ ] Parse the deployed PCK and verify both packaged `cards.json` files are byte-identical to source.
- [ ] Start or inspect one post-deployment game run and reject new Entelechia localization, formatter, model-registration, or upgrade exceptions.
- [ ] Record all 57 cards, expected upgraded effects, source changes, test evidence, artifact hashes, and any manual visual checks still required.

## Completion Gate

The work is complete only when the expectation table covers all 57 concrete cards, every automated assertion passes through the real BaseLib upgrade path, shared localization is packaged into the installed PCK, build artifacts match the deployment, and no required card remains listed as unverified.
