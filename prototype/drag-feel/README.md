# Sort Everything — Prototype P1: Drag Feel

Unity 6 prototype for **P1** in the GDD ([ch. 11 §11.1](../../docs/gdd/11-mvp-roadmap-risks.md#111-prototype-first-before-the-mvp)):

> *Is moving one object into one bin satisfying on a phone?*
> **Go:** ≥ 7 of 10 testers keep playing past 3 minutes unprompted, and median "satisfying" rating ≥ 4/5.

It also runs experiment **E1** (lift offset 40 dp vs 0 dp × weight lag on vs off) and records its primary metric (minutes played) and guardrail (mis-drop rate).

The prototype contains no rules beyond colour, no timer and no fail state. It's endless rounds of 10 toy objects and 3 googly-eyed bins, so the only thing being judged is how the drag feels.

---

## What's in it

| Feature | GDD spec it implements |
|---|---|
| Portrait diorama: pile on a table, 3 bins in the bottom thumb zone | ch. 01 §1.1 |
| Drag with **40 dp lift offset**, 12 dp pickup padding, topmost-object pick priority | ch. 01 §1.4 drag-feel table |
| **Weight felt in the drag**: follow spring 25 ms (mass 1) → 110 ms (mass 5); mass 4/5 sag 6/12 dp and swing like a pendulum | ch. 01 §1.4 |
| Pickup pop: scale to 1.15×, landing shadow projected straight down | ch. 01 §1.4 |
| **Drop assist**: released within 24 dp of a mouth → guided in | ch. 01 §1.4 |
| **Flick-to-throw**: release faster than 1,400 dp/s → ballistic throw ×0.85, with aim assist that bends the arc into a nearby mouth | ch. 01 §1.4 flick spec |
| Counted on entry; correct = gulp squash + note + particles + light haptic; wrong = disgusted face, "PTOO!" spit back to the pile, red flash | ch. 01 §1.5–1.6, ch. 02 §2.5 |
| Bin fills → lid snaps shut, hop, sparkle ring | ch. 01 §1.5 |
| **Sort Melody**: each correct sort plays the next note of a pentatonic melody; combo (< 1.2 s apart) adds harmony; ×5 combo glows the screen edges | ch. 08 §8.2 |
| Round complete: slow-mo, bin cadence, **SORTED!** stamp with shake, NEXT button | ch. 01 §1.7 |
| Colour-blind patterns on every object and rule chip | ch. 08 §8.1 |
| Mobile basics: portrait lock, 60 fps target, 60 Hz fixed physics, safe areas, screen stays on, haptics on iOS (Core Haptics bridge) and Android (VibrationEffect) | ch. 10 §10.1 |

Everything (sprites, sounds, scene) is generated from code. There are no art or audio assets to import.

## Content modes: shapes vs real objects

The second test question is whether sorting **recognisable everyday objects** is more fun than sorting shapes. The drag, bins, feedback and E1 variants are identical in every mode. Only what's on the table changes.

| Mode | What spawns |
|---|---|
| **Shapes** | The original P1 content: tinted geometric shapes with colour-blind patterns |
| **Real** (default) | Recognisable objects from the object library, e.g. a red apple, blue stapler, yellow banana, green plant, purple gift box |
| **Mixed** | Half shapes, half real objects in every round |

Switch modes in the panel (•••) under **Content**: tap **Shapes**, **Real** or **Mixed**. The fourth button cycles the **object pool**:

| Pool | Objects | Colour-sortable |
|---|---|---|
| **Core 30** (default) | The first 30 (food, desk, everyday) | 29 (the orange isn't a bin colour) |
| **Core + Extended** | Plus more food, office, home, toys, electronics, nature | 67 |
| **Everything** | Plus animals and fantasy | 78 |

Switching starts a new round at once, and the choice is saved between launches.

In real-object rounds: no object appears twice in a round, objects from the last two rounds are avoided, categories are spread, and every round has at least one heavy object (mass 4–5) and two light ones (mass 1–2), so weight lag stays testable. With Core 30 alone, about 43% of a round's objects were also in one of the last two rounds, because the pool is small. Core + Extended brings that to ~0%.

### The object library

| File | Role |
|---|---|
| `Scripts/Content/ObjectDefs.cs` | `ObjectDef` plus enums: id, display name, category, sort colour, mass 1–5, size class, material, room, tier, tags, special (reserved) |
| `Scripts/Content/ObjectLibrary.cs` | The database: 103 one-line definitions, with queries such as `ColorSortable(pool, colour)` |
| `Scripts/Content/ObjectDrawings.cs` | One small procedural vector drawing per object id |
| `Scripts/Content/VectorPainter.cs` | Signed-distance vector painter: shapes, sticker outlines, toy shading, convex-hull collider tracing |
| `Scripts/Content/ObjectArt.cs` | Turns drawings into cached Unity sprites (160 px) and polygon colliders |
| `Scripts/Content/RoundContent.cs` | Picks objects for a round's colour slots (no repeats, variety, mass spread) |
| `Scripts/Content/ContentSettings.cs` | Current mode and pool (PlayerPrefs) |

**To add an object:** add one `D(...)` line in `ObjectLibrary.cs` and one drawing function, registered in the dictionary in `ObjectDrawings.cs`. Nothing in the drag, bins or rounds changes. Objects with `SortColor.None` (brown cookie, black smartphone, …) never appear in colour rounds; their category, material and room metadata are there for later rule types.

The library and drawings are plain C# with no UnityEngine dependency, so they can be rendered and tested outside Unity.

## Visual style prototype ("colourful digital toy box")

A visual-only pass on top of the prototype: no gameplay, flow or content changes.

| Element | Treatment |
|---|---|
| Typography | **Titan One** for game text and buttons, **Nunito Black** for small text. Chosen from Baloo 2, Nunito, Fredoka, Lilita One, Titan One, Coiny and Luckiest Guy. The panel's stats block keeps the built-in font because it uses ✓/✗, which neither font has. Fonts and OFL licences are in `Resources/Fonts/` |
| Text | Fill + dark outline + small extrusion (`ToyGui.Text`) |
| Buttons | Physical toy buttons: rounded, thick outline, bright face, darker extrusion, soft shadow; squash while pressed, bounce on release (`ToyGui.Button`) |
| Palette | Green primary · blue/purple secondary · gold rewards · orange warning · red danger · grey-purple inactive |
| HUD | ROUND pill, purple ••• button, gold combo pop, gold SORTED! stamp, green NEXT that pops in |
| Panel | Rounded dark card, toy buttons (selected = coloured, unselected = grey-purple), toy slider track and knob |
| World | Soft sky gradient, outlined toy panels for bins, floor and table, outlined eyes, Titan One bin counters with outline, soft ground shadows under bins, stylised drop shadows under objects |
| Objects | Same shapes and art, with toy shading (soft bevel plus key light) on body shapes |

All new code is in `Scripts/Style/`. Existing files only switch sprites, colours and fonts at their drawing sites.

**v2 (layered construction):** every UI element is built as molded plastic layers: bright face → contrasting rim → dark navy outline → coloured extrusion → dark depth band → soft shadow (e.g. NEXT = green face, yellow rim, blue extrusion). Headlines (SORTED!, ROUND, combo, panel title, toast) use sticker text: cream face → thick navy outline → orange depth → soft shadow. Buttons come in Small / Medium / Large depth presets sized to their existing rects. Bin fronts use the same rim + extrusion construction.

## Opening the project

1. Install **Unity 6 LTS** (6000.0.x) through Unity Hub, with the **Android Build Support** and/or **iOS Build Support** modules. A newer Unity 6 patch is fine; accept the upgrade prompt.
2. In Unity Hub choose **Add → Add project from disk** and pick `prototype/drag-feel`.
3. On first open, the editor script creates `Assets/SortEverything/Prototype/DragFeel.unity`, adds it to the build, sets portrait orientation and bundle IDs, and opens it. You can re-run this any time from **Sort Everything ▸ Set Up Drag-Feel Prototype**.

## Playing in the editor

- Set the Game view to a portrait phone resolution (e.g. **1080×2340**) and press **Play**.
- The mouse acts as one finger. Flicks work with fast mouse releases.
- Even in an empty scene, pressing Play auto-creates the prototype.

The editor is only for sanity checks. **Judge feel on a real phone.** The dp sizes, haptics and finger occlusion that this test is about don't exist on a monitor.

## Building to a phone

**Android:** File ▸ Build Profiles ▸ Android ▸ Switch Platform → connect a device with USB debugging → **Build And Run**. The minimum is Android 8.0 (API 26).

**iOS:** File ▸ Build Profiles ▸ iOS ▸ Switch Platform → **Build** → open the generated Xcode project, set your signing team, then run on the device. The minimum is iOS 15.

## Running a P1 session (protocol)

1. **Before handing over the phone:** open the panel (••• top-right), check the variant, tap **Reset stats**, then **Close**. Variants are assigned at random on first launch. Use the A–D buttons to balance them across testers (aim for at least 3 testers per cell for E1).
2. Hand the phone over and say only: *"This is a little sorting game prototype. Play as long as you like and hand it back when you're done."* Don't explain throwing, the bins or anything else.
3. Watch without helping. Note when they first flick (discovered on their own?), laughs or comments, and fumbled drops.
4. When they hand it back, open the panel and note **active play** time and **missed** %. Tap **Copy summary** to grab the JSON.
5. Ask one question: *"How satisfying was moving the objects, from 1 (not at all) to 5 (very)?"*
6. **Go / kill:** at least 7 of 10 testers with active play over 3:00, and median rating at least 4. If it's a miss, tune (see below) and run another round of testers. The GDD says to iterate on feel before building anything else.

### Reading E1

| Metric | Where | Meaning |
|---|---|---|
| **Active play** (primary) | panel / `activeSeconds` | Time with an input in the last 10 s, i.e. how long they kept playing |
| **Missed** % (guardrail) | panel / `misDropRate` | Releases below the table that didn't land in any bin within 2 s |
| Assisted drops | `assistedDrops` | How often drop assist rescued a release |
| Flicks / hits | `flicks`, `flickHits` | Whether throwing was discovered, and whether it's controllable |
| Avg drag | `avgDragSeconds` | Speed of sorting. Heavy-lag variants should be a little slower without more misses |

## Tuning panel (observer only)

Tap **•••** (top-right). Testers never see the variant name; only the panel shows it.

- **E1 variants:** A (GDD spec), B (no lift), C (no weight lag), D (neither).
- **Sliders:** lift offset, follow time for mass 1 and mass 5, sag, pickup scale, hit padding, drop assist, flick threshold, throw scale, aim assist, max tilt. Changing any slider marks the session "custom" in the logs.
- **Toggles:** Weight lag, Juice (squash, particles, faces, melody, shake), Sound, Haptics, Debug.
- **Content:** Shapes / Real / Mixed and the object pool (see above).
- **Debug** overlays the hit areas (cyan), drop-assist zones (yellow), bin mouths (green) and the container-zone line (pink), labels each object with its name, and shows the last release speed in dp/s. Use it to tune the flick threshold against real thumbs.
- Settings persist between launches.

## Logs

Written to `Application.persistentDataPath`:

| File | Contents |
|---|---|
| `dragfeel_<session>.csv` | One row per event: pickup, release (type, speed in dp/s, drag time and distance, outcome), round complete, next tapped, variant changes |
| `dragfeel_sessions.jsonl` | One summary line per session, updated on every round and when the app is backgrounded or quit. Includes `contentMode` and `objectPool` |
| `dragfeel_objects_<session>.csv` | Per object (`apple`, `stapler`, `shape_circle`, …): category, colour, pickups, releases, correct, wrong, missed, assisted, flicks |

Every event row in the CSV also carries `objectKey`, `objectCategory`, `sortColor` and `contentMode`. That makes it possible to see which objects get picked up, missed or mis-sorted, and which categories do best.

To get them off the device:
- **Android:** `adb pull /sdcard/Android/data/com.sorteverything.dragfeel/files/`
- **iOS:** Finder (or iTunes) ▸ the device ▸ Files ▸ *Sort Everything - Drag Feel*. File sharing is switched on by a post-build step.
- **Quickest:** the panel's **Copy summary** button, then paste into a notes app.

## Code map

`Assets/SortEverything/Prototype/Scripts/`

| File | Role |
|---|---|
| `Bootstrap.cs` | Entry point: mobile settings, camera, wires up the systems |
| `FeelConfig.cs` | All tunables (GDD defaults) and E1 variants; saved in PlayerPrefs |
| `DragController.cs` | Touch/mouse input, pickup priority, follow spring, sag/swing, drop assist, flick + aim assist, landing shadow |
| `SortObject.cs` | Toy object: physics root and a squash-able visual child; impact thuds |
| `Bin.cs` | Container: count-on-entry, gulp/spit, face and eyes, lid, counter |
| `RoundDirector.cs` | Layout from dp and safe areas, rounds, combo, melody, completion sequence |
| `Juice.cs` | Spring squash, pooled particles, capped screen shake |
| `AudioKit.cs` | Synthesised SFX and pentatonic notes |
| `Haptics.cs` + `Plugins/iOS/SEHaptics.mm` | iOS UIImpactFeedbackGenerator, Android VibrationEffect |
| `Telemetry.cs` | CSV events and session summaries |
| `Hud.cs` | HUD, stamp, NEXT, tuning panel, debug overlay (IMGUI) |
| `Editor/PrototypeSetup.cs` | First-open project setup |
| `Editor/IosLogAccess.cs` | iOS Info.plist file-sharing flag |

## Deliberately not in this iteration

Sorting by anything other than colour. The category, size, material, room and tags metadata exists for later rules, but no rule uses it yet.

## Deliberately not in P1

Rules beyond colour, timers, Oops/fail states, special objects, Inspect (hold) and Poke (tap), meta, economy, ads and analytics SDKs. P2 (the twist beat) and P3 (physics fairness) build on this project once P1 passes.

## Status

The runtime scripts compile cleanly with the Android, iOS and editor defines against Unity **2021.3** reference assemblies (the closest available where this was written). The Unity 6 API renames they touch (`linearVelocity`, `linearDamping`) sit behind `#if UNITY_6000_0_OR_NEWER`. The project has **not yet been opened in the Unity editor or run on a device**, so treat the first open as a smoke test. Any issues are likely to be in the editor setup script (not compile-checked) or in visual sizing that needs a tweak.
