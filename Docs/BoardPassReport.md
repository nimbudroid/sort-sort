# Board & art pass + content framework: report

Branch: `claude/sort-everything-kitchen`. The brief's branch placeholder `[branch name]` was not filled in, so I stayed on the current branch. **Nothing is committed or pushed**, as the brief asks.

Screenshots are in `Docs/Screenshots/board-pass/`, referred to below by file name.

## 1. What changed, by file and class

### Part A: presentation only

None of these touch rules, categories, counts, capacities, rewards, progression, timers or the save format.

**Layout**
- `Presentation/BoardLayout.cs` (new): **`BoardLayoutConfig`**
  - Loads `Resources/SortEverything/BoardLayout.json`.
  - This is the brief's FeelConfig / BoardLayout asset.
- `Presentation/BoardLayout.cs` (new): **`BoardLayout`**, the one board layout system. It derives from the safe area:
  - the zones;
  - the board width;
  - the seeded item pile (relativeSize, ±12°, overlap of at most 20%);
  - the container rows (90% span, even gutters, at most 3 per row, wrapping to further rows);
  - the cells that sorted items settle into, filled bottom row first.
  - The board, the Scene Browser preview and the tests all use it.

**Board view**
- `Presentation/BoardView.cs`, **`BoardView`** (rewritten on the same API):
  - Countertop room: one plank direction, a lip with a highlight and a shadow, and a painted cabinet with door seams.
  - No translucent panel over the counter.
  - Draw order: pile underneath; container backs, then items, then container front lips; drag on top.
  - `RestOn` gives each item its size, tilt and touch area on the counter or in a bin.
  - `CleanShine` runs the shine sweep.
- `Presentation/BoardView.cs`, **`TargetView`**: a real container built from a data archetype.
  - Layers: ground shadow, body and interior, optional weave, cutlery dividers or fridge light, a sticker tag (icon and name), and a front lip over the items.
  - The usage pill sits on the lip, and a drawn ✓ stamp appears when done.
  - States: idle; hover-valid (warm glow, rim highlight, the lip dips); accept (a squash bounce); reject (a soft wobble and a neutral "Doesn't go here" note, no red).
- `Presentation/BoardView.cs`, **`ObjectView`**:
  - Two shadows from the top-left light: a tight contact shadow and a soft drop shadow.
  - Lifts to 1.08 when dragged, with the shadow moving out and softening.
  - Settles with a squash on drop; size comes from metadata.

**Containers and mess**
- `Presentation/ContainerArt.cs` (new): procedural looks for basket, fridge door shelf, crisper drawer, cutlery drawer, dish rack and tray.
  - Same outline weight and color as the objects, same light direction.
  - Sprite overrides come from data.
- `Presentation/Fx.cs`, `Fx.Decal`: crisp toy-style crumbs, a juice puddle and a coffee ring, all with the shared outline.
- `Presentation/Fx.cs`, `MessLayer` (rewritten): each decal belongs to one item and fades with a sparkle when that item is sorted.
  - Density and decal set come from area/level data.
  - The old blurry blobs and the dusty haze overlay are removed.

**Board screen**
- `Presentation/BoardScreen.cs` (smallest changes that still meet the brief):
  - Header with pause, level name, `Kitchen n/20`, a progress bar and `x/y`.
  - One instruction line, which tutorial text replaces and then hands back.
  - Zones come from `BoardLayout`.
  - Sound is chosen by object material.
  - The clean shine plays on completion.
  - Debug hooks (`DebugHoverValid`, `DebugReject`, `DebugSortFraction`, `DebugDismissOffers`, `DebugCancelDrag`) drive the real Accept and Reject paths. The project has no debug panel, so the capture test and QA use these.

**Art, font, sound and galleries**
- `Presentation/KitchenArt.cs`: adds `Surface(id, color)`, `ButcherBlock` (teak counter) and `Stone`.
- `Presentation/UIKit.cs`: UI font is bundled **Lato** (`Resources/SortEverything/Fonts`, SIL OFL; license file included).
- `Presentation/AudioFeedback.cs`: placement sounds for fruit, plastic, carton, metal, ceramic, glass and wood; a softer reject sound.
- `Presentation/GameApp.cs`, `KitchenOverviewView.cs`: the "✓" text glyphs are replaced (Lato has no ✓). Done stamps use a drawn check. Boot loads data levels.
- `Editor/SceneBrowser.cs`, `Editor/AssetGallery.cs`, and the in-game **All art** screen:
  - Previews come from `BoardLayout`.
  - They show the new containers, lips, mess decals and counter.

### Part B: content framework

**Data**
- `Domain/ContentData.cs` (new): engine-free DTOs for objects, container archetypes, areas (surface, wall, cabinet, mess) and data levels.
- `Domain/ContentData.cs`, `DataLevelBuilder`: turns data levels into the existing `LevelDef`/`BoardDef`, so there is still one evaluator and one level flow.
- `Runtime/ContentDb.cs` (new): loads `Resources/SortEverything/Content/*.json` and `Content/Levels/*.json`.
- `Domain/KitchenCatalogue.cs`:
  - `Catalogue.CreateKitchen(profile, dataLevels)` installs data levels after Kitchen 20, in the full profile only.
  - Adds `SourceGridFor(n)`.
- `Domain/Definitions.cs`: `LevelDef.InstructionOverride`.
- `Domain/Strings.cs`: `Register` and `AllValues`.
- Content files: `objects.json`, `containers.json`, `areas.json`, `BoardLayout.json`.

**Validation, tests and docs**
- `Editor/ContentValidator.cs` (new): the **Sort Everything ▸ Validate Content** menu, run by a test too (see section 4).
- Tests:
  - `Tests/EditMode/BoardPassTests.cs` (8 tests): the validator, plus layout guarantees for every board at all 4 sizes.
  - `Tests/PlayMode/FlowTests.cs`: Level 1 → complete → result → tap Next; replay without a second grant.
  - `Tests/PlayMode/BoardPassCapture.cs`: explicit capture run.
- `Docs/ContentAuthoring.md`: checklist with a worked Teapot example. README updated.

## 2. A1.8 and A1.10

**A1.8: ghosts or dirt?** They were intentional dirt from the previous feel pass. `MessLayer` drew soft "blob" sprites (stains, dust, spills), and a dusty haze panel sat over the room. They are redesigned per A3:
- Crisp outlined crumbs, a drip puddle and a coffee ring.
- Each decal is tied to an item.
- The haze panel is removed.
- The mess is behind a data flag that defaults on: `BoardLayout.json` `messEnabled`, plus `areas.json` `mess.enabled`.

**A1.9: glare spots.** These were the soft white glows marking empty slots in each bin. They are removed; bins now show their contents instead.

**A1.10: flat objects.** None. All 18 sortable objects render through the sculpted renderer (`SpriteFactory`). Screenshot 1's flat icons came from an older build. The small category icons on bin tags reuse the sculpted object sprites (apple, carrot, bottle, fork).

## 3. Layout values

Tune everything in `Assets/SortEverything/Resources/SortEverything/BoardLayout.json`.

**Zones (share of safe-area height)**

| Zone | Share |
|---|---|
| Header | 8% |
| Instruction | 5% |
| Countertop | 55% |
| Containers | 32% |

The shares are normalized, so they always fill the screen.

**Board width**
- min(safe width, 520). On a landscape screen it is also limited to 0.78 × height, centered, with the room bleeding to the sides.

**Item pile**
- Base size = √(counter area × 0.36 / Σ relativeSize²).
- It is clamped so the smallest item is at least 56 and the largest at most 96. In a very dense pile it falls back to the 48 touch floor.
- Touch area = max(48, 0.9 × drawn size).
- Rotation ±12°; overlap at most 20%.

**Containers**
- Rows span 90% of the board width, with a 10-unit gutter and 8-unit row gap.
- At most 3 bins per row.
- **Overflow rule chosen:** wrap to more rows, then scale the items inside bins down to a 30-unit minimum.
- Items in bins: 0.9 × cell, between 30 and 66 units.
- Drag lift: 1.08.

**Measured on every authored board**

| Screen | Board | Header / instruction / counter / bins | Smallest counter item | Smallest touch area | Smallest item in a bin |
|---|---|---|---|---|---|
| 360×800 | 360 | 64 / 40 / 440 / 256 | 56 | 50 | 36 |
| 393×852 | 393 | 68 / 43 / 469 / 273 | 57 | 51 | 41 |
| 411×914 | 411 | 73 / 46 / 503 / 292 | 60 | 54 | 45 |
| 1024×768 (landscape) | 520 | 61 / 38 / 422 / 246 | 60 | 54 | 45 |

- Kitchen 1's six items draw at 74–96.
- The measured pile overlap is 0%, so the pile currently uses none of the 20% allowance.

## 4. Screenshots and checklist

**What was captured**
- File names: `after_L<level>_<size>_<state>.png`.
- Levels: L01 (6 items), L11 (mid level, P2) and L13 (densest planning board: 12 items, 4 bins in 2 rows).
- Sizes: 360x800, 393x852, 411x914 and tablet1024x768. Captures use canvas units, which are about dp.
- States at 360x800: `1idle`, `1idle_offer_closed` (planning boards), `2drag_over_valid`, `3after_reject`, `4half_sorted`, `5complete_shine`, `6result`.
- At the other sizes: idle, half sorted and complete.
- Before/after pairs: `pair_L01_<size>_before_after.png`.

**Checklist**

| Check | Result | Screenshots |
|---|---|---|
| One instruction line only, no overlap with the play area | **Yes** | `after_L01_360x800_1idle`, `pair_L01_360x800_before_after` |
| No ghost blobs, glare spots or translucent panel | **Yes** | `after_L11_360x800_1idle` |
| All items sculpted, same outline, same light direction, all with contact shadows | **Yes** | `after_L13_360x800_1idle_offer_closed` |
| Every item's hit area ≥ 48 at 360 width | **Yes**: the smallest is 50 at 360×800; asserted for every board and size in `A2_EveryItemHitArea_AtLeast48` | — |
| Bins span 85–92% of width and read as real containers | **Yes**: 90%, asserted in `A2_BinsSpan85To92Percent…` | `after_L13_411x914_4half_sorted` |
| Sorted items visible inside containers | **Yes** | `after_L11_360x800_6result`, `after_L01_360x800_5complete_shine` |
| All level strings render, including the em dash | **Yes** | See below |

On the em dash:
- The validator reads the bundled font's character table and checks every string the game shows; the test passes.
- I also checked it in the **WebGL build** in the browser pane: "Your House · Kitchen — 5/20 scenes sorted" renders, where it was a gap before. Editor captures can't prove this, because the editor falls back to system fonts.

**Automated**
- EditMode **37/37** pass:
  - the 29 existing tests;
  - 7 new board-pass tests, including the validator running clean on all 21 authored boards;
  - 1 data-level test.
- PlayMode: boot smoke and the new **flow test** pass. Level 1 completes, the result screen waits for an explicit Next, Next opens Kitchen 2, and replaying Level 1 keeps the wallet at 50.
- No B3 rule shapes were added, so there are no new rule-shape tests.

## 5. Conflicts and how I resolved them

1. **System names.** The brief's DragController, Juice, AudioKit, Haptics, FeelConfig, Sculpted/ObjectArt, ToyStyle/ToyGui and debug panel don't exist under those names. I mapped them to `BoardScreen`'s pointer code, `ObjectView` springs and `Fx`, `AudioFeedback` (which includes haptics), `BoardLayoutConfig`, `SpriteFactory` and `UIKit`. There is no debug panel, so I added debug hooks instead.
2. **Missing sources.** `Sort Everything Prototype.html` and the two screenshots were not provided. I used the GDD, the brief's descriptions, and my own "before" captures of the current build (`before_*`).
3. **Pile placement.** GDD §25 says the counter uses the authored source grid with fixed rotations (−10/+7/−4/+9). The brief asks for a seeded natural pile within ±12°. I followed the brief, since this is art direction. The authored grid still seeds and biases each item's spot, and all rotations stay within ±12°.
4. **Bin geometry.** GDD §25 sets target card positions and sizes; the brief wants bins spanning 85–92% of the width. The authored rows and relative widths are kept but rescaled to a 90% span. Accepts, capacity and slots are unchanged.
5. **Container types.** GDD §21 lists a *Vegetable basket* and a *Drinks crate*. The brief asks for a crisper drawer and a fridge-door shelf. I followed the brief. Switching back is a data edit in `containers.json`.
6. **Categories.** The brief has separate Utensils (drawer) and Dishes (dish rack); the GDD has one Kitchenware category. Kitchenware uses the cutlery drawer. The dish rack archetype exists but no kitchen category uses it.
7. **"Fits more than one bin" (decided: keep the planning-board exception).** B2 says to fail when an object fits more than one bin. GDD planning boards (P1–P4) share containers on purpose; that is the puzzle. The validator therefore requires **exactly one** home on sorting boards and **at least one** on planning boards. Enforcing it everywhere would reject 9 GDD planning boards (Levels 6, 8, 11–14, 16, 19 and the optional board of Level 20). Confirmed by you: keep the exception.
8. **Ready banner.** The old ready banner showed "Your House · Kitchen — rule". The brief allows only one instruction line, so the place now shows in the header ("Kitchen 1/20") and the line carries the rule or tutorial.
9. **Counter color.** The old honey wood was the brief-mandated "adjust the surface" case: the juice carton and banana had low contrast (ΔE 29/36). The counter is now mid-tone teak `#A87653`. Every item has ΔE ≥ 41 against it, and the validator enforces ΔE ≥ 25.
10. **✓ glyph.** The bundled font has no ✓. Text checkmarks became words ("Sorted", "· in use", "Restored ·"), and the done stamps are drawn.

## 6. Not verified

- **Real devices.** Captures use canvas units at each size, not device pixels on real phones. Touch feel, haptics and audio were not checked on hardware; the tests and captures run in the editor's player loop.
- **Tablet landscape.** The game is portrait-only, and `Screen.orientation` is locked on devices. The 1024×768 captures show the portrait column centered with the room bleeding to the sides.
- **Your open editor.** All verification ran on a copy of the project. To see it in your Unity editor, stop Play and let it recompile.
- **Audio.** The material sounds and the clean-shine timing were not heard or watched at full frame rate. The captures are stills.
- **Mid-state overlay.** The "half sorted" stills can catch the end of a fading "Nice!" or "Tidy!" word.

## 7. B3 items

No B3 boxes were ticked, and the object list is still the template placeholder, so I added **no new containers, objects, rules, keepsakes or junk-drawer levels**.

The framework is ready for them:
- New objects, containers and levels are data-only.
- The dish rack archetype already exists.
- New rule shapes still need code in the single `RuleEvaluator`. Each would be a new `template` value plus its tutorial line, as the brief describes.
