# Entelechia Battle Animation Redesign

## Scope

Design a complete Entelechia combat animation set by referencing STS2/BaseLib character visual conventions and the failed local drafts.

This document replaces the previous crop/rotate/VFX-preview direction for combat animation. It does not import assets into the mod yet.

## Current Evidence

- `EntelechiaCode/Character/Entelechia.cs` inherits `PlaceholderCharacterModel`.
- BaseLib documents that `PlaceholderCharacterModel` uses a base-game character visual by placeholder ID and defaults to Ironclad.
- BaseLib supports custom combat visuals through `NCreatureVisuals`.
- A static image can be wrapped into `NCreatureVisuals` with `NodeFactory<NCreatureVisuals>.CreateFromResource`, but this is only a fallback.
- A custom visual scene should contain `Visuals`, `Bounds`, `IntentPosition`, and `CenterPos`; `OrbPos` and `TalkPos` are optional.
- BaseLib can drive Godot `AnimationPlayer`, `AnimationPlayer2D`, or `AnimationTree` animations named `idle`, `attack`, `cast`, `hurt`, and `die`.

Reference pages:

- https://alchyr.github.io/BaseLib-Wiki/docs/models/custom-character.html
- https://alchyr.github.io/BaseLib-Wiki/docs/scenes/creature-visuals.html

## Rejected Direction

Reject these as final combat animation sources:

- `generated_art/character/battle_state_previews/`
- `generated_art/character/battle_ai_review/`
- direct horizontal combat-splash crop as the standing unit
- body-wide AI redraws that change Entelechia's face, outfit, boots, coat, scythe, or silhouette

Reason:

The result reads as collage, not a creature visual. It lacks a stable combat unit body, rig-friendly layers, usable pivots, and real animation timing.

## Design Decision

Use a Godot 2D layered creature visual first. Do not attempt Spine first.

Rationale:

- BaseLib already supports Godot animation nodes for `NCreatureVisuals`.
- A layered Godot visual is faster to review and iterate than Spine.
- The mod can later upgrade to Spine only after the base silhouette, layers, and timings are accepted.

## Art Direction

Entelechia must read as a STS-style battlefield unit, not card art.

Required visual properties:

- upright or slight three-quarter stance
- grounded feet and stable shadow
- compact vertical silhouette
- scythe visible but not swallowing the body
- face, hair, ears, outfit, and scythe recognizably from the original standing art
- restrained blood/candle/rose VFX as separated layers
- no background scene, no horizontal splash composition, no overlarge red curtain

The 2432x2432 combat illustration from `D:\desktop\mod\pic` is motion reference only. It must not be used as the unit body.

## Layer Plan

Create one reviewed master first, then split it into these layers:

- `body_legs`
- `torso`
- `head`
- `hair_front`
- `hair_back`
- `front_arm`
- `back_arm`
- `coat_front`
- `coat_back`
- `scythe_handle`
- `scythe_blade`
- `blood_thread_vfx`
- `blood_crystal_vfx`
- `candle_ember_vfx`
- `shadow`

Each layer must have enough transparent padding to rotate around a sensible pivot without clipping.

## Animation Set

Minimum complete set:

| State | Purpose | Timing | Body Motion | VFX |
| --- | --- | --- | --- | --- |
| `idle` | default loop | 1.6-2.2s loop | tiny breathing, hair/coat/scythe sway | faint blood beads |
| `attack` | normal attack | 0.45-0.65s then return | small forward press, scythe snap | short crescent slash, not a huge arc |
| `cast` | skill/power use | 0.7-1.0s then return | hand/scythe lift, body steady | blood threads, candle glints |
| `hurt` | hit reaction | 0.25-0.4s then return | small recoil, quick tint | tiny crystal break |
| `die` | defeat | 1.0-1.4s no return | slump/fade or kneel/fade | embers and red beads fade |
| `relaxed` | optional event/menu loop | 1.6-2.2s loop | calmer idle | minimal |

## Next Review Batch

Only produce one asset next:

- `generated_art/character/battle_unit_redesign/entelechia_battle_unit_base_v2.png`

Review sheet:

- `generated_art/review/battle_unit_base_v2_contact.png`

The contact sheet must compare:

- original standing art
- rejected crop/rotate preview
- new battle unit base v2
- thumbnail-size preview

Acceptance:

- still unmistakably Entelechia
- reads as a battlefield unit at thumbnail size
- has stable feet and compact silhouette
- scythe and body can be split into layers
- no large background effects
- no invented outfit redesign

## After Base Approval

Only after `battle_unit_base_v2` passes review:

1. Split reviewed base into layers.
2. Build a Godot-compatible layer folder.
3. Produce `idle`, `attack`, `cast`, `hurt`, `die` preview strips or GIFs.
4. Create a minimal `.tscn`/`AnimationPlayer` plan.
5. Wire `CreateCustomVisuals` only after visual review passes.

Do not import experimental art into `Entelechia/images` until the user explicitly approves.
