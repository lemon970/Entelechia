# 玩家可见文字全量审查（2026-07-11）

## 审查约束

本轮只审查并落盘，不修改业务代码、图片或 PCK。

## 总体结论

当前本地化 JSON 中的玩家文本已经全部是简体中文，但仍有两个运行时状态绕过了模组本地化，另有一张会显示英文的模组封面图。

需要后续正式处理的项目：

1. `BloodlettingStrengthPower`：运行时不使用模组准备的中文状态键。
2. `BloodDebtStrengthPower`：运行时不使用模组准备的中文状态键。
3. `Entelechia/mod_image.png`：图片内嵌英文 `Placeholder`。

## 本地化 JSON

`eng` 与 `zhs` 均包含以下 7 个文件，文件集合和内容完全一致：

| 文件 | 玩家文本叶值 | 英文单词残留 | 空值 |
|---|---:|---:|---:|
| `ancients.json` | 8 | 0 | 0 |
| `cards.json` | 368 | 0 | 0 |
| `card_keywords.json` | 0 | 0 | 0 |
| `characters.json` | 28 | 0 | 0 |
| `powers.json` | 108 | 0 | 0 |
| `relics.json` | 6 | 0 | 0 |
| `static_hover_tips.json` | 0 | 0 | 0 |

合计 518 个玩家文本值。所有文件均能按 UTF-8 解析，没有 JSON 语法错误。

空的 `card_keywords.json` 和 `static_hover_tips.json` 当前没有对应的自定义关键词或静态悬停提示注册，因此暂不构成缺口。

### 卡牌

- 共发现 57 个 `EntelechiaCard` 具体类。
- 每张牌所需的带前缀与无前缀 `title`、`description`、`description+` 共 342 个键全部存在。
- 所有值均为中文。
- `cards.json` 另有 26 个 Power 条目，它们在正确的 `powers.json` 中也存在，属于放错表的冗余副本，不影响当前显示，后续可清理。

### 遗物与角色

- `BloodDemonReplete` 的标题、描述、风味文本均为中文。
- 角色名称、介绍、对话和角色选择文本均为中文。
- `Entelechia.json` 的名称与描述为中文；`lemon` 是作者署名，`Entelechia` 是内部模组 ID，不需要翻译。

## 状态与异常状态

### 正常命中的 16 个自定义状态

以下状态继承 `EntelechiaPower`，实际使用 `ENTELECHIA-<POWER_NAME>` 键。它们的 `title`、`description`、`smartDescription` 在 `eng` 和 `zhs` 中均存在且均为中文：

- 血速 `BloodSpeedPower`
- 失血 `BloodlossPower`
- 萃血 `BloodHarvestPower`
- 心烛 `HeartCandlePower`
- 血宴 `BloodFeastPower`
- 余烛 `CandleEmberPower`
- 血魔形态 `BloodDemonFormPower`
- 永续重盈 `EternalRepletePower`
- 蔷薇步 `RoseStepPower`
- 凝血本能 `ClotInstinctPower`
- 痛觉转化 `PainConversionPower`
- 凝血屏障 `ClottingBarrierPower`
- 不死的血统 `ImmortalBloodlinePower`
- 血裔王庭 `BloodClanCourtPower`
- 绯红庇护 `CrimsonWardPower`
- 余烬血脉 `EmberBloodlinePower`

其中敌方常见异常状态 `失血`、`萃血`、`心烛` 的静态键均正确。若实机仍显示英文，需要记录实际英文标题并在运行时输出 `PowerModel.Id.Entry`，静态文件中没有可继续猜测修补的缺键。

### 两个确定存在问题的我方状态

`BloodlettingStrengthPower` 与 `BloodDebtStrengthPower` 直接继承游戏原生 `TemporaryStrengthPower`，没有继承 `EntelechiaPower`。

当前行为：

- `OriginModel` 返回 `null`。
- Title 不读取 `<本状态 ID>.title`，而是读取 `OriginModel.Title`；当前可能抛出 `InvalidOperationException`。
- Description 固定读取游戏原生 `TEMPORARY_STRENGTH_POWER.description`。
- SmartDescription 固定读取游戏原生 `TEMPORARY_STRENGTH_POWER.smartDescription`。
- `powers.json` 中现有的 `ENTELECHIA-BLOODLETTING_STRENGTH_POWER.*` 和 `ENTELECHIA-BLOOD_DEBT_STRENGTH_POWER.*` 实际不会被这两个类使用。
- BaseLib 分析器会为二者报告 `STS003`，说明其 ID 不会自动添加模组前缀。

因此这两项是当前我方状态英文或显示异常的确定来源。详细修复设计见 `docs/STATUS_LOCALIZATION_AUDIT_20260711.md`。

### 游戏原生状态

模组还会直接施加原生 `StrengthPower`。其 `STRENGTH_POWER.*` 文本由游戏本体按当前语言提供，不属于模组缺键，也不应复制进模组本地化表。

## 代码与场景

- C# 中的英文字符串均属于日志、内部 ID、资源路径、节点名、反射方法名或动画属性名，没有发现直接写入玩家 UI 的英文。
- `.tscn`、`.tres` 和配置中没有发现 Label、tooltip、标题或描述类英文文本。
- `EntelechiaCardPool.Title` 返回内部角色 ID，并有明确注释说明它不是显示名称。

## 图片文字

确认存在一处英文图片文字：

- `Entelechia/mod_image.png`：黑底白字 `Placeholder`，可能出现在模组管理界面。

先前图片审查没有在角色选择背景、角色立绘、卡牌插画、状态图标或遗物图中发现其他明确英文文字。

## 日志证据

2026-07-11 21:46 的实机日志显示：

- 加载 `res://localization/eng`。
- 找到并合并模组 `eng/powers.json`。
- 加载 `res://localization/zhs`。
- 找到并合并模组 `zhs/powers.json`。

最近 5 份游戏日志中没有发现 Missing localization、缺失 LocString 或 `ENTELECHIA-*.title/description` 报错。该证据能证明语言包被加载，但不能替代两个临时力量状态的实机悬停验证。

## 后续正式修改清单

1. 将两个临时力量类改为继承统一的自定义临时力量基类，确保使用现有中文键并保留加力量、叠加和回合结束移除行为。
2. 将 `mod_image.png` 换为无英文文字的正式封面，或只保留中文“隐德来希”。
3. 清理 `cards.json` 中 26 个放错表的 Power 冗余条目。
4. 构建并重新导出 PCK。
5. 实机依次验证 `失血`、`萃血`、`心烛`、`血速`、`余烛`、`血魔形态`，以及由 `血引术`、`血债清算`、`痛觉转化`产生的临时力量。

## 本轮落盘状态

- 仅新增本审查文档。
- 未修改 C#、本地化 JSON 或图片。
- 未重新导出 PCK。

## 补丁准备状态

两个临时力量状态的修复已在独立临时副本中完成编译验证，正式工作区尚未应用。可应用补丁与验证记录：

- `docs/patches/20260711-status-localization-fix.patch`
- `docs/STATUS_LOCALIZATION_PATCH_VALIDATION_20260711.md`
