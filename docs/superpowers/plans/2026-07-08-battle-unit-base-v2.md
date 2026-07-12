# Battle Unit Base V2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce one reviewable Entelechia battlefield-unit base using the original character art as the locked underlay for a full Godot 2D layered animation set.

**Architecture:** Use the original standing art as the base/underlay, crop it into a battlefield-unit plate, then compare it against the rejected crop/rotate preview in a contact sheet. Do not split layers, animate, or import into the mod until the base plate passes review.

**Tech Stack:** Python/Pillow for original-art cropping, contact sheet, and dimension checks; BaseLib/Godot constraints from the redesign spec. Built-in image generation is not used for the body base.

## Global Constraints

- Do not import or overwrite files under `Entelechia/Entelechia/images`.
- Reject body-wide AI redraws that change Entelechia's face, outfit, boots, coat, scythe, or silhouette.
- Use original standing art as the locked body underlay for `battle_unit_base_v2`.
- The new base must read as a STS-style battlefield unit, not card art or horizontal splash art.
- The 2432x2432 combat illustration is motion reference only and must not be used as the unit body.
- Next review output is `generated_art/character/battle_unit_redesign/entelechia_battle_unit_base_v2.png`.
- Review sheet is `generated_art/review/battle_unit_base_v2_contact.png`.

---

### Task 1: Generate Battle Unit Base V2 Original Underlay

**Files:**
- Create: `generated_art/character/battle_unit_redesign/entelechia_battle_unit_base_v2.png`
- Create: `generated_art/review/battle_unit_base_v2_report.md`

**Interfaces:**
- Consumes: `generated_art/review/battle_source_compare_contact.png`
- Consumes: `generated_art/review/battle_from_standing_original_contact.png`
- Produces: one review-only battle-unit base image from original art.

- [ ] **Step 1: Load visual references**

Use `view_image` on:

```text
D:\desktop\mod\Entelechia\generated_art\review\battle_source_compare_contact.png
D:\desktop\mod\Entelechia\generated_art\review\battle_from_standing_original_contact.png
```

- [ ] **Step 2: Crop original standing art into the base plate**

Use the 1736x1736 original standing art from `D:\desktop\mod\pic` and save the output as:

```text
D:\desktop\mod\Entelechia\generated_art\character\battle_unit_redesign\entelechia_battle_unit_base_v2.png
```

- [ ] **Step 3: Create contact sheet**

Create `generated_art/review/battle_unit_base_v2_contact.png` containing:

```text
original standing source
rejected crop/rotate preview
new battle_unit_base_v2
thumbnail-size new battle_unit_base_v2
```

- [ ] **Step 4: Record review notes**

Create:

```text
D:\desktop\mod\Entelechia\generated_art\review\battle_unit_base_v2_report.md
```

It must say whether the draft is accepted, rejected, or review-only, and why.
