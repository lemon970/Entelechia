# 状态中文化补丁验证（2026-07-11）

## 状态

补丁只保存在 `docs/patches/20260711-status-localization-fix.patch`，尚未应用到正式工作区。

## 方案

保留游戏原生 `TemporaryStrengthPower` 的战斗实现，仅补齐：

- `ICustomPower`，使具体状态获得 `ENTELECHIA-` ID 前缀。
- 合法的来源卡牌，消除 `OriginModel == null`。
- 使用具体状态 ID 的标题、描述和智能描述键。
- 使用模组状态图标路径，并继续沿用现有缺图回退。

映射关系：

- `BloodlettingStrengthPower` -> `EntelechiaBloodletting`
- `BloodDebtStrengthPower` -> `BloodDebtSettlement`

补丁不会重写力量施加、叠加、移除或回合结束逻辑。

## 验证方法

1. 将当前项目复制到独立临时目录。
2. 仅在临时目录应用补丁。
3. 将 `ModsPath` 指向临时输出目录，避免覆盖正式模组。
4. 执行：

```powershell
dotnet build Entelechia.csproj -c Release /p:ModsPath="<临时目录>\test-mods\"
```

## 验证结果

- 构建成功。
- 0 个错误。
- 10 个警告，均为项目原有的可空引用或 BaseLib 弃用 API 警告。
- 原先两个临时力量类的 `CS8764` 和 `STS003` 警告均消失。
- `git apply --check docs/patches/20260711-status-localization-fix.patch` 通过。
- 正式工作区业务代码、正式 DLL 和 PCK 未被本次验证覆盖。

## 正式应用后的验收项

1. `血引术`产生的临时力量标题和描述均为中文。
2. `痛觉转化`产生的同类状态正常叠加并显示中文。
3. `血债清算`产生的临时力量显示中文。
4. 三种来源均在拥有者回合结束时正确移除对应力量。
5. 悬停状态不会出现异常或缺失标题。
