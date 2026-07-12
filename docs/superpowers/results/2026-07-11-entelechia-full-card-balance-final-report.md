# 隐德来希 57 张牌全牌库平衡终审报告

日期：2026-07-11

## 完成范围

- 当前 `EntelechiaCode/Cards` 中共有 57 个具体卡牌类。
- 第一至第七批调整或复核 39 张唯一卡牌；第八批明确保持 18 张。
- 两组清单去重后覆盖 57 张，缺失 0，未知项 0。
- `BloodStorm` 在第二批完成主体调整，并在第五批生命费用/双状态批次中再次回归验证，因此批次记录存在一次有意重复。

## 平衡目标

- 高血：过牌、加费、主动压血和体系启动。
- 低血：续航、格挡、返能和高平均爆发。
- 低血边界统一为 `CurrentHp <= MaxHp * 0.5m`，恰好半血进入低血分支。
- Mod 角色允许略高于原版，不因理论木桩心烛爆发主动削弱实战输出。
- 不依赖理论无限判断；以 sim2 出牌次数和 100 次出牌上限命中率为循环证据。

## 已调整或重点复核的 39 张牌

### 第一批：基础生存与资源

- BloodMend
- Autophagy
- BloodRebuild
- CrimsonEmbers

### 第二批：高阶能力与终幕

- BloodStorm
- BloodClanCourt
- BloodDemonForm
- FarewellFinale
- EternalReplete
- ImmortalBloodline
- BloodFrenzy

### 第三批：低血资源与防守

- ClottingBackflow
- BloodBorrow
- SoulBloodDraw
- ClottingBarrier
- SanguineRite
- PainConversion

### 第四批：心烛转换与稀有终幕支援

- BloodFeast
- BloodDissect
- CrimsonMerge
- BloodSweep
- BloodToCandle
- CandleEmber

### 第五批：生命费用与双状态行动力

- BloodDebtSettlement
- Bloodletting
- BloodOverload
- BloodHaste
- BloodOffering
- BloodStorm（回归验证）

### 第六批：普通牌低血行动力与起始防守

- BloodSurge
- Suture
- BloodPulse
- CounterSlash
- BloodFragrance
- BloodVeil

### 第七批：重复牌差异化与多段输出

- BloodDrain
- CrimsonShield
- CrimsonLash
- Lacerate
- BloodClawSlash

## 第八批终审后保持的 18 张牌

### 基础和普通牌

- `BloodBlade`：1 费 `6/9` 并施加 1 层萃血，是略强于原版 Strike 的特色起始牌；起始构筑没有超标。
- `BloodStrike`：1 费 `10/14` 纯伤害，无过牌、返能或状态，作为 Mod 普通牌合理。
- `BloodShield`：1 费 `10/13` 纯格挡，是普通防御标尺，不再追加低血收益。
- `BloodMist`：1 费 `7/9` 格挡并对全体施加 `1/2` 失血，收益延迟且跳过死亡目标。
- `BloodSplash`：1 费全体 `5/7` 并施加 `1/2` 萃血，以较低群伤换后续低血续航。
- `BloodInfect`：1 费施加 `4/6` 失血并抽 1；没有即时伤害，不接受仅基于长战理论总量的削弱。
- `CandleScorch`：1 费 `7/9`，攻击前生成 `5%/8%` 心烛并即时兑现，是心烛普通攻击入口。
- `HeartBrand`：首次建池 `12%/18%`、返至多 1 能量并抽 1；已有心烛时降为 `6%/9%` 且不返能，已有限制重复收益。
- `RoseThorn`：0 费 `3/5`，仅在目标打出前已有萃血时抽 1；过牌需要消耗体系资源。

### 能力、稀有牌和体系组件

- `ClotInstinct`：1 费能力，每回合首次触发萃血时获得 `4/6` 格挡，受每回合一次限制。
- `CrimsonSacrifice`：1 费、6 生命、抽 `4/5` 并消耗；强过牌受稀有度、生命费用和一次性限制。
- `DiscontinuousPulse`：0 费消耗并抽 1；有萃血时消耗 1 层换 1 能量和 `2/3` 失血，资源有限且先自消耗。
- `HeartCandleRitual`：0 费生成 `18%/27%` 心烛并消耗，不能自行兑现。
- `RedCandleAll`：2 费，只攻击已有心烛的敌人；低下限和前置条件约束群体兑现能力。
- `ReviveCandle`：1 费消耗，复燃原始已消费池的 `50%/100%`；账本排除复燃池递归，并受当前生命容量限制。
- `RoseStep`：0 费 `4/6` 格挡并令下一次攻击施加 `2/3` 萃血；完整矩阵持续为 `Cap=0%`，不接受无实证的追加消耗削弱。
- `RoseTrail`：1 费两段 `4/5`，攻击后施加 `2/3` 萃血和 1 失血，是起始牌组的体系入口。
- `SpiritAndDesireFarewell`：3 费群体 `10/12 ×2`，随后对存活目标生成 `12%/18%` 心烛；满费用、延迟生成和死亡过滤限制上限。

## 被否决的削弱建议

### BloodInfect `4/6 -> 4/5`

- 失血是延迟收益，本牌不造成即时伤害且仍支付 1 能量。
- 用户已确认无伤害的生成/铺状态牌不应因理论总量继续卡手。
- 当前 sim2 没有循环或超上限证据，因此保持 `4/6`。

### RoseStep 增加消耗

- 理论上可在多轮洗牌中重复生成萃血，但它不抽牌、不返能，也不能单独组成循环。
- 完整矩阵最高平均出牌约 `5.87`，所有场景 100 次出牌上限命中率均为 `0%`。
- 缺少实证时不削弱其 0 费构筑价值，因此保持不消耗。

## API 与现成实现复用

- 生命费用统一复用 `HpCost`、`CanPayHpCost` 和 `TryPayHpCost`。
- 抽牌统一复用 `EntelechiaCard.DrawCards` / `CardPileCmd.Draw`。
- 返能统一复用 `PlayerCmd.GainEnergy`。
- 条件格挡复用 `CreatureCmd.GainBlock`。
- 攻击统一复用项目当前游戏版本兼容的 `ExecuteCardAttack` 和 `AttackCommand.WithHitCount`。
- 非攻击生命流失复用 `TurnStateTracker.LoseHpTracking` 与游戏 `DamageProps`。
- 心烛复燃复用 `HeartCandleLedger`，不另造递归池。

## 最终验证证据

- Mod Release：成功，14 条既有警告，0 错误。
- sim2 Release：成功，0 警告，0 错误。
- `hidden-tests`：`115/115 passed`。
- sim2 加载确认：57 个 card canonicals。
- 完整平衡矩阵：20 seeds、5 turns，覆盖 7 个构筑、8 组生命/敌伤条件。
- 所有场景 100 次出牌上限命中率：`0%`。
- 低血续航构筑 12 HP、每回合 12 敌伤：死亡率 `0%`，平均治疗 `8.00`，最终生命 `28.00`。
- 轮转加费构筑同场景：死亡率 `0%`，平均出牌 `5.87`，平均获得能量 `1.76`。
- 血速多段构筑同场景：平均伤害 `16.87`，死亡率 `40%`；加强输出但没有额外行动力膨胀。
- 心烛爆发构筑 50 HP 无敌伤：平均伤害 `40.34`；12 HP 高压死亡率 `100%`，说明高输出没有消除极低血风险。

## 最终判断

57 张牌均已逐张进入调整或明确保持清单。当前牌库形成了高血轮转/启动、低血续航/返能/爆发、萃血防守、失血延迟伤害和心烛高生命目标爆发五条相互连接的路线。数值整体高于原版基准，但 sim2 没有出现 100 次出牌上限命中，低血高压场景仍保留构筑差异和死亡风险，符合 Mod 角色略强且保留隐德来希特色的目标。
