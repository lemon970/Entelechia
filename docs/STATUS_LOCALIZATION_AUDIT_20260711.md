# 状态中文化专项审查（2026-07-11）

## 本轮范围

重点检查敌方异常状态和我方状态仍显示英文的问题。本轮只记录结论，不修改业务代码，不重新导出 PCK。

## 已确认

1. `Entelechia/localization/eng/powers.json` 与 `Entelechia/localization/zhs/powers.json` 内容一致。
2. 代码中共有 18 个具体 Power 类；按 BaseLib 的类名转 ID 规则核对后，18 个带 `ENTELECHIA-` 前缀的 `.title`、`.description`、`.smartDescription` 条目均存在。
3. BaseLib 3.3.5 官方资源使用 `zhs` 作为简体中文目录名，当前目录名正确。
4. 2026-07-11 21:46 的实机日志显示游戏依次加载了 `res://localization/eng` 和 `res://localization/zhs`，并找到了、合并了两套 `powers.json`。因此不能再把问题简单归因于缺少 `zhs` 目录。
5. `BloodlossPower`、`BloodHarvestPower`、`HeartCandlePower` 等敌方异常状态继承 `EntelechiaPower`，其运行时 ID 应分别命中：
   - `ENTELECHIA-BLOODLOSS_POWER`
   - `ENTELECHIA-BLOOD_HARVEST_POWER`
   - `ENTELECHIA-HEART_CANDLE_POWER`

## 高风险问题

以下两个我方状态没有继承 `EntelechiaPower`：

- `BloodlettingStrengthPower`
- `BloodDebtStrengthPower`

它们直接继承游戏原生 `TemporaryStrengthPower`，并且都把 `OriginModel` 返回为 `null`。已反编译当前游戏实现确认：

- 状态标题从 `OriginModel` 读取；`null` 不属于合法来源，会进入异常路径。
- 状态描述固定读取游戏原生键 `TEMPORARY_STRENGTH_POWER.description`。
- BaseLib 分析器会对这两个类报告 `STS003`：没有继承 `CustomPowerModel` 或实现 `ICustomModel`，运行时 ID 不会自动添加模组前缀。
- 目前 `powers.json` 中为 `ENTELECHIA-BLOODLETTING_STRENGTH_POWER` 和 `ENTELECHIA-BLOOD_DEBT_STRENGTH_POWER` 准备的文本，按现有继承关系不会被正常使用。

这两个类是当前最明确的我方状态英文或显示异常来源。

## 建议修改（尚未实施）

下一轮将两个临时力量状态改为真正的自定义 Power：

1. 继承统一的 `EntelechiaTemporaryStrengthPower` 基类。
2. 基类继承 `EntelechiaPower` 并实现 `ITemporaryPower`。
3. 保留当前行为：施加时增加力量、叠加时同步力量、拥有者回合结束时移除对应力量与状态。
4. 让标题和描述使用现有的两个 `ENTELECHIA-...` 本地化键。
5. 构建后重点实机检查：`血引术`、`血债清算`、`痛觉转化`生成的临时力量状态。

## 敌方异常状态后续验证

现有静态证据没有发现 `失血`、`萃血`、`心烛`的键缺失。下一轮若实机仍显示英文，应记录具体英文标题并截屏，同时在调试日志中输出实际 `PowerModel.Id.Entry`；根据运行时 ID 修正对应键，不做猜测式批量改名。

建议测试顺序：

1. 对敌人施加 `失血`。
2. 对敌人施加 `萃血`。
3. 对敌人施加 `心烛`。
4. 检查我方 `血速`、`余烛`、`血魔形态`。
5. 单独检查两个临时力量来源。

## 本轮落盘状态

- 仅新增本审查文档。
- 未保留任何 Power 代码改动。
- 未重新导出或替换 PCK。
- 已部署 DLL 已恢复为本轮开始时的原实现。
