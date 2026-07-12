# STS2 Mod 调试工作流

## 问题定位流程

### 1. 收集错误信息
- **游戏日志路径**: `%APPDATA%\Roaming\SlayTheSpire2\logs\`
- **Player.log**: 运行时错误、异常堆栈
- **关键错误模式**:
  - `DuplicateModelException` → ModelDb重复注册
  - `NullReferenceException` → API不存在或对象未初始化
  - `MissingMethodException` → 方法签名不匹配

### 2. 症状分类

#### 黑屏/无法进入战斗
**原因**: 模型加载失败、异常未捕获
**排查**:
1. 检查 `StartingDeck` / `StartingRelics` 是否触发重复注册
2. 查看 `ScriptManagerBridge.LookupScriptsInAssembly` 是否预注册了卡牌
3. **解决方案**: 使用 `GetOrCreate<T>()` 模式检查 `ModelDb` 是否已存在

```csharp
private static T GetOrCreate<T>() where T : CardModel, new()
{
    return ModelDb.Card<T>() ?? new T();
}
```

#### 攻击无伤害
**原因**: `CommonActions.CardAttack` 返回 `AttackCommand` 构建器，未调用 `.Execute()`
**解决方案**:
```csharp
await CommonActions.CardAttack(this, cardPlay, 1).Execute(context);
```

#### 战后黑屏/卡死
**原因**: 奖励卡池无有效稀有度卡牌（Basic不计入）
**解决方案**: 创建至少3张 `CardRarity.Common` 卡牌

#### Power效果不触发
**原因**: 未重写生命周期钩子方法
**解决方案**: 在 `CustomPowerModel` 子类中重写:
- `AfterSideTurnStart` - 回合开始
- `AfterOwnerDamaged` - 受伤后
- `ModifyIncomingDamage` - 修改伤害

### 3. API查找策略

#### 策略A: 反射工具
创建独立 .NET 项目查询 API：
```csharp
var dll = Assembly.LoadFrom(@"path\to\sts2.dll");
var type = dll.GetTypes().FirstOrDefault(t => t.Name == "TargetType");
foreach (var m in type.GetMethods()) { /* 输出 */ }
```

#### 策略B: 在线资源
- **官方Wiki**: https://github.com/Alchyr/ModTemplate-StS2/wiki
- **中文教程**: https://tutorials.sts2modding.com
- **Common Commands Cookbook**: 标准API模式参考

#### 策略C: 反编译
- **GDRETools**: Godot项目反编译（教学PDF推荐）
- **dnSpy**: .NET DLL反编译
- **路径**: `res://src/Core/Models/` 查看原版实现

### 4. 常见API模式

#### HP操作
```csharp
// 设置HP（自动clamp到1）
await CreatureCmd.SetCurrentHp(creature, newHp);
```

#### Power操作
```csharp
// 应用Power
await CommonActions.Apply<MyPower>(context, target, source, amount, showEffect: true);

// 修改Power层数
await PowerCmd.Modify(owner, powerModel, deltaAmount, source);
```

#### 抽牌
```csharp
await CommonActions.Draw(source, context, amount);
```

## 编译验证

### 本地编译
```powershell
cd D:\desktop\mod\Entelechia
dotnet build --no-incremental
```

### 快速测试循环
1. 编译 → 0 errors
2. 启动游戏 → 选择角色
3. 进入战斗 → 使用卡牌
4. 查看日志 → 定位异常
5. 修改代码 → 回到步骤1

## 典型Bug修复案例

### Case 1: DuplicateModelException
**症状**: 黑屏，日志显示 `Already have a model with id X`
**根因**: `ScriptManagerBridge` 预注册 + `StartingDeck` 中 `new T()` 重复注册
**修复**: 使用 `GetOrCreate<T>()` 检查 ModelDb
**验证**: 进入战斗成功，无异常

### Case 2: 自伤功能失效
**症状**: DiscontinuousPulse 不扣血
**尝试**: `CommonActions.LoseHp` (不存在), `CreatureCmd.LoseHp` (不存在)
**解决**: 反射找到 `CreatureCmd.SetCurrentHp`，直接设置 HP
**验证**: 打出卡牌后HP减少

### Case 3: 奖励卡池空
**症状**: 战后卡死，日志显示 `couldn't generate a valid rarity`
**根因**: 所有卡牌都是 `CardRarity.Basic`（不参与奖励）
**修复**: 创建3张 `CardRarity.Common` 卡牌
**验证**: 战后正常显示奖励选择

### Case 4: Power不触发
**症状**: BloodlossPower 挂上了但回合开始不扣血
**根因**: 空实现，未重写生命周期钩子
**修复**: 重写 `AfterSideTurnStart` 方法
**验证**: 敌人回合开始时HP减少

## 工具链

- **IDE**: Rider / VS 2022
- **反编译**: dnSpy (sts2.dll), GDRETools (Godot项目)
- **日志查看**: Notepad++ / VS Code
- **反射工具**: 自建 .NET 9 Console项目
- **在线搜索**: GitHub Wiki + 中文教程站

## 经验总结

1. **先读日志再猜测** - 异常堆栈是第一手资料
2. **参考原版实现** - 反编译查看同类卡牌/遗物/Power
3. **小步快跑** - 每修复一个问题立即验证，避免叠加错误
4. **缓存 GetOrCreate模式** - 避免 ModelDb 重复注册
5. **Command都需.Execute()** - 构建器模式要显式执行
6. **Basic卡不进奖励池** - 至少准备3张Common卡
7. **Power要重写钩子** - 空类不会自动触发逻辑
