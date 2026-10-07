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
- **Debug** overlays the hit areas (cyan), drop-assist zones (yellow), bin mouths (green) and the container-zone line (pink), and shows the last release speed in dp/s. Use it to tune the flick threshold against real thumbs.
- Settings persist between launches.

## Logs

Written to `Application.persistentDataPath`:

| File | Contents |
|---|---|
| `dragfeel_<session>.csv` | One row per event: pickup, release (type, speed in dp/s, drag time and distance, outcome), round complete, next tapped, variant changes |
| `dragfeel_sessions.jsonl` | One summary line per session, updated on every round and when the app is backgrounded or quit |

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

## Deliberately not in P1

Rules beyond colour, timers, Oops/fail states, special objects, Inspect (hold) and Poke (tap), meta, economy, ads and analytics SDKs. P2 (the twist beat) and P3 (physics fairness) build on this project once P1 passes.

## Status

The runtime scripts compile cleanly with the Android, iOS and editor defines against Unity **2021.3** reference assemblies (the closest available where this was written). The Unity 6 API renames they touch (`linearVelocity`, `linearDamping`) sit behind `#if UNITY_6000_0_OR_NEWER`. The project has **not yet been opened in the Unity editor or run on a device**, so treat the first open as a smoke test. Any issues are likely to be in the editor setup script (not compile-checked) or in visual sizing that needs a tweak.
