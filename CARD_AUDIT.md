# Entelechia 牌实现审查报告

> 状态：历史审查记录，已被 2026-07-06 后续平衡迭代部分覆盖。当前权威数值以代码、`Entelechia/localization/eng/*.json`、`D:\desktop\mod\entelechia_sts2_mod_design.md` 和 sim2 输出为准。本文件保留用于追溯旧问题，不作为当前牌面数据来源。

生成时间：2026-07-05

---

## 已修复

| 牌名 | 问题 | 修复内容 |
|---|---|---|
| 断续脉搏 (DiscontinuousPulse) | EXHAUST_MISSING | 补加 `CardCmd.Exhaust(context, this, false, false)` |
| 断续脉搏 (DiscontinuousPulse) | HP可减至0 | `Math.Max(CurrentHp - 2, 1)` clamp |
| 红烛万象 (RedCandleAll) | 目标过滤缺失 | TargetType.None + 手动过滤 HeartCandlePower 循环 |
| 玫刺 (RoseThorn) | .cs文件缺失 | 新建 RoseThorn.cs（4伤+萃血触发抽牌） |
| 放血 (Bloodletting) | 永久力量类型错误 | 新建 BloodlettingStrengthPower : TemporaryStrengthPower |
| 血债清算 (BloodDebtSettlement) | 永久力量类型错误 | 新建 BloodDebtStrengthPower : TemporaryStrengthPower |
| 血魔圆满 (BloodDemonReplete) | null context + 效果缺失 | context-free API + 补全 BloodSpeed/ImmortalBloodline |

---

## 模拟基准

200 seeds × K=30，5回合，ε=0.3：

| 阶段 | Entelechia | 备注 |
|---|---|---|
| 初始（上一 session 修复后） | 88.84 ±0.79 | Bug 修复后基准 |
| 整合改动后（I1+I2+I4） | 84.72 ±0.50 | 见下方说明 |

| 角色 | avg-of-best | CI95 | /turn |
|---|---|---|---|
| 铁甲 (5Strike/4Defend/1Bash) | 99.41 | ±0.70 | 19.88 |
| **Entelechia（整合改动后）** | **84.72** | **±0.50** | **16.94** |
| 沉默者 (5Strike/4Defend/Neutralize/Survivor) | 74.25 | ±0.51 | 14.85 |

**84.72 低于 88.84 的原因**：I4 将 DiscontinuousPulse 从"draw1-2HP"改为"BH→BL转化"，移除了draw1补偿；I1(HeartCandle 浮动乘数)对起始牌组无效（起始牌组无 HeartCandle 施加牌，multiplier 从不触发）。

---

## 待修复（代码层面）

### 红烛万象 (RedCandleAll) — ×4乘数未实现

- 描述隐含：红烛万象触发心烛时伤害应为400%，而非标准250%
- 现状：`HeartCandlePower.Multiplier` 是 `const float`，无法按牌覆盖
- 修复方案：将 `Multiplier` 改为虚属性（virtual property），供子类或触发时临时覆盖
- 优先级：P5

### 心烛系统说明（设计备注）

`HeartCandlePower` 描述为"伤害池，攻击以250%速率消耗"，但当前实现仅有
`ModifyDamageMultiplicative ×2.5`，**无消耗/减少 Amount 逻辑**。

实际效果：一旦施加，永久×2.5，不会耗尽。若要符合描述，需在 `AfterDamageGiven` 中
按伤害量减少 Amount，Amount 归零时 `PowerCmd.Remove`。此改动影响全局平衡，建议在
正式测试前确认是否实现。

- 优先级：P6（需设计决策）

### Bloodletting ID 冲突

Sim 日志显示：游戏自带 `CARD.BLOODLETTING` 与 mod 同名卡冲突，mod 版被跳过注册。
需给 mod 的 Bloodletting 改为唯一 ID（加前缀，如 `ENTELECHIA-BLOODLETTING`）。

---

## 设计诊断：根本问题是系统孤立，不是数值不足

心烛2.5倍乘数本身已够高。问题是**四个系统彼此孤立**，无法形成合力。

### 孤立状态图（现状）

```
BloodHarvest ──→ BloodDebtSettlement（消耗回血+力量）
                 RoseThorn（条件抽牌）

BloodLoss    ──→ BloodDebtSettlement（消耗减失血+力量）

HeartCandle  ──→ 所有攻击 ×2.5（与其他系统零交互）

BloodSpeed   ──→ 攻击速度提升（与其他系统零交互）
```

每个系统都有独立的准备牌要求。起始10张牌只激活萃血（1张RoseTrail），其余三系统完全沉睡。铁甲对比：力量加成所有攻击，Vulnerable乘数所有攻击受益，两条线共享同一张牌（Bash）。

### 起始牌组实际运作分析

| 牌 | 系统贡献 | 问题 |
|---|---|---|
| BloodBlade ×4 | 零 | 纯Strike，无状态交互 |
| BloodVeil ×4 | 零 | 纯Defend，无状态交互 |
| RoseTrail ×1 | 萃血 | 唯一系统牌，但无下游 |
| DiscontinuousPulse ×1 | 零 | -2HP换抽牌，自损无回报 |

前3层本质是"无特色的Strike/Defend套"，差距集中在无乘区。

---

## 系统整合改动（实际落地）

### 落地状态总览

| ID | 改动 | 文件 | 状态 | 备注 |
|---|---|---|---|---|
| I1 | HeartCandle 浮动乘数（1.5+0.2×萃血，上限3.5） | `HeartCandlePower.cs` | ✓ 已实施 | 起始牌组sim无效（无HC施加牌）；中后期有意义 |
| I2 | RoseTrail 同时施加失血 | `RoseTrail.cs` | ✓ 已实施 | 安全，无副作用 |
| I3 | BloodVeil 格挡+1血速 | `BloodVeil.cs` | ✗ 已还原 | BloodSpeed=Counter类型，每BloodVeil叠加→draw雪球爆炸 |
| I4 | DiscontinuousPulse 改为萃血→失血转化 | `DiscontinuousPulse.cs` | ✓ 已实施（重设计） | 原设计萃血→心烛被还原，见下 |

---

### I3 还原原因

`BloodSpeedPower` 是 `PowerStackType.Counter`，每回合多抽 Amount 张牌。BloodVeil×4 每张+1血速→第2回合多抽4张→可打更多BloodVeil→指数爆炸。sim 由88跳至512。

---

### I4 设计修订

原I4设计（萃血→心烛）被还原，原因：

**`CommonActions.Apply<HeartCandlePower>` 触发乘法雪球。**

实测：Apply HC后 sim 从 88 → 754（6.25×铁甲基准）。推断：`PowerStackType.Single` 并不限制Amount增长；每点Amount使`ModifyDamageMultiplicative`额外执行一次，乘数叠加为 2.5^Amount。示例：Amount=2 → ×6.25，Amount=3 → ×15.6。

**规则：不得在战斗中动态调用 `Apply<HeartCandlePower>`。** 心烛只能通过静态卡牌（HeartCandleRitual、HeartBrand等）在牌组阶段施加，不能作为OnPlay效果。

**实际落地的I4**：消耗2层萃血→施加2层失血（纯叠加系统互转，无乘法风险）。

```csharp
// DiscontinuousPulse.cs — 0E, TargetType.None, 自消耗
// Exhaust 在最前，确保任何分支都不会保留在手牌中
await CardCmd.Exhaust(context, this, false, false);
// 找第一个有萃血的敌人，消耗2层→施加2层失血
```

---

### 最终系统连接图（实际落地）

```
BloodHarvest ──(×0.2/层, 上限3.5)──→ HeartCandle 乘数 [I1]
     ↑                                      ↑（静态施加，非动态Apply）
RoseTrail ──→ BloodHarvest + BloodLoss [I2] HeartCandleRitual等
     ↑
DiscontinuousPulse → 消耗BH → 追加BloodLoss [I4]
```



---

## 修复优先级总表

| 优先级 | 项目 | 类型 | 状态 |
|---|---|---|---|
| ✓ | 断续脉搏 Exhaust + HP clamp | Bug | 已修复 |
| ✓ | 红烛万象 目标过滤 | Bug | 已修复 |
| ✓ | 玫刺 缺失实现 | 缺失 | 已修复 |
| ✓ | 放血/血债清算 临时力量 | Bug | 已修复 |
| ✓ | 血魔圆满 效果完整性 | Bug | 已修复 |
| P5 | 红烛万象 ×4乘数 | 需重构 HeartCandlePower | 待实施 |
| P6 | 心烛系统 drain 逻辑 | 需设计决策 | 待实施 |
| P7 | Bloodletting ID 冲突 | 注册冲突 | 待实施 |
| B1-B5 | 平衡调整 | 设计改进 | 待确认方向 |
