# 10 — Mobile Platform, Technical Architecture & Analytics

[← Index](README.md)

---

## 10.1 Mobile platform requirements

Sort Everything is a **native mobile game for iOS and Android phones**, played in **portrait, one-handed, in short sessions**. Every system below is designed for that context first; tablets are supported as scaled-up phones, and there is no PC/console version in scope.

### Target devices

| | iOS | Android |
|---|---|---|
| Minimum OS | iOS 15 | Android 8.0 (API 26) |
| Minimum device ("Low" tier) | iPhone 8 / SE 2 | 3 GB RAM, Snapdragon 450 / Helio P22 class (e.g. Galaxy A10-era) |
| Reference device ("Mid") | iPhone 11 | Galaxy A52 / Pixel 6a |
| High tier | iPhone 14+ | Flagships 2022+ |
| Tablets | iPad (scaled layout: wider Mess Zone, containers stay ≥ 64 dp, max 5 per row) | Android tablets, same rule |

### Performance budget

| Metric | Low tier | Mid/High tier |
|---|---|---|
| Frame rate | Locked 30 fps (physics stays 60 Hz fixed step) | 60 fps; 120 Hz ProMotion optional, physics stays 60 Hz |
| Max simultaneous dynamic objects | 40 | 60 (Big Mess shaft streams objects in/out of the active window) |
| Draw calls (gameplay) | ≤ 60 | ≤ 120 |
| Memory (peak) | ≤ 450 MB | ≤ 700 MB |
| Cold start to Level 1 playable | ≤ 6 s | ≤ 4 s |
| Level load | ≤ 0.8 s | ≤ 0.4 s |
| Battery | ≤ 8% per 30 min on Mid reference at 60 fps | — |
| Thermal | No throttling within 20 min continuous play on Mid tier; automatic quality drop (particles −50%, shadows off) on thermal warning | — |

**Device tiering** is detected at first launch (GPU family, RAM, a 2-second benchmark during the splash) and remotely overridable.

### App size

| Component | Budget |
|---|---|
| Initial store download | **≤ 150 MB** (stay under cellular download warnings; Android AAB base ≤ 150 MB) |
| Bundled content | L1–60 + 8 themes, all audio for those themes |
| On-demand content | Remaining themes, chapters, events via Addressables from CDN, downloaded in the background over Wi-Fi first, cellular allowed for the *next* chapter only |
| Per theme pack | ≤ 6 MB (compressed meshes/textures ASTC/ETC2, audio Vorbis/AAC) |

### Screen & input
- **Portrait only**, locked. Supports aspect ratios 16:9 to 21:9 (tall phones get a taller Mess Zone; container row never moves out of thumb reach).
- **Safe areas:** HUD respects the notch/Dynamic Island/punch-hole cutouts; the container row respects the home-indicator and Android gesture-navigation inset + 16 dp. No interactive element within 16 dp of a screen edge (prevents back-gesture conflicts on Android).
- **Touch:** Unity Input System, enhanced touch; support for up to 2 simultaneous touches; touch sampling interpolation for 120 Hz screens.
- **Haptics:** iOS Core Haptics; Android `VibrationEffect` predefined effects with amplitude fallback; disabled automatically in low-power mode.
- **Font scaling:** UI respects OS text size up to 130%; rule banners have a hard 6-word limit for this reason.

### Mobile lifecycle & interruptions
- **Pause on interruption:** Any focus loss (call, notification shade, app switch, Control Centre) pauses the level instantly, including timers and fuses; resume shows a 3-2-1 countdown on timed levels.
- **Kill-safe:** Progress saved on every level complete and on background; an in-progress level is not resumed after the OS kills the app (levels are short) — the player simply restarts it, with no penalty and no streak loss.
- **Offline-first:** The entire campaign is playable offline. Online is needed only for Daily Sort leaderboards (the puzzle itself is pre-downloaded 7 days ahead), IAP, ads, and cloud save sync. Ads gracefully skip if offline (no interstitial = no penalty).
- **Audio:** Respects the iOS silent switch for music/SFX (ambient session category), ducks for other apps' audio, pauses on headphone disconnect.

### Notifications (opt-in, max 1 per day)
| Trigger | Message example | Timing |
|---|---|---|
| Daily Sort available | "Today you're sorting… a Vampire's Fridge. 🧛" | Player's usual play hour (learned), not before 9:00 local |
| Streak at risk | "Your 12-day streak ends in 3 hours!" | 3 h before local day end |
| Next level teaser | "Level 88 is waiting. It's *alive*." | 24 h after last session, max twice per week |
| Event start | "Halloween Sorting is here 🎃" | Event day 1 |

The permission prompt is shown after the player completes their **first Daily Sort** (when the value is obvious), with a pre-prompt explaining what we'll send.

### Store & platform compliance
- iOS App Tracking Transparency prompt after L5 with a friendly pre-prompt; game fully functional without consent.
- GDPR/UK GDPR/CCPA consent via the mediation platform's CMP (Google UMP-certified) shown at first launch where legally required.
- Age gate: target audience 13+ (avoid child-directed classification, which limits ad monetization); design keeps content family-friendly regardless.
- Platform game services: Game Center & Google Play Games for achievements and friend leaderboards; Sign in with Apple / Google only for cloud save (optional, never on first launch).
- Restore Purchases in Settings (App Store requirement); IAP receipts validated server-side.
- Store assets: portrait screenshots built around the four recognisable elements (Ch. 08 §8.1), 15–30 s app preview video built from real gameplay.

## 10.2 Engine & architecture overview

**Unity 6 LTS, URP (mobile renderer), C#.** Gameplay physics in Unity Physics 2D (Box2D) with fixed timestep.

```
┌──────────────────────── CLIENT (Unity) ─────────────────────────┐
│                                                                  │
│  Content Layer (data, no code)                                   │
│   ObjectDefs · ContainerDefs · ThemeDefs · LevelDefs · EventDefs │
│        ▲ loaded via Addressables (local bundle + CDN catalog)    │
│        │                                                         │
│  Core Systems                                                    │
│   ┌────────────┐ ┌────────────┐ ┌───────────┐ ┌───────────────┐  │
│   │ Object Sys │ │ Container  │ │ Rule      │ │ Physics       │  │
│   │ (behaviour │ │ Sys (mods, │ │ Engine    │ │ (deterministic│  │
│   │ components)│ │ sensors)   │ │ (predicate│ │  2D, sensors) │  │
│   └────────────┘ └────────────┘ │  eval)    │ └───────────────┘  │
│   ┌────────────┐ ┌────────────┐ └───────────┘ ┌───────────────┐  │
│   │ Level      │ │ Input &    │ ┌───────────┐ │ Replay /      │  │
│   │ Director   │ │ Feel       │ │ Feedback  │ │ Recorder      │  │
│   │ (twists,   │ │ (drag/     │ │ (VFX/SFX/ │ │ (inputs+seed) │  │
│   │  events)   │ │  flick)    │ │  haptics) │ └───────────────┘  │
│   └────────────┘ └────────────┘ └───────────┘                    │
│                                                                  │
│  Services: Save · Economy · Analytics · Remote Config · Ads · IAP │
│            Localization · Notifications · Leaderboards           │
└──────────────────────────────────────────────────────────────────┘
          │ HTTPS                                  ▲ CDN
          ▼                                        │
┌──────────── BACKEND ────────────┐   ┌────── CONTENT PIPELINE ──────┐
│ Cloud save · Daily seeds ·      │   │ Level Editor (Unity) ·       │
│ Leaderboards + replay verify ·  │   │ Generator + Validator (batch │
│ Remote config · IAP validation  │   │ headless Unity) · Asset build│
│ (e.g. Unity Gaming Services or  │   │ → Addressables → CDN         │
│  Firebase + small custom svc)   │   └──────────────────────────────┘
└─────────────────────────────────┘
```

## 10.3 Object system

**Data-driven, component-composed.**

```csharp
// ScriptableObject authored in editor, serialised to the content catalog
class ObjectDef {
    string id;                 // "egg_white"
    AssetRef visual;           // prefab/mesh + material
    ColliderShape[] colliders; // ≤ 4 convex shapes
    Material material;         // Plastic | Glass | Metal | Organic | Fabric | Paper | Goo | Stone
    SizeClass size;            // S | M | L
    int massClass;             // 1–5
    string[] tags;             // ["food","egg","breakable","white","round"]
    BehaviourConfig[] behaviours; // e.g. Fragile{breakImpulse=4.0}, Perishable{seconds=12, spoilsTo="egg_rotten"}
    LocKey nameKey, museumDescKey;
}
```

- Each behaviour is a runtime `IObjectBehaviour` component with lifecycle hooks: `OnSpawn, OnGrab, OnDrag, OnRelease, OnCollision(impulse, other), OnEnterContainer, OnTick(dt), OnStateChange`.
- Behaviours communicate only through **events and object state flags** (`contaminated`, `frozen`, `burning`, `asleep`, `revealed`, `spoiled`), never by referencing each other. That's what makes combinations safe.
- New behaviours require a client update; new *objects* using existing behaviours do **not** (pure data via CDN).

## 10.4 Container system
- `ContainerDef`: visual, mouth sensor shape, capacity, weight limit, rule chip, modifiers (`Moving`, `Swapping`, `LidCycle`, `Locked`, `Leaky`, `Tippy`…).
- Mouth sensor = trigger collider; counting happens on entry (Ch. 02 §2.5 rule 2).
- Container modifiers are components like object behaviours, with the same event-driven contract.

## 10.5 Rule engine
- Rules are **predicates over object tags and state**, defined in a tiny expression format so designers and the generator can author them without code:

```
container[0].accepts = tag:fruit AND NOT state:contaminated
container[1].accepts = state:contaminated OR behaviour:radioactive
container[2].accepts = tag:metal                      # inferred: chip shows "?" + one example
level.sequence       = order_by(size, asc)            # sequence rule
level.global         = invert                         # opposite day
```

- The same predicate engine runs in the client, the generator, and the solver — one source of truth for "correct".
- Rule changes (Ch13) are timeline events in the level that swap predicates and re-stamp chips.

## 10.6 Physics system
- Fixed timestep 1/60 s; `Physics2D.simulationMode = Script` stepping from our own loop for determinism and replay.
- Determinism: avoid frame-rate-dependent forces; seeded `System.Random` per level; identical physics settings on all platforms. Cross-device float differences are tolerated for campaign play; **Daily Sort leaderboard verification** re-simulates on the server with the same Unity build in headless mode and accepts results within tolerance (completion + time within 3%).
- Custom layers: Pile objects, Held object (ignores pile collisions while lifted — prevents dragging through things knocking everything over, except Radioactive/Sticky which use *contact while held* deliberately), Containers, Walls, Sensors.
- Sleep thresholds tuned for 1.2 s settle; continuous collision detection only for thrown objects.

## 10.7 Level data & content delivery
- Levels: JSON (schema in Ch. 05 §5.3), compiled to a compact binary per chapter pack.
- **Addressables + remote catalog**: themes, objects, chapters, events, dailies ship from CDN. The client checks the catalog at launch (non-blocking) and prefetches the next chapter.
- **Content versioning:** each level has `id` and `rev`. Analytics logs both, so a rebalanced level's data is separated.
- **Hot-fixing levels:** a broken level can be replaced or reordered via remote config within an hour, no store submission.

## 10.8 Procedural generation (where it runs)
- **Offline, not on device.** The generator and validator run as a headless Unity batch job (same physics, same rule engine) on build servers. Outputs are reviewed and published as content packs.
- **Why not on device:** validation needs hundreds of physics simulations per level; generated-on-device levels can't be curated or reproduced for leaderboards; and on-device generation costs battery.
- The only on-device randomness is cosmetic (particle variation, idle animations) and rare-variant rolls, all seeded.

## 10.9 Save system
- Local save: binary/JSON in persistent storage, written atomically (write temp → rename) after each level and on app background.
- Cloud save (optional login): last-write-wins with per-field merge (max level, union of Museum items, max stars per level, sum-safe currency via server ledger for purchased coins).
- Purchased items are always restorable via store receipts.

## 10.10 Remote configuration & LiveOps
Everything tunable without a build:

| Area | Parameters |
|---|---|
| Ads | Interstitial start level, frequency, cooldown, skip rules, segment overrides |
| Economy | Coin rewards, prices, crate odds and pity timer |
| Difficulty | Timer multipliers, drop-assist radius, dynamic assist thresholds |
| Content | Level order, level replacements, event schedule, teaser texts |
| Experiments | A/B group assignment, feature flags |

Use Unity Remote Config or Firebase Remote Config + A/B Testing; experiments assigned on install and stable per user.

## 10.11 Ads, IAP, localization

| System | Choice | Notes |
|---|---|---|
| Ad mediation | AppLovin MAX or Unity LevelPlay | Bidding-first; rewarded + interstitial only; no banners |
| IAP | Unity IAP (StoreKit 2 / Play Billing 6+) | Server-side receipt validation; Remove Ads as a non-consumable |
| Localization | Unity Localization package; string tables + smart strings | Launch: EN, ES, PT-BR, FR, DE, IT, JA, KO, ZH-Hans, ZH-Hant, RU, TR. All gameplay is icon-driven so localisation load is ~1,500 strings at launch (titles + Museum descriptions are the bulk) |
| Crash/perf | Firebase Crashlytics + Unity Cloud Diagnostics; frame-time sampling per device tier | |

## 10.12 Analytics

### Core events

| Event | Key parameters |
|---|---|
| `session_start` / `session_end` | session_id, length_s, levels_played, device_tier, app_version, network |
| `level_start` | level_id, level_rev, attempt_no, tier, chapter, source (campaign/daily/event/replay), boosters_owned |
| `level_complete` | level_id, rev, attempt_no, time_ms, oops_used, stars, max_combo, boosters_used[], breaks, coins_earned, continues_used |
| `level_fail` | level_id, rev, attempt_no, fail_cause (oops/time/weight/break/escape/contamination/explode/sequence/eaten), progress_pct, time_ms, last_mechanic_involved |
| `level_restart` | level_id, from (fail_screen/pause), time_since_fail_ms |
| `level_abandon` | level_id, progress_pct, time_ms, exit (home/background/kill) |
| `level_skip` | level_id, attempts, method (ad/ticket) |
| `object_misplaced` | level_id, object_id, wrong_container_rule, correct_container_rule (sampled 10%) |
| `twist_triggered` | level_id, twist_type, progress_pct, time_ms |
| `ad_shown` / `ad_completed` | format, placement, level_id, ecpm (from mediation), session_ad_count |
| `ad_offer_declined` | placement |
| `reward_claimed` | source (level/crate/daily/streak/milestone), items |
| `booster_used` | booster, level_id, attempt_no, source (owned/bought) |
| `iap_view` / `iap_purchase` | product_id, price, placement, trigger |
| `daily_start` / `daily_complete` | date_seed, official/practice, time_ms, oops, percentile, streak_len |
| `museum_item_added` / `shelf_complete` | object_id, shelf_id |
| `share_tapped` / `share_completed` | type (fail_clip/daily_card/milestone/challenge), level_id |
| `teaser_viewed` | next_level_id, dwell_ms, then (next/home/quit) |
| `tutorial_step` | level 1–10 step markers (first grab, first spit, first twist seen) |

### KPIs and dashboards

| Dashboard | KPIs | Alert thresholds |
|---|---|---|
| **Retention** | D1/D3/D7/D14/D30, by install cohort, country, device tier, ad-exposure cohort | D1 < 38%, D7 < 11% |
| **Engagement** | Sessions/DAU, session length, **levels per session**, levels/DAU, D0 levels reached | Levels/session < 6 |
| **Funnel** | % reaching L5/10/16/26/41/56/100; drop-off per level (churn = last level seen before 7-day inactivity) | Any level with churn > 2× chapter mean |
| **Level health** | First-try win rate vs. band, attempts-to-win, median time vs. band, fail cause mix, booster rate, skip rate, misplacement heatmap per object | Win rate outside band ±15 pts; one object > 10% misplacement |
| **Twist effect** | Fail rate after twist vs. before; retry rate after twist-caused fails | Twist-fail retry rate < 60% (frustrating twist) |
| **Teaser effect** | % teaser views followed by NEXT within 5 s | < 80% |
| **Monetization** | ARPDAU, ad ARPDAU, impressions/DAU by format, rewarded opt-in rate, IAP conversion, ARPPU, Remove Ads conversion, LTV D7/D30 projections | Interstitial-attributable session-end rate > 15% |
| **Daily & social** | Daily participation (% DAU), streak distribution, share rate per 1,000 levels, installs from share links (k-factor proxy) | — |
| **Tech** | Crash-free users (target ≥ 99.7%), ANR rate (≤ 0.3%), fps p10 by tier, load times, download failures | Crash-free < 99.5% |

### Level health loop (weekly ritual)
1. Pull the 20 worst levels by churn and by deviation from win-rate band.
2. Watch 5 recorded replays per level (from opt-in internal/beta testers; replays are input logs, tiny).
3. Fix via data change (timer, object swap, art tweak, reorder) → push via remote config → compare `rev` cohorts next week.
