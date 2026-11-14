# Sort Everything

A portrait mobile sorting game (iOS/Android) built in Unity from the *Sort Everything Game Design and Prototype Specification* (October 2026).

This branch implements the complete **Kitchen**: all 20 exact scenes (21 boards) from GDD Sections 9 and 25, following the GDD's own build order (Section 22, steps 1–3 and the Kitchen part of step 5). The world layer (Living Room, Picnic Lawn, Check-in Hall, Returns Counter, atlas, journey milestones; Sections 29–30) is **not built yet**. See *Status* below.

## Requirements

| Item | Version |
|---|---|
| Unity editor | **6000.6.4f1** (pinned in `ProjectSettings/ProjectVersion.txt`) |
| Input System | 1.20.0 (Active Input Handling = Input System only) |
| uGUI | 2.6.0 |
| Unity Test Framework | 1.8.0 |

> The GDD asks for Unity 6.3 LTS (6000.3). This project was already created on 6000.6.4f1, and 6000.3 isn't installed on this machine, so the code targets 6000.6.4f1. Switching editors needs revalidation.

## Open and play

1. Open the project folder in Unity Hub with editor 6000.6.4f1.
2. If `Assets/SortEverything/Scenes/Bootstrap.unity` is missing, run **Sort Everything ▸ Create Bootstrap Scene**.
3. Open `Bootstrap.unity` and press Play. Set the Game view to a portrait phone resolution (for example 1080×2340).

A brand-new save opens Kitchen 1 directly. Later launches open Home, which has Resume and Continue.

## Configuration (developer only)

`Assets/SortEverything/Resources/SortEverythingConfig.json`

| Field | Default | Notes |
|---|---|---|
| `buildProfileId` | `kitchen_20` | `kitchen_gate_08` (8-scene experience gate) or `kitchen_20` (full single-area Kitchen). `journey_20/32`, `travel_38` and `collection_44` need the world layer, which isn't built yet. |
| `manualDisplaySlots`, `movingTargets`, `forcedPlanningResearch`, `researchExtras` | `false` | Disabled experiments. They're not implemented beyond the flag and the configuration signature. |
| `taskTraces` | `true` | |
| `telemetryEnabled` | `true` | Local JSONL only: `persistentDataPath/SortEverything/telemetry.jsonl`. |

## Editor menu (Sort Everything ▸ …)

- **Validate Catalogue**: expands, validates and solves every board, then writes `Docs/ValidationReport.txt`.
- **Generate Catalogue**: writes the expanded runtime JSON to `Assets/SortEverything/Data/catalogue_expanded.json`.
- **Create Bootstrap Scene**: rebuilds the scene and sets portrait orientation and `com.sorteverything.prototype`.
- **Reset Development Save**: deletes the editor save files after a confirmation dialog.
- **Build Android Development**: writes `Builds/Android/SortEverything-dev.apk`.

## Command line

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity"
```

```bash
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod SortEverything.Editor.CatalogueCommands.ValidateBatch -artifacts "$PWD/Artifacts" -logFile "$PWD/Artifacts/validate.log"
```

```bash
"$UNITY" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testResults "$PWD/Artifacts/editmode.xml" -logFile "$PWD/Artifacts/editmode.log"
```

```bash
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod SortEverything.Editor.BuildCommands.BuildAndroidDevelopment -artifacts "$PWD/Builds/Android" -logFile "$PWD/Artifacts/android.log"
```

The Android build needs Android Build Support (SDK/NDK/OpenJDK) installed through Unity Hub.

## Project layout

```
Assets/SortEverything/
  Scenes/Bootstrap.unity        single scene; the UI is built at runtime
  Scripts/Domain/               pure C# (noEngineReferences): catalogue, rules, solver, hints, progression, save DTOs
  Scripts/Runtime/              atomic save store, session transactions, config, telemetry
  Scripts/Presentation/         board/target/object views, drag, HUD, screens, procedural art and audio
  Scripts/Editor/               validator, catalogue export, scene builder, build commands
  Tests/EditMode/               acceptance-scenario tests (Section 27 IDs)
  Resources/SortEverythingConfig.json
Docs/                           validation report, test report, asset manifest
```

### Architecture rules (GDD Sections 14, 24, 26)

- The board model is authoritative. Input proposes a `DropProposal`, `RuleEvaluator` returns Accept / Cancel / SameTargetNoOp / WrongCategory / Full / Locked without mutating anything, and `BoardReducer` returns a new state.
- Every mutation runs through `GameSession.Commit`: clone the save, mutate the candidate, write it once, and publish only if the write succeeds. The final placement, board checkpoint and reward are one transaction.
- Saves are `save.json`, `save.backup.json` and `save.pending.json`, each a SHA-256 checked envelope. The pending file is verified, then promoted with `File.Replace` or a rename fallback that never deletes the only valid copy. At boot the highest valid revision wins. If every file is corrupt, the bytes are kept and the player gets Retry or a confirmed Reset.
- Grants are idempotent: `campaign_complete:<levelId>` (50 coins once) and `cosmetic_purchase:<variantId>`.

## Status

**Implemented**
- All 20 kitchen scenes, built deterministically from the exact compact tables: interleaved source order, every source grid, U/P1/P2/P3/P4 target expansion, unit costs, slots, and the Level 20 optional P4 board followed by the required board.
- One-pointer drag ownership, lift and scale, a stable pickup offset, forgiving drop bounds resolved by closest center then target ID, a neutral hover state, specific rejection reasons (Full / Doesn't go here), board returns and transfers, cancellation on interruption, and pause or backgrounding during a drag.
- Leave for later on planning scenes, per-area cursors, Later Puzzles, Finish Kitchen / Resume Puzzle / Replay Finish for Level 20, Restart Board, Replay, Inspect Scene.
- Calm and Mastery preferences, active-time clock, best and best-unassisted records, Clean Sort and Unassisted Planner badges, Mastery-only simple-board streak.
- The Level 1 drag cue, plus a ghost-hand illustration after 4 seconds. Learn this puzzle offers for shared_capacity_recovery, weighted_units and overlapping_baskets, with skippable ghost demos. A free deterministic hint (Pause, plus a HUD chip after 8 seconds idle) that marks assistance only when shown.
- Kitchen overview with task traces and four anchors. Four optional variants at 75, 100, 125 and 150 coins, with separate preview, purchase and equip. An 18-item collection shelf and the Kitchen Ready / Kitchen Master / kitchen_collection_complete recognitions.
- Settings (preference, music, effects, haptics, reduced motion), the save-error Retry modal, and local telemetry events.

**Placeholder or simplified**
- Art is procedurally drawn: outlined, shaded silhouettes for all 18 assets plus simple target and prop panels. Final sprites can replace any asset by placing `Resources/SortEverything/Art/<assetId>`. This is not the polished art the experience gate requires (GDD Section 23).
- Audio cues and the music loop are synthesized placeholders.
- Before/after profiles: the before state uses the authored rotations, and the after state shows the purpose prop panel plus a micro-action. No bespoke compositions per scene.
- Next-scene previews show the next scene's object icons, not screenshot renders.

**Not built yet**
- World progression (Sections 29–30): House overview with the Living Room, Park/Picnic Lawn, Airport, Bookshop, journey milestones, Places, recommendations, keepsakes, and the 24 extra scenes. The `journey_*`, `travel_38` and `collection_44` profiles.
- Save migrations from schema 2/3 and content 3 (there are no older saves of this build yet).
- PlayMode pointer tests, the device test record, the Android APK, the iOS export, and any player testing.
