# Entelechia Full Card Balance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement and verify the first strengthening pass for high-health draw/energy and low-health sustain/burst across Entelechia's 57-card pool.

**Architecture:** Extend sim2 before changing cards so the harness can measure player health, incoming damage, block, healing, draw, energy, play count, death, and play-cap hits. Add one shared 50% health-state helper to `EntelechiaCard`, then implement the eight confirmed card buffs in two independently verifiable batches. Preserve card names, IDs, art, heart-candle values, and existing combat animation hooks.

**Tech Stack:** C#/.NET 9, STS2/BaseLib APIs, Harmony-backed sim2 harness, JSON localization, CodeGraph.

---

## Working Tree Rule

The repository already contains extensive user changes and untracked Mod files. Do not reset, clean, or create mixed commits. Each task ends with targeted `git diff --check`, builds, and tests instead of a commit.

### Task 1: Add player starting health to sim2 harness

**Files:**
- Modify: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/Harness.cs:192-285`
- Test: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaSmokeTests.cs`

- [ ] **Step 1: Write a failing harness test**

Add a smoke test that starts Entelechia at 25 HP and asserts the combat creature begins at exactly 25 HP:

```csharp
private static Task<TestResult> Test_Harness_PlayerStartingHp()
{
    var h = Harness.BeginCombat(
        EntelechiaCharacter(),
        Harness.AsEntries(new[] { C("BloodMend") }),
        playerHp: 25);
    try
    {
        return Task.FromResult(h.Player.Creature.CurrentHp == 25
            ? Pass("Harness player starting HP")
            : Fail("Harness player starting HP", $"expected 25, got {h.Player.Creature.CurrentHp}"));
    }
    finally { Harness.EndCombat(); }
}
```

- [ ] **Step 2: Run the smoke suite and confirm compile failure**

Run:

```powershell
dotnet run --project D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/StS2Sim.csproj -c Release -- hidden-tests
```

Expected: compile failure because `playerHp` is not yet a `BeginCombat` parameter.

- [ ] **Step 3: Extend all BeginCombat overloads**

Add `decimal? playerHp = null` after `dummyHp`, propagate it through the runtime reflection invocation, and set the player's current HP after `PopulateCombatState`:

```csharp
if (playerHp is { } requestedHp)
{
    var clampedHp = Math.Clamp(requestedHp, 1m, player.Creature.MaxHp);
    Reflect.SetCurrentHp(player.Creature, clampedHp);
}
```

Update the `BeginCombatGeneric.Invoke` argument array to include the new parameter.

- [ ] **Step 4: Run the smoke suite**

Expected: the new test passes and all existing hidden tests still pass.

### Task 2: Add survivability and loop metrics to DamagePerTurnSim

**Files:**
- Modify: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/DamagePerTurnSim.cs`
- Modify: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/PlayCapture.cs`
- Test: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaSmokeTests.cs`

- [ ] **Step 1: Add failing metric tests**

Add tests for these exact cases:

```csharp
// 25 starting HP is visible in TurnResult.PlayerHpBefore.
// Playing BloodOffering records at least 2 draws, 1 play, and positive self HP loss.
// A 12-damage enemy hit reduces block before HP.
// A forced zero-cost cycle reports HitPlayCap instead of silently stopping.
```

- [ ] **Step 2: Extend sim inputs**

Add:

```csharp
public decimal? StartingPlayerHp { get; init; }
public int IncomingDamagePerTurn { get; init; }
public int MaxPlaysPerTurn { get; init; } = 100;
```

Pass `StartingPlayerHp` to `Harness.BeginCombat`.

- [ ] **Step 3: Extend TurnResult and TrialResult**

Use these fields:

```csharp
public sealed record TurnResult(
    int Turn,
    int Damage,
    int EnergySpent,
    decimal PlayerHpBefore,
    decimal PlayerHpAfterCards,
    decimal PlayerHpAfterEnemy,
    decimal SelfHpLost,
    decimal EffectiveHealing,
    decimal BlockBeforeEnemy,
    decimal IncomingDamageTaken,
    int CardsDrawn,
    int CardsPlayed,
    int EnergyGained,
    bool PlayerDied,
    bool HitPlayCap,
    decimal HeartCandleGenerated,
    decimal HeartCandleConsumed,
    decimal HeartCandleRemaining,
    IReadOnlyList<PlayCapture.Event> Events);
```

Add trial aggregates for final/minimum HP, death, draw/play counts, healing, block, self loss, energy gained, and cap hits.

- [ ] **Step 4: Return play-phase status instead of only energy spent**

Replace the integer return with:

```csharp
private sealed record PlayPhaseResult(int EnergySpent, int Plays, bool HitCap);
```

Use `plays < MaxPlaysPerTurn`; set `HitCap` only when the loop stopped because the limit was reached while a playable card remained.

- [ ] **Step 5: Apply fixed enemy damage through CreatureCmd.Damage**

After the player end-turn hooks, capture block and HP, then invoke the seven-parameter `CreatureCmd.Damage` overload with `DamageProps.monsterMove`, `harness.Dummy` as dealer, and the player creature as target. Do not use `Reflect.SetCurrentHp` for incoming damage.

- [ ] **Step 6: Derive draw and play counts from PlayCapture**

Count `EventKind.Draw` and `EventKind.Play`; do not add a second event system.

- [ ] **Step 7: Run hidden tests and both builds**

Expected: all hidden tests pass; Mod and sim2 report `0 warning / 0 error`.

### Task 3: Add a full-card balance command

**Files:**
- Create: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaFullBalance.cs`
- Modify: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/Program.cs`

- [ ] **Step 1: Register `hidden-full-balance`**

Add a command branch that calls `EntelechiaFullBalance.Run()`.

- [ ] **Step 2: Implement the matrix**

Use `40` seeds, `5` turns, starting HP `50, 26, 25, 12`, and incoming damage `0, 8, 12, 16`. Include starter, high-health startup, low-health sustain, blood-speed multi-hit, blood-harvest rhythm, energy cycle, heart-candle burst, and farewell engine decks.

Print:

```text
Deck HP In Dmg Block Heal Self FinalHP Death Draw Play GainE Cap%
```

Run paired same-seed baselines for every changed card.

- [ ] **Step 3: Capture the pre-change baseline**

Run:

```powershell
dotnet run --project D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/StS2Sim.csproj -c Release -- hidden-full-balance
```

Save console output in the final results document before changing card code.

### Task 4: Add shared health-state helpers and first resource/sustain cards

**Files:**
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/EntelechiaCard.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/BloodMend.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/Autophagy.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/BloodRebuild.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/CrimsonEmbers.cs`
- Modify: `D:/desktop/mod/Entelechia/Entelechia/localization/eng/cards.json`
- Test: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaSmokeTests.cs`

- [ ] **Step 1: Add failing card tests**

Test BloodMend at 26 and 25 HP, Autophagy energy/HP, BloodRebuild at 50/26/25/12 HP, and CrimsonEmbers at low HP without prior self loss.

- [ ] **Step 2: Add shared helpers**

```csharp
protected bool IsLowHealth()
{
    var creature = Owner?.Creature;
    return creature != null && creature.CurrentHp <= creature.MaxHp / 2m;
}

protected bool IsHighHealth() => !IsLowHealth();
```

Only call these helpers during combat when `Owner.Creature` exists.

- [ ] **Step 3: Implement BloodMend**

Snapshot `lowHealth` before healing. Low health heals `5/7` then draws `1/2`; high health draws `2/3`.

- [ ] **Step 4: Implement Autophagy**

Pay `3/2` HP first, exhaust after successful payment, then gain `2` energy.

- [ ] **Step 5: Implement BloodRebuild**

Snapshot current HP and half HP. High health sets HP to half, gains `3` energy, and draws `1`. Low/equal health sets HP to half, applies `3` BloodHarvest to every living enemy, and gains `8` block. Exhaust after effects.

- [ ] **Step 6: Implement CrimsonEmbers**

Snapshot low health before healing. Grant block when the snapshot was low or `TurnStateTracker.LostHpThisTurn` is true.

- [ ] **Step 7: Update both prefixed and fallback localization keys**

Update base and upgraded descriptions for the four cards without changing titles or IDs.

- [ ] **Step 8: Build and run targeted tests**

Expected: all new behavior tests and existing hidden tests pass.

### Task 5: Implement direct attack and engine buffs

**Files:**
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/BloodStorm.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/BloodClanCourt.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/FarewellFinale.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/EternalReplete.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/BloodDemonForm.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/ImmortalBloodline.cs`
- Modify: `D:/desktop/mod/Entelechia/EntelechiaCode/Cards/BloodFrenzy.cs`
- Modify: `D:/desktop/mod/Entelechia/Entelechia/localization/eng/cards.json`
- Test: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaSmokeTests.cs`

- [ ] **Step 1: Add failing tests**

Assert BloodStorm is `16/20` with `2/3` Bloodloss; BloodClanCourt immediately applies 2 Bloodloss to all living enemies; BloodDemonForm immediately grants 1 Strength; FarewellFinale attacks `1/2/3/3` times for `0/1/2/3` status types; EternalReplete remains 2 cost and has high/low-health immediate value; ImmortalBloodline and BloodFrenzy refund 1 energy only at low health.

- [ ] **Step 2: Implement BloodStorm**

Set `BaseDamage`, `WithDamage`, and Bloodloss amounts to `16/20` and `2/3`.

- [ ] **Step 3: Implement BloodClanCourt immediate value**

Before applying `BloodClanCourtPower`, iterate living enemies and apply 2 Bloodloss.

- [ ] **Step 4: Implement BloodDemonForm immediate value**

Before applying the existing form power, immediately gain `1` Strength. Do not change any HeartCandle amount or multiplier.

- [ ] **Step 5: Implement FarewellFinale status scaling**

Use `1 + min(statusTypeCount, 2)` total attacks. No status attacks once, one status twice, and two or three statuses three times. Keep cost `2` and per-hit damage `7/9`.

- [ ] **Step 6: Implement EternalReplete dual-state immediate value**

Keep cost `2`. High health draws `2`; low/equal health applies `2` BloodHarvest to every living enemy and grants `1` BloodSpeed. Preserve the existing EternalReplete power values.

- [ ] **Step 7: Implement low-health refunds**

After ImmortalBloodline or BloodFrenzy resolves, refund `1` energy only when the card began at or below 50% HP. Each card can refund at most once per play.

- [ ] **Step 8: Update localization and run tests**

Update both prefixed and fallback localization keys. Run the targeted matrix, all hidden tests, and the full-balance command; every play-cap rate must remain `0%`.

- [ ] **Step 6: Update localization and run tests**

Expected: exact descriptions match implementation and all tests pass.

### Task 6: Run full first-pass verification

**Files:**
- Modify: `D:/desktop/mod/Entelechia/docs/superpowers/results/2026-07-10-entelechia-full-card-balance-results.md`

- [ ] **Step 1: Build both projects**

```powershell
dotnet build D:/desktop/mod/Entelechia/Entelechia.csproj -c Release
dotnet build D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/StS2Sim.csproj -c Release
```

Expected: both `0 warning / 0 error`.

- [ ] **Step 2: Run smoke and regression suites**

```powershell
dotnet run --project D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/StS2Sim.csproj -c Release -- hidden-tests
dotnet run --project D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/StS2Sim.csproj -c Release -- hidden-archetypes
dotnet run --project D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/StS2Sim.csproj -c Release -- hidden-balance
dotnet run --project D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/StS2Sim.csproj -c Release -- hidden-full-balance
```

- [ ] **Step 3: Check acceptance gates**

Verify starter `>=18.77`, blood-harvest rhythm `>=15.88`, blood-speed multi-hit `>=20.43`, energy cycle `>=22.90`, heart-candle burst no more than 5% below `49.47`, farewell engine above `45.73`, and lower low-health death rate or higher final HP under 12 incoming damage.

- [ ] **Step 4: Record exact output**

Write build counts, test counts, every archetype row, survival rows, cap-hit percentages, and any deferred second-round candidates to the results document.

- [ ] **Step 5: Run final hygiene checks**

```powershell
git -C D:/desktop/mod diff --check
git -C D:/desktop/mod status --short
```

Do not clean or revert unrelated files.

### Task 7: Decide second-round conditional buffs from evidence

**Files:**
- Reference: `D:/desktop/mod/Entelechia/docs/superpowers/specs/2026-07-10-entelechia-full-card-balance-design.md`
- Reference: `D:/desktop/mod/Entelechia/docs/superpowers/results/2026-07-10-entelechia-full-card-balance-results.md`

- [ ] **Step 1: Evaluate BloodBorrow, BloodPulse, CounterSlash, Suture, BloodDissect, BloodFeast, and ImmortalBloodline**

Use paired same-seed results at 26 and 25 HP.

- [ ] **Step 2: Reject accidental cheap loops**

A starter or two-card common/uncommon package must have `Cap% = 0`. Multi-component rare pseudo-infinite engines may be retained only when their components, upgrades, HP cost, and stability are documented.

- [ ] **Step 3: Write a second implementation plan**

Only include candidates whose marginal results show a real remaining weakness. Do not weaken first-pass improvements merely because a high-HP dummy produces a large theoretical heart-candle number.
