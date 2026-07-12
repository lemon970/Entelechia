# Entelechia Static Pan + VFX Animation Plan

Date: 2026-07-10

Status: planning only. No code change, no image generation, no image2 workflow, no PNG edit, no import change.

## Current Decision

Use the currently accepted combat standing portrait as the only character body.

Do not redraw the character. Do not split the standing portrait into body, hair, coat, arm, scythe, or face layers. Do not return to AI frame animation or manual scythe/body cutout work.

The animation style is:

- one main `Sprite2D` for the full standing portrait;
- small translate / scale / rotation / alpha / tint changes on that sprite;
- a few separate VFX sprites for emphasis;
- short effects that always return to the stable idle pose;
- no effect should outlive combat or draw above map UI.

## Confirmed Baseline

The combat visual now loads and is positioned correctly.

Current visual facts from the latest working line:

- main image: `Entelechia/images/character/idle.png`
- texture size: `1024 x 1536`
- measured non-transparent content: `x=174..821`, `y=361..1177`
- current logical content size at scale: about `233 x 294`
- current `Sprite2D` scale: `0.36`
- current `Sprite2D` position: about `(5.22, -147.24)`
- current `Bounds`: about `257 x 306`
- `ZIndex = 10` has been removed from the main Sprite2D; it should remain at default z-order unless a later test proves otherwise.

Latest relevant record:

- `generated_art/review/BATTLE_VISUAL_ZINDEX_FIX_20260710_V7.md`

## Available VFX Assets

Existing assets under `Entelechia/images/vfx/`:

| Asset | Planned Use | Notes |
|---|---|---|
| `slash_arc.png` | attack slash / scythe sweep | primary attack VFX |
| `impact_burst.png` | hit spark / cast pulse / hurt crack | reusable burst overlay |
| `blood_drops.png` | blood cards, HP cost, death fade | use sparingly |
| `speed_lines.png` | not used in first pass | recorded earlier as not ready as a transparent overlay |

First pass should use only `slash_arc.png`, `impact_burst.png`, and `blood_drops.png`.

## Animation Architecture

Use a code-driven animation helper attached to the manual `NCreatureVisuals` tree.

Planned node shape:

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

Initial implementation should keep all VFX hidden by default:

- `Visible = false`
- `Modulate.a = 0`
- default scale and position reset after each effect
- no positive z-index unless absolutely needed; if needed, use the smallest local z-order that does not bleed above map UI

The animation helper should store the accepted base values:

- base sprite position
- base sprite scale
- base sprite rotation
- base sprite modulate
- base VFX transforms

Every state animation must end by restoring these values.

## State Set

### Idle

Purpose:

- make the character feel alive without drawing attention away from gameplay.

Motion:

- loop duration: `1.8s-2.4s`
- y movement: `-2px..+2px`
- scale: `x 1.000`, `y 0.997..1.003`
- rotation: none, or at most `0.2deg`

VFX:

- first pass: none
- later optional: one very faint blood shimmer every few seconds, not continuous particles

Acceptance:

- no visible jitter;
- no clipping against Bounds;
- map transition still clean.

### Attack

Purpose:

- give all attack cards a crisp scythe/action cue.

Motion:

- duration: `0.35s-0.55s`
- wind-up: tiny backward/upward pull for `0.06s-0.10s`
- strike: forward/right movement `18px-28px`, slight downward `2px-6px`
- scale punch: `1.02..1.04`
- rotation: `-1.5deg..+2deg`, very short
- return: ease back to idle in `0.14s-0.22s`

VFX:

- `slash_arc.png`
  - appears near the scythe/body front, not centered on the whole canvas
  - alpha peaks quickly, then fades
  - duration `0.18s-0.28s`
- `impact_burst.png`
  - optional for heavy attacks or multi-hit finisher
  - duration `0.12s-0.20s`

Variants:

- normal attack: one strike pulse
- multi-hit attack: one main body lunge plus 2-4 small slash flickers, not a full body lunge per hit
- AoE attack: wider slash arc, smaller body movement

Acceptance:

- the body never leaves the player slot;
- slash does not cover card UI;
- repeated attacks do not accumulate offset.

### Cast / Skill / Power

Purpose:

- represent blood/candle/ritual effects without implying a weapon strike.

Motion:

- duration: `0.45s-0.75s`
- lift: y `-5px..-10px`
- scale: `1.01..1.025`
- rotation: none or tiny `0.4deg`
- modulate: slight warm/red tint for less than `0.25s`

VFX:

- `blood_drops.png`
  - for blood, HP-cost, and self-sacrifice style skills
  - keep low opacity and short duration
- `impact_burst.png`
  - tinted or scaled as a soft pulse for power setup

Variants:

- Skill card: small lift and red pulse
- Power card: slower pulse, no forward movement
- Blood/candle special cards: add brief blood drops

Acceptance:

- cast effects feel distinct from attack;
- no large red overlay;
- no persistent tint after completion.

### Block / Guard

Purpose:

- give defensive cards a small response without adding new shield art.

Motion:

- duration: `0.28s-0.45s`
- settle backward/left: `4px-8px`
- scale: `0.99..1.00`
- optional quick rebound to idle

VFX:

- first pass: use `impact_burst.png` as a small low-alpha pulse near torso/feet
- do not invent a new shield asset in this pass

Acceptance:

- readable but subtle;
- does not look like taking damage.

### Hit / Hurt

Purpose:

- communicate damage taken while keeping the static portrait clean.

Motion:

- duration: `0.18s-0.32s`
- recoil left/back: `10px-16px`
- y: `0px..-4px`
- scale: `0.99..1.00`
- red tint: peak for `0.06s-0.10s`, then restore

VFX:

- `impact_burst.png` near upper torso or center;
- optional tiny `blood_drops.png` if HP loss is significant.

Acceptance:

- hurt must not be confused with attack wind-up;
- no repeated red tint accumulation;
- animation interruption returns to valid idle baseline.

### HP Cost / Self-Blood

Purpose:

- distinguish Entelechia's own HP payment from enemy damage.

Motion:

- duration: `0.30s-0.50s`
- small inward contraction: scale `0.985..0.995`
- slight downward dip: `2px-5px`
- quick recovery to idle

VFX:

- `blood_drops.png` at low alpha near lower body/feet;
- optional red pulse on main sprite.

Trigger priority:

- higher priority than generic cast if card pays HP;
- lower priority than fatal/death.

Acceptance:

- visible enough to explain HP cost;
- not as violent as hurt from an enemy hit.

### Heal / Restore

Purpose:

- mark blood recovery or relic restoration without green/healing art that clashes with the character.

Motion:

- duration: `0.35s-0.60s`
- small upward lift: `-3px..-6px`
- scale: `1.005..1.015`
- return smoothly

VFX:

- first pass: very small `impact_burst.png` pulse, tinted pale red/white if tinting is safe;
- avoid full-screen glow.

Acceptance:

- clearly calmer than attack/hurt;
- no new art requirement.

### Death

Purpose:

- provide a clean end state without redrawing a death pose.

Motion:

- duration: `0.9s-1.3s`
- downward drift: `20px-36px`
- slight rotation: `2deg-4deg`
- alpha fade to `0`

VFX:

- `blood_drops.png` fade-out;
- optional small `impact_burst.png` at start.

Acceptance:

- no return to idle;
- no sprite remains visible after combat transition;
- map screen remains clean.

## Trigger Strategy

The animation system should be implemented in layers.

### Layer 1: Visual Driver

Create a small code-side driver that can play named effects on the `NCreatureVisuals` tree:

- `PlayIdle()`
- `PlayAttack(AnimationVariant variant)`
- `PlayCast(AnimationVariant variant)`
- `PlayBlock()`
- `PlayHurt()`
- `PlayHpCost()`
- `PlayHeal()`
- `PlayDeath()`
- `ResetVisuals()`

This layer should not know about cards.

### Layer 2: Simple Combat Hooks

Wire only the most reliable game events first:

- attacks through the existing `EntelechiaCard.ExecuteAttack(...)` path;
- skills/powers through `EntelechiaCard` card type checks where practical;
- HP cost through existing `TurnStateTracker.LoseHpTracking(...)`;
- heal through `TurnStateTracker.HealTracking(...)`;
- death only after confirming the right STS2/BaseLib death hook.

### Layer 3: Card-Specific Polish

Only after Layer 1 and Layer 2 are stable:

- multi-hit attacks get multi-flicker slash variants;
- blood-themed cards get blood drop variants;
- candle-themed powers get slower pulse variants;
- rare cards may get larger effects, but still no redraws.

## Implementation Phases

### Phase 1: Build the Driver Skeleton

Goal:

- add `VfxRoot` and hidden VFX sprites;
- store/reset base transforms;
- implement `ResetVisuals()`;
- keep current combat appearance unchanged when no animation is playing.

Acceptance:

- combat portrait still appears in the correct position;
- ending combat does not leave visuals on map;
- no visible VFX yet unless explicitly triggered by a test call.

### Phase 2: Idle + Attack Prototype

Goal:

- add idle breathing loop;
- trigger one attack animation from `ExecuteAttack(...)`;
- show `slash_arc.png` briefly.

Acceptance:

- attack cards show one clean lunge + slash;
- non-attack cards do not trigger attack;
- repeated attacks return to idle baseline.

### Phase 3: Cast + Block

Goal:

- skill/power cards trigger cast pulse;
- block-heavy cards optionally trigger block/guard pulse.

Acceptance:

- attack/cast/block are visually distinct;
- no effect overlaps card UI;
- no persistent alpha/tint/scale drift.

### Phase 4: Hurt + HP Cost + Heal

Goal:

- distinguish enemy damage, self HP payment, and recovery.

Acceptance:

- self HP payment uses the HP-cost animation, not generic hurt;
- enemy hit uses hurt recoil;
- heal uses calm lift/pulse;
- animations do not conflict when several events happen in quick sequence.

### Phase 5: Death and Cleanup

Goal:

- add death fade/drift;
- ensure all tweens and VFX are stopped/cleaned when combat ends.

Acceptance:

- death does not return to idle;
- map screen is clean;
- VFX nodes do not remain visible after scene transition.

## Risk Controls

| Risk | Mitigation |
|---|---|
| Combat visual leaks onto map again | keep default z-index; reset VFX visibility; avoid `TopLevel`; test combat-end-to-map after every phase |
| Tweens accumulate offsets | store base transforms and reset before/after every named animation |
| Card hooks become too invasive | start with `EntelechiaCard.ExecuteAttack` and existing HP tracking; avoid broad Harmony hooks until needed |
| VFX feels noisy | first pass uses very low count and short durations; add variants only after screenshot review |
| Effects mismatch character identity | never redraw body; VFX stays secondary to the accepted standing portrait |
| Speed lines asset has bad alpha | exclude `speed_lines.png` from first pass |

## Do Not Do

- Do not use image generation.
- Do not use image2.
- Do not redraw Entelechia's face, outfit, scythe, boots, or body.
- Do not split the standing portrait into detailed body layers.
- Do not reintroduce `ZIndex = 10` on the main sprite.
- Do not create a `.tscn` route unless code-driven animation cannot be reliably triggered.
- Do not implement all states in one batch.

## Verification Plan

Each implementation phase should be verified with:

- `dotnet build D:/desktop/mod/Entelechia/Entelechia.csproj -c Release`
- one Entelechia combat screenshot;
- one combat-end-to-map screenshot;
- `godot.log` marker confirming the correct build;
- review note under `generated_art/review/`.

Minimum manual checks:

- attack card played once;
- skill card played once;
- block card played once;
- self HP cost card played once, if available in hand;
- take enemy damage once;
- end combat and confirm map has no leftover portrait/VFX.

## First Implementation Batch Recommendation

Start with Phase 1 only.

Do not animate yet. Add only:

- `VfxRoot`;
- hidden `SlashArc`, `ImpactBurst`, `BloodDrops`;
- base transform storage;
- `ResetVisuals()`;
- diagnostic log listing VFX child nodes.

Reason:

The previous hardest issues were visibility, positioning, unique node lookup, z-order, and map leakage. The first animation batch should prove the visual tree can safely hold extra nodes without reopening those problems.
