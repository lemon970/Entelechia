# Entelechia Mod — 完整交接文档

**写作时间：** 2026-07-09（代码部分最后更新）  
**写作目的：** 供新会话/新 agent 无缝接手，不依赖任何旧对话历史。  
**项目路径：** `D:\desktop\mod\Entelechia`

---

## 零、代码/技术状态（2026-07-09 最新）

### 0.1 当前状态总览

| 功能 | 状态 |
|------|------|
| 角色出现在角色选择界面 | ✅ 正常 |
| 战斗进入（HP 50、卡牌、遗物正确） | ✅ 正常 |
| Harmony 补丁全部加载 | ✅ 已修复 |
| 战斗中角色立绘显示 | ⚠️ **最后一次构建已部署但未经用户测试** |
| 卡牌/技能/遗物图片 | ❌ 大部分未打包 |
| 动画 | ❌ 未开始 |

### 0.2 本次会话修复的三个根本问题

**问题 1：`harmony.PatchAll()` 崩溃 → 角色根本没有注册**

- 文件：`EntelechiaCode/CombatPatches.cs` 第 179 行
- 原因：`EntelechiaCardCanPlayReasonPatch.Postfix` 参数名为 `source`，游戏 v0.108.0 已将其改名为 `preventer`。Harmony 按参数名匹配，导致 `PatchAll()` 整体抛异常，`Initialize()` 后续的 `ApplyModelDbFallback` 从未执行——角色没有注册进 ModelDb，所以根本不会出现。
- **已修复**：`source` → `preventer`。

**问题 2：`StringExtensions.cs` 所有路径用了 `Path.Join()`**

- 在 Windows 上 `Path.Join("res://X", "y")` → `res://X\y`（反斜杠），Godot 无法识别。
- **已修复**：全部改为字符串插值 `$"{MainFile.ResPath}/images/..."`。

**问题 3：`idle.png` 不在 PCK 里**

- 原因 A：项目里存在 `entelechia.tscn` → BSchneppe.StS2.PckPacker 跳过全部打包。
- 原因 B：`idle.png` 没有经过 Godot headless `--import`，无 `.import` sidecar。
- **已修复**：把 `.tscn` 移出到 `scenes_backup/`，对 `idle.png` 执行 headless import。

### 0.3 立绘显示问题——调查记录

游戏日志（`godot.log`）确认：

```
[CreateCustomVisuals] path=res://Entelechia/images/character/idle.png
[WARN] Asset not cached: res://Entelechia/images/character/idle.png
[BaseLib] Creating NCreatureVisuals from resource CompressedTexture2D
[BaseLib] Creating NCreatureVisuals from Texture2D
[CreateCustomVisuals] success
```

`GodotUtils.CreatureVisualsFromImage` 成功执行并返回了 `NCreatureVisuals`。  
但角色仍然不可见——根本原因：**GodotUtils 把 `Sprite2D` 嵌套在子节点内部，不是直接子节点**。之前的代码只遍历直接子节点，找不到 `Sprite2D`，所以缩放没有应用，图片以 2560×2560 全尺寸显示，导致不可见（超出屏幕/摄像机裁剪）。

此外，当使用 `new NCreatureVisuals() + AddChild(Sprite2D)` 手动构建时（Image #3 显示角色出现了），游戏报错：
```
ERROR: Node not found: "%IntentPos"
ERROR: Node not found: "%CenterPos"
```
说明 `NCreatureVisuals._Ready()` 要求内部有 `%IntentPos` 和 `%CenterPos` UniqueNameInOwner Marker2D 节点。`GodotUtils.CreatureVisualsFromImage` 会创建这些节点，手动构建不会。

### 0.4 当前部署代码（未测试）

`EntelechiaCode/Character/Entelechia.cs`：

```csharp
public override NCreatureVisuals CreateCustomVisuals()
{
    var path = $"{MainFile.ResPath}/images/character/idle.png";
#pragma warning disable CS0618
    var visuals = GodotUtils.CreatureVisualsFromImage(path);
#pragma warning restore CS0618
    if (visuals == null) return base.CreateCustomVisuals();

    // GodotUtils nests the Sprite2D — search the full subtree
    var sprite2D = FindSprite2D(visuals);
    if (sprite2D != null)
    {
        sprite2D.Scale = new Vector2(0.13f, 0.13f);
        sprite2D.Position = new Vector2(0f, -170f);
    }
    return visuals;
}

private static Sprite2D? FindSprite2D(Node node)
{
    foreach (var child in node.GetChildren())
    {
        if (child is Sprite2D s) return s;
        var found = FindSprite2D(child);
        if (found != null) return found;
    }
    return null;
}
```

**此版本已编译部署，但用户放弃测试。** 逻辑上是正确的——如果 `GodotUtils` 确实把 `Sprite2D` 嵌套在某个子节点中，`FindSprite2D` 递归查找会找到它并正确设置缩放。

### 0.5 如果上述方法仍然失败

**备选方案（更可靠）：用 `.tscn` + `NodeFactory`**

BaseLib 官方推荐方式，可以完全解决 `%IntentPos`/`%CenterPos` 问题：

1. 把 `scenes_backup/entelechia.tscn` 移回 `Entelechia/Entelechia/scenes/`
2. 场景内容（最小结构）：
   ```
   [NCreatureVisuals] 根节点
   ├── CenterPos  [Marker2D, unique_name_in_owner=true, position=(0,-100)]
   ├── IntentPos  [Marker2D, unique_name_in_owner=true, position=(0,-220)]
   └── Visuals    [Sprite2D, scale=(0.13,0.13), position=(0,-170), texture=idle.png]
   ```
3. 解决 PckPacker 跳过 `.tscn` 的问题：用 `dotnet publish` 代替 `dotnet build`（需要 Steam 运行，已有 `steam_appid.txt` 在游戏目录，内容 `2868840`）
4. 代码改为：
   ```csharp
   return NodeFactory<NCreatureVisuals>.CreateFromResource(
       $"{MainFile.ResPath}/scenes/entelechia.tscn");
   ```

### 0.6 构建与部署

```powershell
cd D:\desktop\mod\Entelechia
dotnet build Entelechia.csproj -c Release
```

自动复制到：`D:\steam\NEW\steamapps\common\Slay the Spire 2\mods\Entelechia\`

**约束**：项目内不能有 `.tscn` 文件，否则 PckPacker 跳过全部打包。

### 0.7 诊断命令

```powershell
# 查看最新游戏日志中的 Entelechia 相关信息
Select-String "Entelechia|CreateCustom|IntentPos|CenterPos|idle" `
    "C:\Users\34062\AppData\Roaming\SlayTheSpire2\logs\godot.log" | Select-Object -Last 30
```

---

---

## 一、项目概述

这是一个为 **Slay the Spire 2（STS2）** 制作的 Mod，主角是 **隐德来希（Entelechia）**，原型来自明日方舟角色。Mod 框架基于 **BaseLib-StS2**，使用 Godot 引擎。

图片资源分两大块：
1. **卡牌图片（card portraits / powers / relics）** — 已基本完成
2. **战斗动画角色（battle animation）** — 进行中，尚未导入正式 mod 目录

---

## 二、目录结构

```
D:\desktop\mod\
├── pic\                          ← 原始参考素材（不可修改）
│   ├── 立绘_隐德来希_1.png          主身份参考，黑红短外套、镰刀、尖耳、红眼
│   ├── 立绘_隐德来希_2.png          战斗姿态参考，大型镰刀
│   ├── 立绘_隐德来希_skin1.png      备用皮肤（蓝黑），不作默认身份
│   ├── 头像_隐德来希.png
│   ├── 技能图_*.png               技能卡面风格参考（3张小图）
│   ├── 道具_带框_隐德来希的信物.png  起始遗物参考
│   └── uniequip_002/003_etlchi.png 心脏蜡烛、玫瑰故事册参考
│
└── Entelechia\
    ├── IMAGE_ART_PLAN.md          ← 完整图片规划文档（命名规则/尺寸/所有卡牌设计）
    ├── HANDOVER.md                ← 本文档
    ├── docs\superpowers\specs\
    │   ├── 2026-07-08-entelechia-character-art-design.md
    │   └── 2026-07-08-entelechia-battle-animation-redesign.md
    ├── Entelechia\images\      ← 游戏实际读取路径（已同步的文件）
    └── generated_art\             ← 工作区（全部产出物）
        ├── card_portraits\big\    1000×760，57张
        ├── card_portraits\small\  250×190，57张
        ├── powers\big\            256×256，4张
        ├── powers\small\          64×64，4张
        ├── relics\                blood_demon_replete 3件套
        ├── character\             动画工作文件（层、rig、脚本）
        └── review\                所有验收文件、contact sheet、进度日志
```

---

## 三、角色身份约束（硬性，不可违反）

来源：`IMAGE_ART_PLAN.md` + `docs/superpowers/specs/2026-07-08-entelechia-character-art-design.md`

**必须保持：**
- 短黑发、红眼、尖耳，带轻微非人感（萨卡兹）
- 黑色短外套，红色内衬，暗银配件，礼服裙摆
- 巨型月牙镰刀是主武器
- 气质：冷静、仪式化、优雅残酷——不是狂暴嗜血

**禁止：**
- 长发、裙装、重甲、蓝黑肤色、通用吸血鬼披风、翅膀
- 写实厚涂、3D 渲染、过度发光、画面内文字水印
- 重新设计脸型、发型、服装

**卡面视觉语言约束：**
- 血液 = 精确操控的线状/结晶，不是失控喷溅
- 心脏蜡烛 = 生命之火符号，不是普通火焰
- 每张卡面必须有至少一个独立差异点（主体/动作/镜头/场景/道具/特效）

---

## 四、文件命名与尺寸规则

来源：`IMAGE_ART_PLAN.md`

| 类型 | 路径 | 尺寸 |
|------|------|------|
| 卡牌小图 | `Entelechia/images/card_portraits/{id}.png` | 250×190 |
| 卡牌大图 | `Entelechia/images/card_portraits/big/{id}.png` | 1000×760 |
| Power 小图 | `Entelechia/images/powers/{id}.png` | 64×64 |
| Power 大图 | `Entelechia/images/powers/big/{id}.png` | 256×256 |
| 遗物小图 | `Entelechia/images/relics/{id}.png` | 94×94 |
| 遗物大图 | `Entelechia/images/relics/big/{id}.png` | 256×256 |
| 遗物轮廓 | `Entelechia/images/relics/{id}_outline.png` | 94×94 |

---

## 五、卡牌/图标资源现状（截至 2026-07-09）

### 5.1 已同步到游戏目录（Entelechia\images）

**卡牌（card_portraits）：已同步 3 张**
- `crimson_merge`（绯红壁合）
- `blood_mend`（血愈）
- `crimson_shield`（深红壁垒）

**Powers：已同步 4 张（全部核心 Power）**
- `blood_harvest_power`
- `bloodloss_power`
- `heart_candle_power`
- `blood_speed_power`（已重设计，旧斩击版已移至 rejected）

**遗物：已同步 1 套**
- `blood_demon_replete`（小图/大图/轮廓全套）

**CharUI：已同步**
- `char_select_char_name.png`（132×195）
- `char_select_char_name_locked.png`（132×195）
- `character_icon_char_name.png`（85×85）
- `map_marker_char_name.png`（49×64）

### 5.2 generated_art 中已生成、未同步到游戏目录

`generated_art/card_portraits/big` 和 `small` 各有 **57 张**，包含以下全部卡牌：

**Basic（起始牌组）：** blood_blade, blood_veil, rose_trail, discontinuous_pulse

**Common 攻击：** blood_strike, blood_surge, blood_claw_slash, blood_drain, blood_splash, candle_scorch, counter_slash, crimson_lash, lacerate, rose_thorn

**Common 技能/能力：** blood_shield, blood_mend, blood_infect, heart_brand, blood_haste, blood_offering, crimson_shield, blood_mist, blood_pulse, blood_fragrance, autophagy, rose_step, suture, clot_instinct

**Uncommon 攻击：** blood_dissect, blood_storm, blood_sweep, crimson_merge, soul_blood_draw

**Uncommon 技能/能力：** blood_borrow, blood_debt_settlement, blood_overload, blood_to_candle, clotting_backflow, clotting_barrier, crimson_embers, entelechia_bloodletting, heart_candle_ritual, sanguine_rite, blood_feast, candle_ember, pain_conversion

**Rare 攻击：** blood_frenzy, farewell_finale, red_candle_all, spirit_and_desire_farewell

**Rare 技能/能力：** blood_rebuild, crimson_sacrifice, immortal_bloodline, revive_candle, blood_clan_court, blood_demon_form, eternal_replete

**其他：** blood_haste, blood_veil, blood_blade, blood_mend, crimson_merge 等（含系统代表牌）

### 5.3 仍需同步（批量同步任务）

57 张卡牌中，目前游戏目录里只有 3 张，**剩余 54 张需要执行同步**。同步前须确认每张的生成质量符合验收标准。

### 5.4 画廊验收结论（来自 GALLERY_REVIEW.md）

**Priority 1 重做——已完成：** crimson_merge, blood_mend, crimson_shield（已同步）

**Priority 2 重做队列——未完成（生成失败或待重做）：**

| 目标 | 问题 | 状态 |
|------|------|------|
| `blood_haste` | 读起来像血线而非速度感 | 生成网络错误，未完成 |
| `blood_surge` | 自伤血本不明显 | 生成网络错误，未完成 |
| `blood_splash` | 群体+采血标记太小 | 生成网络错误，未完成 |
| `blood_mist` / `blood_fragrance` | 机制不清晰 | 待修改 |
| `clot_instinct` | 采血触发盾感弱 | 待修改 |
| `blood_overload` | 速度残影弱 | 待修改 |
| `clotting_barrier` / `clotting_backflow` | 像宝石堆，机制不清 | 待修改 |
| `crimson_embers` | 火与盾关系模糊 | 待修改 |
| `candle_ember` | 余烬落进伤口感弱 | 待修改 |
| `farewell_finale` | 三种标记（血珠/血痕/心脏蜡烛）太小 | 待修改 |
| `spirit_and_desire_farewell` | 风险读成普通斩击场景 | 待修改 |
| `blood_rebuild` | 过于人物海报感 | 待修改 |
| `blood_demon_form` | 略微人物海报感 | 待修改 |

### 5.5 尚未生成的 Power 图标

以下 Power 图标在 `IMAGE_ART_PLAN.md` 中有规划，但 `generated_art/powers` 目录目前只有 4 个，**以下 14 个仍未生成：**

blood_feast_power, candle_ember_power, blood_demon_form_power, eternal_replete_power, rose_step_power, clot_instinct_power, pain_conversion_power, clotting_barrier_power, immortal_bloodline_power, blood_clan_court_power, bloodletting_strength_power, blood_debt_strength_power, crimson_ward_power, ember_bloodline_power

---

## 六、战斗动画资源现状（截至 2026-07-09）

**重要：所有战斗动画资源均在 `generated_art` 下，尚未导入 mod，`formal_import_allowed = false`。**

### 6.1 制作历史摘要

| 阶段 | 结论 |
|------|------|
| AI 重绘候选（Batch 1） | 拒绝——脸/服饰漂移过大 |
| 原图裁剪候选 | 接受——作为身份锁定底图 |
| VFX 叠加层（vfx_layers） | 生成但未推进为最终方向 |
| 水平横幅战斗图 | 拒绝——读作卡面，不读作战斗单元 |
| battle_unit_base_v2 | 接受——原图裁剪，1024×1536 RGBA |
| battle_layers_v1 | 生成——5 层粗层，不足以做最终动画 |
| 镰刀反复尝试（hybrid v1/v2, reference v1/v2/v3） | v1/v2 拒绝；v3 覆盖改善但仍不完整 |
| 人工标注镰刀（manual_scythe_annotation_marked_v1.png） | 用户完成标注，已执行提取 |
| 人工标注人物层（manual_person_layers_annotation_marked_v1.png） | 用户完成标注（头部重新标注），已执行提取 |
| battle_person_manual_v1 | 4 层人工提取（body_legs / torso / coat_cloth / head_hair） |
| battle_scythe_manual_v1 | 镰刀人工提取（scythe_reference_from_manual_v1.png） |
| battle_head_cleanup_v1/v2/v3 | v3 基于用户标注完成，候选已清理（alpha 6052px） |
| battle_safe_layer_candidates_v1 | 4 个安全粗层候选（body_legs / coat_back / scythe_full / torso） |
| safe_rig_metadata_v1 | 4 层 rig（无 head） |
| safe_rig_metadata_v2 | 5 层 rig（含 V3 清理版 head） |
| motion_check_v1 | 4 层动作检测，attack/cast 太弱 |
| motion_check_v2 | 5 层（含 head），仍是 review-only |

### 6.2 当前接受的层级体系

绘制顺序：`body_legs → coat_back → scythe_full → torso → head`

| 层 | 文件 | 状态 |
|----|------|------|
| body_legs | `battle_safe_layer_candidates_v1/body_legs_candidate.png` | 接受 |
| coat_back | `battle_safe_layer_candidates_v1/coat_back_candidate.png` | 接受 |
| scythe_full | `battle_safe_layer_candidates_v1/scythe_full_candidate.png` | 接受（粗层，不可分割） |
| torso | `battle_safe_layer_candidates_v1/torso_candidate.png` | 接受 |
| head | `battle_head_cleanup_v3/head_cleanup_candidate_v3.png` | 接受（候选，待最终确认） |

**明确排除（需手工标注后才可切）：** front_arm, back_arm, grip_hand, scythe_handle, scythe_blade, coat_front, hair_front, hair_back

### 6.3 当前 Rig 规格（safe_rig_metadata_v2.json）

画布：1024×1536，`formal_import_allowed: false`

| 动画 | 时长 | 关键帧数 | 旋转幅度（scythe_full） |
|------|------|----------|------------------------|
| idle | 1800ms | 3 | ±0.8° |
| attack | 620ms | 4 | -5° → +10° |
| cast | 860ms | 3 | -2° |
| hurt | 320ms | 3 | -1° |
| die | 1200ms | 3 | -4° → -5° |

### 6.4 质量门禁（BATTLE_ANIMATION_QUALITY_REDESIGN_V1.md）

**当前五粗层方案被明确判定为不合格最终动画质量。** 问题：
- 层太少，头发/袖子/手臂/握持/裙摆/镰刀刃/柄无法独立运动
- 动作幅度仍然偏小，attack/cast 读感太弱（用户反馈：V2 contact sheet 幅度不够）
- 无 hidden surface 修补
- VFX 是装饰而非结构性

正确生产路线（Route A，已选定）：需要细分层，按 `BATTLE_ANIMATION_QUALITY_REDESIGN_V1.md` 中的完整 18 层规划执行。

### 6.5 待执行的下一步工作（两个并列方向）

**Phase 1：增大 V2 动作幅度（无需标注，可立即执行）**

修改 `safe_rig_metadata_v2.json`，参考幅度：
- attack：scythe_full 旋转 ±10° → ±18°，torso ±4° → ±7°，body_legs ±2° → ±4°
- cast：scythe_full -2° → -5°，torso 上抬 -2° → -4°
- hurt：scythe_full -1° → -3°，head -3° → -5°
- die：整体旋转 → -8° 到 -10°，root 下移更多

生成 V3 motion check contact sheet 供审查。

**Phase 2：细分层（需要用户手工标注——必须停止并通知用户）**

待切割：front_arm, back_arm, grip_hand, scythe_handle, scythe_blade, coat_front, hair_front, hair_back

每个细分层在自动切割之前必须停止，通知用户，等待手工标注文件。

### 6.6 镰刀切割诊断（SCYTHE_RECUT_DIAGNOSIS_V1.md）

历史失败原因和正确切割区域已记录在 `generated_art/review/SCYTHE_RECUT_DIAGNOSIS_V1.md`。核心要点：
- 右侧服装/裙摆尾部（x=560-910, y=820-980）是服装层，不是镰刀
- 上部半圆刃（x=135-585, y=330-610）历次遮罩都过窄
- 手握处（x=230-365, y=575-770）需要激进包含
- 脸左下连接区（x=455-555, y=560-670）是镰刀本体，不可用大椭圆遮罩保护脸时误删

### 6.7 人工标注文件（不可删除）

| 文件 | 内容 | 路径 |
|------|------|------|
| `manual_scythe_annotation_marked_v1.png` | 用户标注的镰刀边界（红色区域均为镰刀本体） | `generated_art/review/` |
| `manual_person_layers_annotation_marked_v1.png` | 用户标注的人物层级（含头部重新标注） | `generated_art/review/` |
| `battle_head_cleanup_v1_contact - 副本.png` | 头部清理标注（含用户标注绿色保留/红色删除） | `generated_art/review/` |

---

## 七、关键文件索引

### 进度/计划文档

| 文件 | 作用 |
|------|------|
| `IMAGE_ART_PLAN.md` | 全部图片规划（所有卡牌/Power/遗物的命名、尺寸、设计要求） |
| `generated_art/review/CHARACTER_ART_PROGRESS.md` | 所有批次进度追加日志（最完整的历史记录） |
| `generated_art/review/ACCEPTANCE_LOG.md` | 验收批次日志（gallery review 结论） |
| `generated_art/review/GALLERY_REVIEW.md` | 全量图库审查结果（哪些接受/哪些需重做） |
| `generated_art/review/BATTLE_ANIMATION_QUALITY_REDESIGN_V1.md` | 战斗动画质量门禁（当前五层方案不合格，Route A 细分层计划） |
| `generated_art/review/BATTLE_EXPORT_PLAN_V1.md` | 导出计划（Task 4 完成，Task 5 暂停中） |
| `generated_art/review/BATTLE_ANIMATION_BLUEPRINT_V1.md` | 动画蓝图（完整层级树、Pivot 坐标表、动画时序） |
| `generated_art/review/SCYTHE_RECUT_DIAGNOSIS_V1.md` | 镰刀切割诊断（坐标和历史失败原因） |
| `generated_art/review/REDO_DISPATCH.md` | 重做派遣记录 |

### 当前最新产出物

| 文件 | 说明 |
|------|------|
| `generated_art/character/battle_safe_rig_v2/safe_rig_metadata_v2.json` | 当前 5 层 rig（review-only） |
| `generated_art/review/battle_safe_motion_check_v2_contact.png` | 最新动作检测 contact sheet（用户审查后说幅度不够） |
| `generated_art/character/battle_head_cleanup_v3/head_cleanup_candidate_v3.png` | 头部候选（基于用户标注清理） |
| `generated_art/character/battle_scythe_manual_v1/scythe_reference_from_manual_v1.png` | 镰刀人工提取参考 |
| `generated_art/character/battle_person_manual_v1/` | 人物各层人工提取（body_legs / torso / coat_cloth / head_hair） |
| `generated_art/character/render_battle_safe_motion_check_v2.py` | 渲染脚本 |

---

## 八、工作方式约束（硬性规则）

1. **小步快走**，每轮只处理一个小批次（4-6 张图或 1 个 contact sheet）
2. **需要手工标注时必须停止并通知用户**，即使在 goal 模式也要停止，不得继续自动执行
3. **不导入 mod 正式目录**（`Entelechia/Entelechia/images`），除非用户明确批准
4. 红色系带/握持区经用户确认均为镰刀组成部分，不得删除
5. 每完成一小批，追加记录到 `CHARACTER_ART_PROGRESS.md`
6. 不使用旧对话历史推断状态，只读项目文件

---

## 九、当前任务优先级

按可执行性排序：

1. **立即可做（无需标注）：** 修改 `safe_rig_metadata_v2.json` 增大动作幅度 → 生成 V3 motion check contact sheet → 等用户审查
2. **需先审查 V3 contact sheet：** 用户决定是否进入细分层（Phase 2）
3. **需手工标注后才可做：** 细分 front_arm / back_arm / scythe_handle / scythe_blade 等
4. **卡牌资源后续：** Priority 2 重做队列（blood_haste / blood_surge / blood_splash 等 13 张）；14 个未生成 Power 图标
5. **最后：** 所有验收通过的卡牌批量同步到游戏目录

---

## 十、已知风险点

| 风险 | 影响 | 处置 |
|------|------|------|
| `scythe_full` 作为整体层限制攻击动作 | 中 | 保持短促挥击，只在用户批准新标注后才分割 |
| 细分层自动切割遗漏像素 | 高 | 每个高风险边界先出 annotation board，等用户确认再切 |
| 会话上下文膨胀（历史问题） | 高 | 每批完成后立刻落盘，不在单轮做多个批次 |
| Priority 2 生成质量 | 中 | 生成后必须先出 contact sheet，人工确认再同步 |
| 14 个 Power 图标缺失 | 低（当前未用到） | 按计划生成，验收后同步 |

---

*本文档基于 2026-07-09 03:36 时刻的项目文件状态生成，后续每次重大工作节点应追加更新。*
