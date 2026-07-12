# Entelechia Character Art Design

## Scope

This stage draws character and animation source assets only. Do not import, sync, or overwrite files under `Entelechia/images` in this stage.

Working outputs go under `generated_art/character/`. Review sheets go under `generated_art/review/`.

## Character Lock

Entelechia must match the existing character identity:

- short black hair, red eyes, pointed ears, pale nonhuman Sarkaz look
- black short jacket with red lining, dark silver details, white neck detail, fitted silhouette, black-red trailing hem
- giant crescent scythe as the main weapon
- cold, restrained, ritualized, elegant cruelty; never berserk gore or generic vampire glamour

Do not use:

- long hair, dress, heavy armor, blue-black skin, generic vampire cape, bat wings, angel wings, blood doors, heroic fire, mobile-game badge symmetry
- photoreal paint, 3D render, excessive glow, text, watermark

## Required References

Use these as priority references:

1. `D:\desktop\mod\pic\立绘_隐德来希_1.png` - primary face, outfit, silhouette
2. `D:\desktop\mod\pic\立绘_隐德来希_2.png` - combat pose and scythe dynamics
3. `D:\desktop\mod\pic\头像_隐德来希.png` - icon crop reference only
4. `D:\desktop\mod\pic\技能_玫影觅迹.png`, `技能_绯红壁合.png`, `技能_灵与欲的惜别.png` - motion and effect language
5. `D:\desktop\mod\pic\uniequip_002_etlchi.png`, `uniequip_003_etlchi.png`, `道具_带框_隐德来希的信物.png` - candle, rose, wax, blood-crystal, token motifs

`立绘_隐德来希_skin1.png` is not a primary reference and must not overwrite the default identity.

## Background Motifs

Use precise blood control, red beads, blood threads, crystalized blood, red wax, rose thorns, sutures, candle flame, and quiet assassination/information-work cues.

Avoid uncontrolled gore. Blood should read as deliberate Sarkaz blood arts and Rose River contract work, not horror splatter.

## Batch 1: Static UI Drafts

Generate four draft assets first, saved only as candidates:

| Asset | Candidate path | Later final path | Size |
| --- | --- | --- | --- |
| character select | `generated_art/character/charui/char_select_char_name.png` | `Entelechia/images/charui/char_select_char_name.png` | 132x195 |
| locked select | `generated_art/character/charui/char_select_char_name_locked.png` | `Entelechia/images/charui/char_select_char_name_locked.png` | 132x195 |
| character icon | `generated_art/character/charui/character_icon_char_name.png` | `Entelechia/images/charui/character_icon_char_name.png` | 85x85 |
| map marker | `generated_art/character/charui/map_marker_char_name.png` | `Entelechia/images/charui/map_marker_char_name.png` | 49x64 |

Review them in one contact sheet: `generated_art/review/charui_batch1_contact.png`.

## Batch 2: Battle Source Drafts

After Batch 1 review, draw a transparent battle master candidate and split-layer plan under `generated_art/character/battle/`.

Minimum layers:

- body and legs
- head and hair
- coat and trailing hem
- near arm
- far arm
- scythe
- blood thread / crystal VFX

The intended runtime path is Godot 2D layered animation for idle, attack, hurt, die, and relaxed poses. Do not create Spine `.skel` assets in this stage.

## Acceptance

Each batch passes only if:

- the character remains recognizably Entelechia at small size
- all dimensions match the table or the batch-specific target
- no text or watermark is present
- UI assets stay readable after downscaling
- transparent assets validate with alpha where required
- outputs remain in `generated_art/character/` until the user explicitly asks to import them into the mod
