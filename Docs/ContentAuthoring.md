# Content authoring checklist

All content lives in `Assets/SortEverything/Resources/SortEverything/`. After any change, run **Sort Everything ▸ Validate Content** (the same checks also run as the EditMode test `B2_ContentValidator_RunsCleanOnAllAuthoredContent`).

| To add | Edit | Code change? |
|---|---|---|
| A sortable object | `Content/objects.json` + a sprite | No |
| A container type | `Content/containers.json` (+ optional sprites) | No, unless you want a new procedural style |
| A level | `Content/Levels/<anything>.json` | No |
| An area or room | `Content/areas.json` | Partly (see below) |

Layout proportions (zone shares, item sizes, bin spacing, drag lift, mess on/off) are tuned in `BoardLayout.json`. They are not part of content.

---

## 1. A sortable object

1. Add an entry to `Content/objects.json`:
   - `id`: unique, lowercase.
   - `code`: two letters, unique.
   - `category`: one of `fruit`, `vegetables`, `drinks`, `kitchenware`. Sorting uses only this field, never the art.
   - `color`: the item's dominant color. The validator checks its contrast against every counter.
   - `relativeSize`: 1.0 is a typical fruit. A carton is about 1.3, a plate 1.4, a spoon 0.9.
   - `material`: one of `fruit`, `plastic`, `carton`, `paper`, `metal`, `ceramic`, `glass`, `wood`. This picks the placement sound.
   - `tags`: free-form.
   - `art`: optional sprite path.
2. Add art: a square PNG with a transparent background at `Art/<id>.png`. Follow `Docs/ArtDirection.md` (sculpted 2.5D, top-left light, warm-brown outline). It imports as a sprite automatically.
3. Use the object in a level (step 3).
4. Run **Validate Content**. It fails if `relativeSize`, `material` or the art is missing, or if the color is too close to the counter.

## 2. A container type

1. Add an archetype to `Content/containers.json`:
   - `id`, `label`.
   - `style`: one of `basket`, `fridge`, `crisper`, `drawer`, `rack`, `tray`.
   - Colors: `body`, `inner`, `accent`, `light`.
   - `pattern`: `weave`, `slats` or empty.
   - `sound`, and `lipShare` (how tall the front lip is).
   - Optional `backSprite` / `frontSprite`: Resources paths to your own art. Use a 9-sliced back and a horizontally sliced front lip.
2. Map it:
   - For single-category bins, set it in `categoryArchetypes` (`{ "category": "kitchenware", "archetype": "dish_rack" }`).
   - Bins that accept several kinds use `sharedArchetype`.
3. Validate.

A brand-new procedural look, meaning a new `style` value, needs a few lines in `ContainerArt.cs`. Supplying sprites needs no code.

## 3. A level

Create `Content/Levels/<pack>.json`:

```json
{ "levels": [ {
  "levelId": "kitchen_extra_001",
  "purpose": "Set out the tea things",
  "instruction": "Put each item with its group",
  "region": "finishing",
  "finishMicroAction": "tray_settle",
  "purposeProp": "breakfast_tray",
  "boards": [ {
    "id": "a", "required": true, "template": "U",
    "objects": ["teapot", "mug", "plate", "apple", "banana"],
    "targets": [
      { "id": "kitchenware_only", "accepts": ["kitchenware"], "capacity": -1, "x": 0.25, "y": 0.5, "rectProfile": "wide", "slotProfile": "four_square", "slots": 3 },
      { "id": "fruit_only",       "accepts": ["fruit"],       "capacity": -1, "x": 0.75, "y": 0.5, "rectProfile": "wide", "slotProfile": "two",         "slots": 2 }
    ] } ] } ] }
```

**Fields:**
- `template` is the rule shape the existing evaluator understands: `U` for sorting, or the GDD planning shapes `P1`–`P4`.
- `capacity: -1` means unlimited.
- Write `"mug:2"` for a two-space (large) item.
- Targets that share a `y` form one row of containers.
- `rectProfile` sets relative width: `narrow`, `wide` or `full_width`.

**How data levels behave:**
- They come after Kitchen 20 in the full build profile.
- They grant no coins and set no anchors, because the GDD economy is untouched.
- They use the same board, rule evaluator, save and result flow as the authored Kitchen.

**What Validate checks:**
- Missing objects, unknown rules, unknown categories, containers without an archetype, and unsolvable boards.
- On sorting boards, any object that fits zero containers or more than one.
- Any character the board font cannot render.

## 4. An area or room

What is data today, in `Content/areas.json`:
- The counter surface (`surface` ID or a sprite at `Art/Surfaces/<id>.png`) and its `surfaceColor`.
- The wall/backsplash.
- Cabinet colors.
- The counter lip.
- Mess (`enabled`, `density` = decals per item, the `decals` set from `crumbs`, `drip`, `ring`), plus per-level overrides in `levelMess`.

Levels choose their area with `areaId`.

**Not yet data-only:**
- A new room's entry on Home and its restoration overview with milestones and anchors. `GameApp.ShowHome` and `KitchenOverviewView` are written for the Kitchen.
- The smallest change: make the overview read region names, anchor levels and composition item lists from `areas.json`, and list areas on Home.
- Restoration anchors grant progress and economy, so they also need a save-safe ID scheme decided against GDD Sections 29–30 before they move to data.

---

## Worked example: add a Teapot

The GDD Kitchen has four categories: Fruit, Vegetables, Drinks and Kitchenware. There is no separate Dishes category, so a teapot is **Kitchenware**. Adding a fifth category such as Dishes is a code change: `CategoryIds` defines the stable category IDs and the instance-ID letters, and the GDD fixes the set.

1. Add to `objects.json`:

   ```json
   { "id": "teapot", "code": "TP", "category": "kitchenware", "color": "#7FB3C8", "relativeSize": 1.3, "material": "ceramic", "tags": ["dish", "tea"], "art": "" }
   ```
2. Make the art. Generate or paint `teapot.png` using the base prompt in `Docs/ArtDirection.md` and this line: *"Round ceramic teapot, glossy pale-blue glaze, short spout, loop handle, small lid knob, empty."* Save it as `Resources/SortEverything/Art/teapot.png`.
3. Optional: show kitchenware in a dish rack. In `containers.json`, change `kitchenware` in `categoryArchetypes` from `cutlery_drawer` to `dish_rack`. Every kitchenware bin then becomes a dish rack.
4. Put the teapot in a level: the example level file above uses it.
5. Run **Sort Everything ▸ Validate Content**. It confirms the teapot has a size, a material, art and enough contrast with the teak counter (ΔE ≥ 25), and that the level is solvable.
6. Play it from **Sort Everything ▸ Scene Browser**, or from **All levels** on Home.
