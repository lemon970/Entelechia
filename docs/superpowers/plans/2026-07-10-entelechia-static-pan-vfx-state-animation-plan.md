# Entelechia Static Pan VFX State Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add combat animation feedback for Entelechia using only the accepted standing portrait's transform and separate lightweight VFX sprites.

**Architecture:** Keep the current manual `NCreatureVisuals` tree and add a small code-driven visual driver around it. The body remains one full `Sprite2D`; all animation is translate, scale, rotate, tint, alpha, and temporary VFX sprites. Every effect resets to the verified baseline and must not reintroduce map-layer leakage.

**Tech Stack:** C#, Godot `NCreatureVisuals`, `Sprite2D`, `Node2D`, `Tween`, existing STS2/BaseLib combat card and HP tracking paths.

---

## Scope Lock

This plan intentionally replaces the abandoned layer-rig route.

- Use `D:\desktop\mod\Entelechia\Entelechia\images\character\idle.png` as the only character body.
- Do not redraw Entelechia.
- Do not split face, scythe, clothes, hair, legs, hands, or weapon layers.
- Do not use image generation, image2, or model-based image review.
- Do not edit PNG assets in this implementation pass.
- Do not restore `ZIndex = 10` on the main portrait sprite.
- Do not use `speed_lines.png` in the first visible pass.
- Do not implement all states in one batch.

## Current Verified Baseline

Current stable implementation is in:

- `D:\desktop\mod\Entelechia\EntelechiaCode\Character\Entelechia.cs`

Important existing values:

- Visual root: `EntelechiaVisuals : NCreatureVisuals`
- Main portrait node: `%Visuals : Sprite2D`
- Bounds node: `%Bounds : Control`
- Marker nodes: `%IntentPos`, `%CenterPos`
- Main portrait scale: `0.36`
- Main portrait position: about `(5.22, -147.24)`
- Trim content bounds: `x=174..821`, `y=361..1177`
- Default main sprite z-index is required. Do not set positive z-index unless a later test proves a local child VFX needs it.

Latest stable visual record:

- `D:\desktop\mod\Entelechia\generated_art\review\BATTLE_VISUAL_ZINDEX_FIX_20260710_V7.md`

## Animation State Design

### Idle

Purpose: keep the character alive without attracting attention.

Motion:

- Loop duration: `2.0s`
- Body position: base -> `(base.X, base.Y - 2)` -> `(base.X, base.Y + 1)` -> base
- Body scale: base -> `(base.X * 1.002, base.Y * 0.998)` -> base
- Rotation: `0`

VFX:

- None in Phase 2.

Acceptance:

- Idle does not change slot alignment.
- Idle does not move beyond current `Bounds`.
- Ending combat leaves no portrait or VFX on the map.

### Attack

Purpose: make attack cards read as a scythe strike without redrawing the scythe.

Motion:

- Total duration: `0.44s`
- Wind-up: `0.08s`, body moves `(-5, -2)` and scales to `0.995`
- Strike: `0.10s`, body moves `(22, 4)`, scales to `1.035`, rotates `1.5deg`
- Hold/flash: `0.06s`
- Return: `0.20s`, body returns to base transform

VFX:

- `slash_arc.png`
  - show for `0.22s`
  - position around `(base.X + 55, base.Y - 110)`
  - scale around `0.30`
  - alpha `0 -> 0.85 -> 0`
  - rotation aligned visually to scythe sweep, initial value around `-12deg`
- Optional `impact_burst.png` only for later heavy/multi-hit variants.

Variants:

- Normal attack: one lunge and one slash arc.
- Multi-hit attack: one lunge, then 2-4 small slash flickers while body stays near base.
- AoE attack: same lunge but slash arc scale up to about `0.38`, no extra body travel.

Acceptance:

- Repeated attack calls do not accumulate offset.
- Body never leaves the player slot.
- Slash arc does not cover hand cards, energy, or enemy intent UI.

### Cast / Skill / Power

Purpose: show ritual, blood, candle, or power activation without implying a weapon hit.

Motion:

- Total duration: `0.60s`
- Lift: body moves `(0, -7)` over `0.18s`
- Pulse: scale `1.018` for `0.14s`
- Return: base transform over `0.28s`
- Tint: main sprite modulate briefly to warm red, max about `Color(1.08, 0.82, 0.88, 1)`

VFX:

- `impact_burst.png` as a soft torso/feet pulse
  - position around `(base.X + 15, base.Y - 85)`
  - scale around `0.22`
  - alpha `0 -> 0.35 -> 0`
- `blood_drops.png` only for blood/HP cost themed skill cards, not generic skills.

Acceptance:

- Cast is visually different from attack: no forward lunge and no slash arc by default.
- Tint is restored after completion.
- No VFX remains visible after the sequence.

### Block / Guard

Purpose: provide defensive feedback without introducing shield artwork.

Motion:

- Total duration: `0.34s`
- Settle back: body moves `(-7, 2)` and scales `0.993` over `0.10s`
- Rebound: body moves `(2, -1)` over `0.08s`
- Return: base transform over `0.16s`

VFX:

- `impact_burst.png` as a low-alpha compact pulse
  - position around `(base.X, base.Y - 65)`
  - scale around `0.16`
  - alpha max `0.22`

Acceptance:

- Does not look like damage recoil.
- Does not use red damage tint.
- Does not require any new shield or barrier asset.

### Hurt

Purpose: communicate enemy damage while preserving the accepted portrait.

Motion:

- Total duration: `0.26s`
- Recoil: body moves `(-14, -2)` over `0.07s`
- Snap: body moves `(4, 1)` over `0.06s`
- Return: base transform over `0.13s`
- Tint: red hit tint max for `0.07s`, then restore

VFX:

- `impact_burst.png`
  - position around `(base.X - 5, base.Y - 90)`
  - scale around `0.20`
  - alpha max `0.55`
- `blood_drops.png` is not used in the first hurt pass. Keep it for HP cost and death.

Acceptance:

- Hurt does not resemble attack wind-up.
- Repeated enemy hits do not leave the body tinted.
- Interrupting hurt with another effect still resets to a valid baseline.

### HP Cost / Self-Blood

Purpose: distinguish Entelechia paying HP from enemy damage.

Motion:

- Total duration: `0.42s`
- Contract: scale to `0.988`, body moves `(0, 4)` over `0.12s`
- Recovery pulse: scale to `1.008`, body moves `(0, -2)` over `0.10s`
- Return: base over `0.20s`
- Tint: controlled dark red pulse, lower intensity than hurt.

VFX:

- `blood_drops.png`
  - position around `(base.X + 18, base.Y - 55)`
  - scale around `0.20`
  - alpha max `0.45`
  - fade out by the end of the animation

Trigger rule:

- `TryPayHpCost(...)` should trigger HP cost feedback when the HP payment succeeds.
- HP cost animation has higher priority than generic cast, lower priority than death.

Acceptance:

- The player can tell HP was paid by the character.
- It does not look like being struck by an enemy.
- No blood VFX persists after the effect.

### Heal / Restore

Purpose: show recovery without using generic green healing art.

Motion:

- Total duration: `0.46s`
- Lift: body moves `(0, -5)` and scales `1.010` over `0.14s`
- Calm hold: `0.10s`
- Return: base over `0.22s`

VFX:

- `impact_burst.png`
  - position around `(base.X, base.Y - 80)`
  - scale around `0.18`
  - alpha max `0.28`
  - tint pale red/white only if simple `Modulate` tint works reliably

Acceptance:

- Heal is calmer than attack and hurt.
- No green visual language is introduced.
- It does not trigger from ordinary block gain unless a later gameplay decision says it should.

### Death

Purpose: provide a clean end state and avoid any return to idle.

Motion:

- Total duration: `1.10s`
- Body drifts `(0, 28)` and rotates `3deg`
- Body alpha fades `1 -> 0`
- Do not restart idle after death.

VFX:

- `blood_drops.png`
  - position around `(base.X + 10, base.Y - 45)`
  - scale around `0.26`
  - alpha max `0.50`, then fade to `0`
- Optional first-frame `impact_burst.png` pulse if Phase 5 review says death feels too empty.

Acceptance:

- Death never returns to idle.
- After battle or scene transition, all VFX nodes are hidden and alpha-reset.
- Map screen remains clean.

## File Structure

### Modify

- `D:\desktop\mod\Entelechia\EntelechiaCode\Character\Entelechia.cs`
  - Phase 1 only: add `VfxRoot` and hidden VFX sprites to the existing manual visual tree.
  - Later: register the visual root with a small driver.

- `D:\desktop\mod\Entelechia\EntelechiaCode\Cards\EntelechiaCard.cs`
  - Phase 2 only: trigger attack animation after successful `ExecuteAttack(...)`.
  - Phase 4 only: trigger HP-cost animation after successful `TryPayHpCost(...)`.

- `D:\desktop\mod\Entelechia\EntelechiaCode\CombatPatches.cs`
  - Phase 4 only: connect heal/hurt events after identifying a reliable event source.

### Create

- `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaCombatAnimationDriver.cs`
  - Owns base transforms, VFX sprite references, current tween, reset, and named effect playback.

- `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaAnimationKind.cs`
  - Enum values: `Idle`, `Attack`, `Cast`, `Block`, `Hurt`, `HpCost`, `Heal`, `Death`.

- `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaAnimationVariant.cs`
  - Enum values: `Normal`, `MultiHit`, `Area`, `Blood`, `Power`, `Heavy`.

### Review Records

Write one record after every phase:

- `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE1_SKELETON_20260710_V1.md`
- `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE2_IDLE_ATTACK_20260710_V1.md`
- `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE3_CAST_BLOCK_20260710_V1.md`
- `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE4_HP_EVENTS_20260710_V1.md`
- `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE5_DEATH_CLEANUP_20260710_V1.md`

## Dependency Order

1. Add hidden VFX nodes and reset support.
2. Add animation driver with no triggers.
3. Enable idle and attack only.
4. Add cast and block.
5. Add HP cost, hurt, and heal.
6. Add death and lifecycle cleanup.

Do not skip the hidden-node skeleton phase. Previous failures were caused by visual tree shape, unique node lookup, positioning, and z-order/lifecycle behavior.

## Task List

### Task 1: Hidden VFX Skeleton

**Files:**

- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\Character\Entelechia.cs`
- Record: `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE1_SKELETON_20260710_V1.md`

- [x] **Step 1: Add `VfxRoot : Node2D` under `EntelechiaVisuals`.**

Expected tree:

```text
EntelechiaVisuals : NCreatureVisuals
  Bounds : Control
  Visuals : Sprite2D
  IntentPos : Marker2D
  CenterPos : Marker2D
  VfxRoot : Node2D
    SlashArc : Sprite2D
    ImpactBurst : Sprite2D
    BloodDrops : Sprite2D
```

- [x] **Step 2: Load existing VFX textures.**

Use these exact resource paths:

```csharp
$"{MainFile.ResPath}/images/vfx/slash_arc.png"
$"{MainFile.ResPath}/images/vfx/impact_burst.png"
$"{MainFile.ResPath}/images/vfx/blood_drops.png"
```

- [x] **Step 3: Keep every VFX sprite hidden by default.**

Each VFX sprite must start with:

```csharp
Visible = false;
Modulate = new Color(1f, 1f, 1f, 0f);
Scale = Vector2.One;
Rotation = 0f;
```

- [x] **Step 4: Log diagnostics.**

Log `%VfxRoot`, `%SlashArc`, `%ImpactBurst`, and `%BloodDrops` in `LogVisualDiagnostics(...)`.

- [x] **Step 5: Verify build.**

Run:

```powershell
dotnet build D:\desktop\mod\Entelechia\Entelechia.csproj -c Release
```

Expected:

- Build completes with `0` errors.
- Existing nullable/BaseLib warnings may remain.

- [x] **Step 6: Manual/log check.**

Expected:

- Combat portrait still appears in the verified position.
- No visible VFX appears yet.
- Ending combat to map still leaves no full portrait above the map.

Runtime log checked on `2026-07-10 01:01`: `20260710-vfx-skeleton-phase1` loaded, `%VfxRoot`, `%SlashArc`, `%ImpactBurst`, and `%BloodDrops` resolved, all VFX sprites were `visible=False`, alpha `0`, and `zIndex=0`.

### Task 2: Driver State and Reset

**Files:**

- Create: `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaCombatAnimationDriver.cs`
- Create: `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaAnimationKind.cs`
- Create: `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaAnimationVariant.cs`
- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\Character\Entelechia.cs`

- [x] **Step 1: Add enums.**

```csharp
namespace Entelechia.EntelechiaCode.Animation;

public enum EntelechiaAnimationKind
{
    Idle,
    Attack,
    Cast,
    Block,
    Hurt,
    HpCost,
    Heal,
    Death
}
```

```csharp
namespace Entelechia.EntelechiaCode.Animation;

public enum EntelechiaAnimationVariant
{
    Normal,
    MultiHit,
    Area,
    Blood,
    Power,
    Heavy
}
```

- [x] **Step 2: Driver stores baseline.**

Driver fields must include:

```csharp
private readonly Sprite2D _body;
private readonly Sprite2D _slashArc;
private readonly Sprite2D _impactBurst;
private readonly Sprite2D _bloodDrops;
private readonly Vector2 _baseBodyPosition;
private readonly Vector2 _baseBodyScale;
private readonly float _baseBodyRotation;
private readonly Color _baseBodyModulate;
private Tween? _activeTween;
private bool _dead;
```

- [x] **Step 3: Driver reset behavior.**

`ResetVisuals()` must:

- kill `_activeTween` if it exists;
- restore body position, scale, rotation, and modulate;
- hide every VFX sprite;
- set each VFX alpha to `0`;
- skip restarting idle if `_dead` is true.

- [x] **Step 4: Attach driver after manual visual creation.**

Do not change current placement math. Only add the driver after the current tree exists.

- [x] **Step 5: Verify no visible animation yet.**

Build and run one combat. Expected result is identical to Task 1.

Build completed on `2026-07-10 01:08` with `0` errors.

Runtime check after user smoke test loaded `20260710-driver-reset-phase2`, but it was not clean: `godot.log` contained repeated `System.ArgumentException` entries through `EntelechiaCombatAnimationDriver.InvokeGodotClassMethod` during `Node.SetName` / `AddChild`. Root cause: making the animation driver itself a custom Godot `Node` pushed it through the Godot C# bridge under STS2/MonoMod.

Follow-up fix built on `2026-07-10 01:19`: `EntelechiaCombatAnimationDriver` is now a plain C# object, while the scene tree keeps only a built-in `Node` named `%AnimationDriver` as a lookup marker. New diagnostic marker is `20260710-driver-plain-phase2`.

Task 2 Step 5 remains open until a fresh combat log shows `20260710-driver-plain-phase2` with no new `EntelechiaCombatAnimationDriver.InvokeGodotClassMethod` / `System.ArgumentException` entries after that marker.

Fresh runtime check completed on `2026-07-10 01:24`: `godot.log` loaded `20260710-driver-plain-phase2` five times, `%AnimationDriver` resolved as `AnimationDriver:Node`, `%Visuals` stayed at `(5.2200003, -147.24)` with scale `(0.36, 0.36)` and z-index `0`, all VFX sprites stayed hidden with alpha `0`, and both `EntelechiaCombatAnimationDriver.InvokeGodotClassMethod` and `System.ArgumentException` had count `0`.

Task 2 is verified. Next batch starts Task 3 with idle first, then attack API, before wiring card triggers.

### Task 3: Idle and Attack Prototype

**Files:**

- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaCombatAnimationDriver.cs`
- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\Cards\EntelechiaCard.cs`
- Record: `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE2_IDLE_ATTACK_20260710_V1.md`

- [x] **Step 1: Implement `PlayIdle()`.**

Use a looped tween with the Idle values listed above.

Implementation batch `20260710-idle-phase3a` built and deployed on `2026-07-10 01:27`. Idle is started from the built-in `%AnimationDriver` node's `TreeEntered` signal so `CreateTween()` is only called after the visual tree enters the scene. Runtime verification is still pending.

- [x] **Step 2: Implement `PlayAttack(EntelechiaAnimationVariant variant)`.**

Use the Attack values listed above. Start by calling `ResetVisuals()` unless `_dead` is true.

The callable attack API exists in `20260710-idle-phase3a`, but it is not wired to cards yet and has not been runtime-triggered.

- [x] **Step 3: Trigger attack from `ExecuteAttack(...)`.**

Trigger after `attack.Execute(context)` succeeds, not before. If no driver is found, gameplay must continue silently.

Read-only trigger research completed on `2026-07-10`:

- all shared card attacks converge on `EntelechiaCard.ExecuteAttack(...)`;
- the safe trigger point is immediately after `attack = await attack.Execute(context)`;
- `Owner.Creature.GetCreatureNode().Visuals` exposes the current `NCreatureVisuals`;
- the existing driver registry can resolve the plain C# driver from that visual root;
- variant mapping can use `attack.IsMultiTargeted` for `Area` and the existing `hitCount` argument for `MultiHit`;
- animation lookup/playback must be wrapped so failures cannot interrupt damage or `BloodFeastPower` reconciliation.

Phase 3B implementation and user smoke testing are complete for the shared trigger. Runtime logs recorded `161` normal and `22` multi-hit calls with no `PlayAttack skipped`, bridge, script, or tween exception. The user judged normal and multi-hit behavior basically correct, but the original whole-body translation was too subtle.

Phase 3C built and deployed on `2026-07-10 14:29`. The actual checked-out driver still had the old `-5 -> +22 px` values despite an earlier progress summary claiming `+36 px`; the source was corrected and the final bold motion now travels from `-14 px` wind-up to `+72 px` strike (`86 px` total), with `1.06` scale, `2.5 degree` rotation, and a `0.28 s` return.

The all-enemy attack failure was localized to `CardAttack(...)`: `AllEnemies` plays have no single `play.Target`, so the command executed without targets. The shared helper now calls `TargetingAllOpponents(Owner.Creature.CombatState!)` for `TargetType.AllEnemies` while preserving existing single-target behavior.

Diagnostic marker: `20260710-attack-motion-bold-area-fix-phase3c`.

Phase 3C record: `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE3C_ATTACK_MOTION_AREA_FIX_20260710_V1.md`.

Step 4 remains open for the bold movement and repaired area-attack runtime test.

- [x] **Step 4: Verify attack cards.**

Manual check:

- Play `BloodBlade`.
- Confirm one lunge and one slash.
- Play another attack.
- Confirm transform resets between attacks.
- End combat and confirm map is clean.

Runtime verification passed on `2026-07-10 14:37`. The fresh Phase 3C log contained `21` normal, `3` multi-hit, and `2` area triggers. It contained `0` `No targets set`, `0` `PlayAttack skipped`, and `0` script or bridge exceptions. The user accepted the stronger movement and confirmed the visual result passes.

Task 3 is complete. The next implementation batch is Task 4 Step 1, `PlayCast(...)`, using the existing `impact_burst.png` without changing portrait art.

Verification record: `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE3C_ATTACK_MOTION_AREA_FIX_20260710_V1.md`.

### Task 4: Cast and Block

**Files:**

- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaCombatAnimationDriver.cs`
- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\Cards\EntelechiaCard.cs`
- Record (Step 1): `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE4A_CAST_API_20260710_V1.md`
- Record (Step 2): `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE4B_BLOCK_API_20260710_V1.md`
- Record (Step 3): `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE4C_CAST_TRIGGER_20260710_V1.md`
- Record (scale-only Phase 4D): `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE4D_BATTLE_SCALE_REFERENCE_20260710_V1.md`
- Record (body-matched Phase 4E): `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE4E_BODY_MATCHED_SCALE_20260710_V1.md`
- Record (position-only Phase 4F): `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE4F_HORIZONTAL_NUDGE_20260710_V1.md`
- Record (toe-aligned Phase 4H): `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE4H_TOE_ALIGNMENT_20260710_V1.md`

- [x] **Step 1: Implement `PlayCast(EntelechiaAnimationVariant variant)`.**

Use cast motion and `impact_burst.png`. Use `Blood` variant only when the card pays HP or is explicitly blood-themed.

Implemented and deployed as `20260710-cast-api-phase4a` on `2026-07-10 14:43`. The API uses a `0.60 s` lift/pulse/return sequence, restores the exact saved baseline, and uses only the existing `impact_burst.png`. `Normal`, `Blood`, and `Power` variants currently differ by tint, impact alpha, and impact scale.

No card trigger is wired yet, so Step 1 is verified by source inspection and a successful build; runtime visual verification will occur after Step 3 establishes the shared trigger.

Reference comparison included the official Godot Tween documentation and `GanbaruKing/BloodMazeMod-StS2`'s state-separated custom-character scene. Entelechia keeps the approved single-portrait Tween architecture rather than adopting frame animation.

- [x] **Step 2: Implement `PlayBlock()`.**

Use defensive settle/rebound motion and low-alpha `impact_burst.png`.

Implemented and deployed as `20260710-block-api-phase4b` on `2026-07-10 14:47`. The API follows the planned `0.34 s` settle/rebound/return sequence and uses only a compact low-alpha `impact_burst.png` pulse.

Block keeps body tint and rotation unchanged, does not expose slash or blood VFX, and returns through the shared baseline reset and idle-resume path.

No block trigger is wired yet. Step 2 is verified by source inspection and a successful build; runtime visual verification remains part of Step 5 after selective block wiring exists.

- [x] **Step 3: Trigger cast by card type.**

For first pass:

- `CardType.Skill` triggers cast unless it immediately triggers attack or HP cost.
- `CardType.Power` triggers power-flavored cast.

Implemented and deployed as `20260710-cast-trigger-phase4c` on `2026-07-10 14:57`.

Runtime reflection confirmed the exact shared `CardModel.OnPlayWrapper(PlayerChoiceContext, Creature, bool, ResourceInfo, bool)` signature. A Harmony pass-through postfix awaits the original card-play `Task` and requests animation only after successful completion.

Current classification: `21` Attack cards excluded, `7` HP-cost Skills excluded, `22` ordinary Skills mapped to `Normal`, and `7` Powers mapped to `Power`. `Blood` is not inferred from card names and remains reserved for explicit HP-cost work.

Animation lookup and playback are isolated from gameplay exceptions. No individual card implementation was modified.

Step 3 implementation is complete, but its runtime gate remains open until a fresh combat proves Normal/Power Cast playback and the absence of Harmony or animation exceptions. Step 4 must wait for that gate.

Phase 4C runtime evidence is now available from `godot.log` written at `2026-07-10 15:05:42`: six Phase 4C visual loads, `52` Normal Cast calls, `49` Normal attacks, `19` MultiHit attacks, and `2` Area attacks. `No targets set`, `PlayAttack skipped`, Entelechia errors, `System.ArgumentException`, and `SCRIPT ERROR` all have count `0` after the first marker.

This proves ordinary Skill Cast and Area attack behavior, but Power Cast remains unverified (`0` calls). Step 4 remains gated.

Phase 4D's `0.375` scale was superseded after the user pointed out that the oversized scythe distorted the full-art size comparison.

Body-matched Phase 4E was deployed on `2026-07-10 15:27`. Existing manual person masks define a `616 px` body height versus `817 px` full-art height. Scale `0.48` produces a `295.68 px` person body, close to Watcher (`303 px`) and BloodMaze Revenant (`310 px`). The foot baseline remains `0`.

`Bounds`, `CenterPos`, and `IntentPos` now derive from the person body bounds while sprite placement still uses the full art, preventing the enlarged scythe from pushing UI anchors excessively high.

Position-only Phase 4F was deployed on `2026-07-10 15:34`: the sprite and all derived body anchors move right by exactly `12 px`; scale, Y coordinates, foot baseline, and animation behavior are unchanged.

An intermediate Phase 4G build moved the total offset to `20 px`. The fresh runtime screenshot `D:\desktop\mod\8cf9e1ad-56ab-4414-b596-d36f98e044c0.png` and `godot.log` confirmed that exact build was loaded.

Screenshot/template measurement placed the manually annotated foot tip at screen X `668.64` and the health-bar centerline at screen X `720`. The `51.36 px` screen difference converts to `38.52` Godot pixels at the observed `0.64 / 0.48` display ratio.

Phase 4H was deployed on `2026-07-10 15:56` with total horizontal offset `58.5 px`, but runtime rejected the alignment. The fresh screenshot `D:\desktop\mod\953f3ae0-484b-4409-839e-0e3107d29c8e.png` matched the portrait at `(400, 289)` and scale `0.64`, proving the portrait moved right. The health-bar frame also moved from center X about `720` to center X about `771` because `%Bounds` was derived from the same `spritePosition.X`.

Phase 4I decouples the offsets:

- portrait offset remains `58.5f`, placing the foot tip near screen X `720.64`;
- bounds/UI offset returns to `20f`, placing the health-bar center near screen X `720`;
- scale, Y coordinates, foot baseline, body dimensions, animations, cards, and VFX timing are unchanged.

The current diagnostic marker is `20260710-scale048-toealigned-phase4i`. Step 4 must wait for Phase 4I position verification and one Power Cast.

- [x] **Step 4: Trigger block only where reliable.**

Do not infer block from every skill. Only wire block-heavy known cards after checking their class behavior.

- [x] **Step 5: Manual check.**

Expected:

- Attack, cast, and block are visually distinct.
- No red damage tint on block.
- No slash arc on ordinary cast.

Task 4's final manual gate was accepted under Phase 5B. The accepted session contains 26 Attack, 7 Cast, and 12 Block markers with no Phase 5B failure marker; see `STATIC_PAN_VFX_PHASE5B_OFFICIAL_HOOKS_20260710_V1.md`.

### Task 5: HP Cost, Hurt, and Heal

**Files:**

- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaCombatAnimationDriver.cs`
- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\Cards\EntelechiaCard.cs`
- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\CombatPatches.cs`
- Record: `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE4_HP_EVENTS_20260710_V1.md`

- [x] **Step 1: Implement `PlayHpCost()`.**

Use the HP Cost values listed above. Use `blood_drops.png`.

- [x] **Step 2: Trigger HP cost from `TryPayHpCost(...)`.**

Trigger only after `TurnStateTracker.LoseHpTracking(...)` completes successfully.

- [x] **Step 3: Implement `PlayHurt()`.**

Use recoil and red tint. Use `impact_burst.png`.

- [x] **Step 4: Implement `PlayHeal()`.**

Use calm lift and low-alpha `impact_burst.png`.

- [x] **Step 5: Identify reliable hurt/heal sources before wiring.**

Do not add a broad Harmony hook until a specific game event or existing patch is confirmed. If the reliable source is unclear, stop this phase and write a diagnostic record instead of guessing.

Phase 5B replaced the initial low-level event patches before runtime acceptance. The installed game assembly confirms these public event sources:

- `Hook.AfterBlockGained(...)` for successful card block;
- `Hook.AfterDamageReceived(...)` with `DamageResult` and `ValueProp` for Hurt;
- `Hook.AfterCurrentHpChanged(...)` with a signed `delta` for Heal and turn tracking.

Current runtime marker: `20260710-statefx-official-hooks-phase5b`. Runtime acceptance is still pending. See `STATIC_PAN_VFX_PHASE5B_OFFICIAL_HOOKS_20260710_V1.md`.
### Task 6: Death and Cleanup

**Files:**

- Modify: `D:\desktop\mod\Entelechia\EntelechiaCode\Animation\EntelechiaCombatAnimationDriver.cs`
- Modify: exact death hook file after the reliable hook is identified
- Record: `D:\desktop\mod\Entelechia\generated_art\review\STATIC_PAN_VFX_PHASE5_DEATH_CLEANUP_20260710_V1.md`

- [x] **Step 1: Implement `PlayDeath()`.**

Use death drift, rotation, and alpha fade. Set `_dead = true`.

- [x] **Step 2: Implement lifecycle cleanup.**

Add a cleanup method that:

- kills active tween;
- hides all VFX;
- restores z-index defaults;
- does not keep any node `TopLevel`;
- does not leave body alpha visible after death transition.

- [x] **Step 3: Wire death only after confirming the correct STS2/BaseLib hook.**

If the hook is not clear from local source and logs, stop and write the uncertainty into the phase record.

- [x] **Step 4: Manual check.**

Expected:

- Death fade happens once.
- Death does not restart idle.
- Map is clean after combat.

## Priority Rules

If effects overlap, use this priority:

1. Death
2. Hurt
3. HP Cost
4. Attack
5. Cast / Power
6. Block
7. Idle

Rules:

- A higher-priority effect may interrupt a lower-priority effect.
- A lower-priority effect must not interrupt death.
- Each effect must begin from a reset baseline unless it is a planned multi-hit continuation.
- Idle resumes only when `_dead == false`.

## Verification Commands

Build after every task:

```powershell
dotnet build D:\desktop\mod\Entelechia\Entelechia.csproj -c Release
```

Check latest game log marker:

```powershell
Select-String "Entelechia|CreateCustomVisuals|VfxRoot|SlashArc|ImpactBurst|BloodDrops" `
  "C:\Users\34062\AppData\Roaming\SlayTheSpire2\logs\godot.log" | Select-Object -Last 80
```

Manual screenshot set after every visible phase:

- one combat screenshot while idle;
- one screenshot immediately after triggering the new state;
- one map screenshot after combat ends.

## Risk Controls

| Risk | Control |
|---|---|
| Portrait leaks above map again | Keep default z-index; avoid `TopLevel`; hide and reset all VFX; test map after every phase |
| Body offset accumulates | Store base transforms once; call reset before and after every effect |
| Effect feels noisy | Keep durations under one second except death; use low alpha VFX |
| Hook breaks gameplay | Trigger only after successful gameplay actions; no animation failure can block card execution |
| Hurt/HP cost ambiguity | HP cost triggers from `TryPayHpCost`; enemy hurt must use a separately confirmed source |
| Speed lines alpha issues | Exclude `speed_lines.png` from first implementation |

## First Batch To Execute After This Plan

Execute Task 1 only.

Do not animate yet. Task 1 should only prove that the stable combat visual tree can safely hold hidden VFX nodes without changing the current appearance or reopening the map-layer issue.
