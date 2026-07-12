# Entelechia Heart Candle Balance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现已批准的心烛第一轮平衡，使生成牌在前期能独立启动，兑现倍率随战斗等级递增，血魔形态可加法叠层，残烛复燃不能递归复制伤害池。

**Architecture:** `HeartCandlePower` 继续持有敌方剩余池并结算追加失血；`HeartCandleLedger` 区分原生消耗、未消耗复燃池和累计复燃量。sim2 的 `Harness` 增加遭遇类型和目标生命参数，在既有 smoke tests 上先覆盖行为，再新增 `hidden-balance` 输出前期与长战矩阵。

**Tech Stack:** C#/.NET 9、STS2/BaseLib API、Harmony、sim2 headless harness、JSON 本地化。

## 执行状态（2026-07-10）

- Task 1-7 的实现与测试已完成，最终 smoke tests 为 `65/65 passed`。
- Task 8 的 Mod 构建、sim2 构建、`compare-chars`、`hidden-archetypes` 和 `hidden-balance` 已完成。
- 最终结果见 `docs/superpowers/results/2026-07-10-entelechia-heart-candle-balance-results.md`。
- 代码审查补充修复：sim2 的每次 `BeginCombat` / `EndCombat` 会清理 `HeartCandleLedger`，避免长矩阵保留旧战斗 Creature 引用。
- 卡牌名称和 ID 未修改，本轮未生成、重绘或改动图片资源。
- sim2 位于主仓库外层的未跟踪目录 `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main`，其修改未单独提交。
- 主仓库在本轮前已有大量用户改动和未跟踪文件，因此没有把实现文件打包提交；当前仅保留既有计划提交 `429c074`。
- 下方步骤清单保留为实施过程记录，不据此推断 Git 提交状态。

---

### Task 1: Extend sim2 combat scenarios

**Files:**
- Modify: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/Harness.cs`
- Modify: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaSmokeTests.cs`

- [ ] **Step 1: Write failing room-type tests**

注册普通、精英、Boss 和无遭遇四个测试。固定 100 HP 目标、100 点心烛和固定攻击，断言心烛追加伤害分别按 `2.0/2.5/3.0/2.0` 结算。

```csharp
private static async Task<TestResult> Test_HeartCandle_RoomMultiplier(
    RoomType? roomType, decimal expectedMultiplier)
{
    var h = BeginHiddenCombat(new[] { T("BloodBlade") }, roomType: roomType, dummyHp: 100m);
    await ApplyEnemyPower(h, "HeartCandlePower", 100m);
    var attackDamage = GetAttackBaseDamage(card);
    var hpBefore = h.Dummy.CurrentHp;
    await Play(h, card, h.Dummy);
    var actualBonus = hpBefore - h.Dummy.CurrentHp - attackDamage;
    return actualBonus == Math.Ceiling(attackDamage * expectedMultiplier) ? Pass(name) : Fail(name, details);
}
```

- [ ] **Step 2: Run `dotnet run -c Release -- hidden-tests` and verify RED**

Expected: `BeginCombat` 尚不支持场景参数，或新倍率断言按旧 1.5 倍失败。

- [ ] **Step 3: Add encounter and HP parameters**

为 `BeginCombat` 重载追加 `RoomType? roomType = null, decimal dummyHp = 999m`，并使用：

```csharp
var encounter = roomType is null
    ? null
    : ModelDb.AllEncounters.First(e => e.RoomType == roomType.Value).ToMutable();
var combat = new CombatState(encounter, NullRunState.Instance);
dummy.MaxHp = dummyHp;
dummy.CurrentHp = dummyHp;
```

- [ ] **Step 4: Run `dotnet build -c Release`**

Expected: sim2 编译通过，倍率行为测试仍为红灯。

- [ ] **Step 5: Commit `test(sim2): cover heart candle encounter multipliers`**

### Task 2: Implement encounter multipliers and form stacks

**Files:**
- Modify: `EntelechiaCode/Powers/HeartCandlePower.cs`
- Modify: `EntelechiaCode/Powers/BloodDemonFormPower.cs`
- Test: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaSmokeTests.cs`

- [ ] **Step 1: Add failing tests for two form stacks, Red Candle `+0.75`, additive coexistence, and pool cap**
- [ ] **Step 2: Run `hidden-tests` and verify RED**
- [ ] **Step 3: Implement additive multiplier**

```csharp
var baseMultiplier = target.CombatState?.Encounter?.RoomType switch {
    RoomType.Elite => 2.5m, RoomType.Boss => 3.0m, _ => 2.0m };
var formBonus = dealer.Powers?.OfType<BloodDemonFormPower>().FirstOrDefault()?.Amount * 0.5m ?? 0m;
var redBonus = cardSource is RedCandleAll ? 0.75m : 0m;
return baseMultiplier + formBonus + redBonus;
```

- [ ] **Step 4: Set form `StackType.Counter`; apply `Amount` strength, `Amount * 4%` seed, and `Amount` BloodSpeed**

### Task 3: Fix early starters and pure-generation costs

**Files:**
- Modify: `EntelechiaCode/Cards/CandleScorch.cs`
- Modify: `EntelechiaCode/Cards/HeartBrand.cs`
- Modify: `EntelechiaCode/Cards/HeartCandleRitual.cs`
- Modify: `EntelechiaCode/Cards/BloodToCandle.cs`
- Test: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaSmokeTests.cs`

- [ ] **Step 1: Add failing behavior tests**

断言烛痕刺攻击前生成 `5%/8%` 并可自行兑现；心烛印记首次 `12%/18%` 且只返实际支付最多 1 能量、已有池 `6%/9%` 不返能；仪式和熔血为烛为 0 费；熔血为烛每两层失血生成 `4%/6%`，最多消耗 8 层。

- [ ] **Step 2: Run `hidden-tests` and verify RED**

- [ ] **Step 3: Implement CandleScorch order and values**

```csharp
if (cardPlay.Target != null)
    await HeartCandlePower.ApplyPercent(context, cardPlay.Target, this, IsUpgraded ? 8m : 5m, true);
await ExecuteCardAttack(context, cardPlay);
```

- [ ] **Step 4: Implement HeartBrand first-seed logic**

```csharp
var hadCandle = target.Powers?.Any(p => p is HeartCandlePower) == true;
var percent = hadCandle ? (IsUpgraded ? 9m : 6m) : (IsUpgraded ? 18m : 12m);
await HeartCandlePower.ApplyPercent(context, target, this, percent, true);
if (!hadCandle)
    await PlayerCmd.GainEnergy(context, Owner, Math.Min(cardPlay.Resources.EnergySpent, 1));
await DrawCards(context, 1);
```

- [ ] **Step 5: Change Ritual to cost 0 and `18%/27%`; BloodToCandle to cost 0 and `4%/6%` per pair**
- [ ] **Step 6: Build/copy DLL/run `hidden-tests`, expect GREEN**
- [ ] **Step 7: Commit `feat: improve early heart candle generation`**

### Task 4: Raise conditional generation

**Files:**
- Modify: `EntelechiaCode/Cards/BloodSweep.cs`
- Modify: `EntelechiaCode/Cards/SpiritAndDesireFarewell.cs`
- Modify: `EntelechiaCode/Cards/CrimsonMerge.cs`
- Test: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaSmokeTests.cs`

- [ ] **Step 1: Change tests to require BloodSweep `6%/9%`, Farewell `12%/18%`, CrimsonMerge `4%/6%` per harvest trigger**
- [ ] **Step 2: Run `hidden-tests` and verify RED**
- [ ] **Step 3: Replace only generation constants**

```csharp
// BloodSweep: IsUpgraded ? 9m : 6m
// SpiritAndDesireFarewell: IsUpgraded ? 18m : 12m
// CrimsonMerge per trigger: IsUpgraded ? 6 : 4
```

- [ ] **Step 4: Build/copy DLL/run `hidden-tests`, expect GREEN**
- [ ] **Step 5: Commit `feat: raise conditional heart candle generation`**

### Task 5: Make Revive Candle non-recursive

**Files:**
- Modify: `EntelechiaCode/CombatPatches.cs`
- Modify: `EntelechiaCode/Powers/HeartCandlePower.cs`
- Modify: `EntelechiaCode/Cards/ReviveCandle.cs`
- Test: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaSmokeTests.cs`

- [ ] **Step 1: Add failing ledger tests**

覆盖基础 50%、升级 100%、生命上限截断、复燃池优先消耗、两张复燃不能重复恢复同一原生额度。

- [ ] **Step 2: Run `hidden-tests` and verify RED**

- [ ] **Step 3: Replace ledger dictionaries and consumption logic**

```csharp
private static readonly Dictionary<Creature, decimal> OriginalConsumedByCreature = new();
private static readonly Dictionary<Creature, decimal> OutstandingRevivedByCreature = new();
private static readonly Dictionary<Creature, decimal> TotalRevivedByCreature = new();

public static void RecordConsumed(Creature creature, decimal amount)
{
    var outstanding = OutstandingRevivedByCreature.GetValueOrDefault(creature);
    var revivedConsumed = Math.Min(amount, outstanding);
    OutstandingRevivedByCreature[creature] = outstanding - revivedConsumed;
    OriginalConsumedByCreature[creature] =
        OriginalConsumedByCreature.GetValueOrDefault(creature) + amount - revivedConsumed;
}
```

- [ ] **Step 4: Bound preview and record actual revive**

### Task 6: Synchronize localization data

**Files:**
- Modify: `Entelechia/localization/eng/cards.json`
- Modify: `Entelechia/localization/eng/powers.json`

- [ ] **Step 1: Add a failing JSON consistency check to `hidden-tests`**

检查带前缀和不带前缀的重复条目都存在且数值文本一致，涉及 CandleScorch、HeartBrand、HeartCandleRitual、BloodToCandle、BloodSweep、SpiritAndDesireFarewell、CrimsonMerge、ReviveCandle、HeartCandlePower 和 BloodDemonFormPower。

- [ ] **Step 2: Run `hidden-tests` and verify RED**
- [ ] **Step 3: Update descriptions without changing card names or IDs**
- [ ] **Step 4: Parse JSON and run `hidden-tests`, expect GREEN**

```powershell
Get-Content Entelechia/localization/eng/cards.json -Raw | ConvertFrom-Json | Out-Null
Get-Content Entelechia/localization/eng/powers.json -Raw | ConvertFrom-Json | Out-Null
```

- [ ] **Step 5: Commit `docs: sync heart candle balance descriptions`**

### Task 7: Add hidden-balance matrix and metrics

**Files:**
- Create: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/EntelechiaBalance.cs`
- Modify: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/Program.cs`
- Modify: `D:/desktop/mod/3mkyqpycotk22-main/3mkyqpycotk22-main/src/Harness.cs`

- [ ] **Step 1: Add the `hidden-balance` command before implementation**

```csharp
case "hidden-balance":
    await EntelechiaBalance.Run();
    break;
```

Run: `dotnet run -c Release -- hidden-balance`

Expected: compile failure because `EntelechiaBalance` does not exist.

- [ ] **Step 2: Add per-combat counters**

Expose natural heart-candle generation, consumed amount, remaining pool, total damage, energy spent and damage per energy. Reset counters in `Harness.BeginCombat`/`EndCombat`; `HeartCandlePower.ApplyPercent` records the actual post-apply delta, not the theoretical pool.

- [ ] **Step 3: Implement long-fight matrix**

```text
K = 1, 5, 10, 40
target HP = 80, 200, 500
room = Monster, Elite, Boss
```

- [ ] **Step 4: Implement early-game matrix**

```text
deck = starter / +CandleScorch / +HeartBrand / +both
K = 1, 5
target HP = 60, 80, 120
room = Monster
```

- [ ] **Step 5: Print total attack damage, generated, consumed, remaining, energy spent and total damage per energy**
- [ ] **Step 6: Run `dotnet run -c Release -- hidden-balance`, expect all rows and no exception**
- [ ] **Step 7: Commit `test(sim2): add hidden entelechia balance matrix`**

### Task 8: Full verification and results record

**Files:**
- Create: `docs/superpowers/results/2026-07-10-entelechia-heart-candle-balance-results.md`

- [ ] **Step 1: Build mod and sim2 from clean command invocations**

```powershell
dotnet build Entelechia.csproj -c Release
dotnet build D:\desktop\mod\3mkyqpycotk22-main\3mkyqpycotk22-main -c Release
```

- [ ] **Step 2: Run the four verification commands**

```powershell
dotnet run -c Release --no-build -- hidden-tests
dotnet run -c Release --no-build -- compare-chars
dotnet run -c Release --no-build -- hidden-archetypes
dotnet run -c Release --no-build -- hidden-balance
```

- [ ] **Step 3: Record exact command outputs, failed rows, and balance interpretation**
- [ ] **Step 4: Confirm all names/IDs are unchanged and no image files changed**
- [ ] **Step 5: Commit `docs: record heart candle balance verification`**

## Self-review

- Spec coverage: room multipliers, additive form/card bonuses, eight generation sources, first-seed energy refund, costs, non-recursive revive ledger, localization and both sim matrices all have explicit tasks.
- Placeholder scan: no `TBD`, `TODO`, or undefined follow-up step remains.
- Type consistency: `RoomType?`, `decimal dummyHp`, `EnergySpent` as `int`, and the three revive-ledger dictionaries are named consistently across tasks.
- Scope: card names, IDs and image assets remain unchanged.
