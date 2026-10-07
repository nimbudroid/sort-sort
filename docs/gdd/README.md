# SORT EVERYTHING — Game Design Document

**Version:** 1.0 (Pre-production)
**Platform:** **Mobile game** — native iOS & Android phones (tablets supported as scaled phones), portrait-locked, one-handed touch play, offline-first, free-to-play
**Engine (assumed):** Unity 6, 2D physics with 3D-styled visuals
**Genre:** Hybrid-casual physics puzzle
**Business model:** Hybrid (interstitial + rewarded ads, Remove Ads, cosmetics, light boosters, season pass post-launch)

---

## How to read this document

| # | Chapter | What it answers |
|---|---------|-----------------|
| 00 | **This page** | Pitch, pillars, the ten decisions that define the game |
| 01 | [Core Gameplay & Interaction](01-core-gameplay.md) | Screen, moment-to-moment loop, input model, feedback, timings |
| 02 | [Physics System](02-physics.md) | Which physics we use, which we cut, how chaos stays fair |
| 03 | [Special Objects, Containers & Rules](03-special-objects.md) | The mechanic library (objects, container modifiers, environments, rule types) |
| 04 | [Progression, Difficulty, Tutorial & Failure](04-progression-difficulty.md) | Chapter structure, mechanic schedule, difficulty budget, fairness, retry loop |
| 05 | [Level Generation System](05-level-generation.md) | Level grammar, generator, validation, 30+ generation recipes, what is AI vs hand-made |
| 06 | [Level Examples](06-level-examples.md) | 110 concrete levels from "sort two colours" to full chaos |
| 07 | [Retention, Meta, Rewards & Monetization](07-meta-economy-monetization.md) | One-more-level loop, Museum meta, economy, ads/IAP, D1/D7/D30 plan |
| 08 | [Visual, Audio & UX/UI](08-art-audio-ux.md) | Visual language, sound design, every screen and flow |
| 09 | [Content Worlds, LiveOps, Daily & Social](09-content-liveops-social.md) | 70 themes, seasonal events, daily puzzle, sharing |
| 10 | [Mobile Platform, Technical & Analytics](10-tech-analytics.md) | Device tiers, performance/battery/app-size budgets, safe areas, interruptions, notifications, store compliance; Unity architecture, remote content, events, KPIs |
| 11 | [MVP, Roadmap, Competition, Risks & The Magic](11-mvp-roadmap-risks.md) | What to build first, what to cut, risks, experiments, why it works |

---

## 0. Platform: this is a mobile game

Every decision in this document assumes a **phone in one hand, in portrait, for a few minutes at a time** — on a bus, in a queue, on the sofa.

| Mobile constraint | How the design answers it |
|---|---|
| One thumb, portrait screen | Containers (drop targets) sit in the bottom thumb zone; objects lift 40 dp above the finger so it never hides them ([Ch. 01](01-core-gameplay.md)) |
| Small screens, fat fingers | Objects ≥ 56 dp, ≤ 5 containers per row, generous pickup and drop-assist areas |
| 2–10 minute sessions, constant interruptions | 25–60 s levels, instant pause on focus loss, ≤ 1 s retry, offline campaign ([Ch. 10 §10.1](10-tech-analytics.md#101-mobile-platform-requirements)) |
| Low-end Android devices | 2D physics, 30 fps low tier, 40-object cap, ≤ 150 MB install with on-demand content packs |
| Free-to-play app stores | Rewarded + interstitial ads, Remove Ads, cosmetics, light boosters, season pass — no lives/energy ([Ch. 07](07-meta-economy-monetization.md)) |
| Sound often off | Every special object has a visual tell as well as an audio tell; haptics carry feedback |
| Store discovery via short vertical video | Screenshots and fail clips built around googly-eyed bins and absurd objects ([Ch. 08](08-art-audio-ux.md), [Ch. 09](09-content-liveops-social.md)) |

## 1. One-line pitch

> **"Every level, you sort something new — and every level, the game finds a new way to make sorting go wrong."**

Shorthand for the team: **"Ball Sort's simplicity, WarioWare's surprise, Suika's physics."**

## 2. Concept summary

The player's verb never changes: **pick something up and put it where it belongs.** What changes is *what* they are sorting (socks, planets, a vampire's fridge, a burglar's loot) and *what is trying to stop them* (weight limits, fragile eggs, a sneezing cat, a radioactive banana, a ship that will not stop rocking).

Each level is built in two beats:

1. **The Setup** — "Oh, that's easy." A readable pile, obvious containers, obvious rule.
2. **The Twist** — "Oh no." Partway through (typically when 50–70% is sorted) something changes: a new object type drops in, a container starts moving, the lights go out, one object turns out to be alive.

The player finishes, sees a teaser card for what they will sort next ("NEXT: A Dragon's Tax Return"), and taps Next. That curiosity gap — *"what on earth are they going to make me sort next?"* — is the primary retention engine, ahead of any economy.

## 3. Target audience

| Segment | Description | What they want from us |
|---|---|---|
| **Primary** | 18–45, casual-to-mid-core mobile players who play Ball Sort, Match Factory, Royal Match, Fill the Fridge-style ASMR games | Short, satisfying sessions; low cognitive entry; visible progress |
| **Secondary** | Younger adults (16–24) on TikTok/Shorts who watch "satisfying" and "fail" clips | Funny surprises, shareable fails, daily brag-able score |
| **Tertiary** | Puzzle enthusiasts who will chase 3 stars and daily leaderboards | Mastery depth, fair challenge, speedrun-able levels |

Platform norms assumed: global launch both stores, ad-led revenue in first 6 months (~65–75% ads), IAP share growing with the season pass.

## 4. Design pillars

Every feature proposal must name which pillar it serves. If it serves none, it is cut.

| Pillar | Meaning | Test question |
|---|---|---|
| **1. Readable in one second** | A screenshot of any level must be understandable without text. | "Could a player who doesn't read English start this level correctly?" |
| **2. The hand feels good** | The drag-and-drop of one object must be satisfying on its own, with zero rules. | "Is it fun to move one object into one bin in an empty level?" |
| **3. One new thing at a time** | Never more than one *unfamiliar* element per level. | "How many things in this level has the player not seen before?" (must be ≤1) |
| **4. Chaos you caused** | Physics chaos is the consequence of the player's actions or telegraphed events — never random punishment. | "When the player fails, can they say *why* within 2 seconds of the replay?" |
| **5. What's next?** | Content variety is the product. Each level should make the next one feel unknown. | "Would a player screenshot the teaser card?" |

## 5. The ten decisions that define this game

| # | Decision | Alternative considered | Why we chose this |
|---|---|---|---|
| 1 | **Drag-and-drop is the core input**, with flick-to-throw emerging from fast releases | Tap-to-send (Ball Sort style) | Tap is fast but has no tactility and no physics expression. Drag gives weight, aim and control; flick gives skill expression for free. |
| 2 | **Front-view diorama, gravity points down**, objects in a pile above, containers along the bottom | Top-down table view | Gravity, stacking, weight, tipping and bouncing are only readable from the side. Top-down kills physics. |
| 3 | **Themes and mechanics are separate axes.** The *theme* (what you sort) changes nearly every level; the *mechanic* (how sorting goes wrong) changes every chapter. | Theme-locked worlds (20 candy levels, then 20 kitchen levels) | Theme-locked worlds make "what's next?" predictable. Decoupling gives Themes × Mechanics combinations and keeps content production parallel. |
| 4 | **Two-beat levels (Setup → Twist)** as a structural rule for most levels from L5 onward | Static puzzles | Directly produces the "that's easy… oh no" emotion the concept is built on. |
| 5 | **Every system appears as juice before it appears as a rule.** Heavy things thud before weight limits exist; things wobble before tipping matters. | Introduce each mechanic cold with a rule card | Players pre-learn the physics intuitively, so the rule lands as "oh, of course" instead of "what?" |
| 6 | **Containers are characters** (googly-eyed bins that gulp, spit and pull faces) | Neutral containers | Free personality, instant feedback readability, and a recognisable brand in ads and screenshots. |
| 7 | **No lives / energy system at launch** | Candy Crush-style lives | Lives break "one more level" — the exact loop we are selling. Monetize via ads, Remove Ads, boosters and cosmetics instead. |
| 8 | **Museum meta** (you *sort your collection of sorted objects* onto display shelves) | Home decoration meta | Decoration requires a separate art pipeline and fantasy. The Museum reuses the object art we already make and *is itself sorting*. |
| 9 | **Generator proposes, designer disposes.** Procedural generation creates candidates; designers curate the main path; automatic validation gates daily content. | Fully procedural or fully handmade | Fully procedural produces bland levels; fully handmade cannot scale to 1,000+. |
| 10 | **Deterministic physics** (fixed step, seeded, forgiving sensors) | Free-form physics | Determinism enables replays, ghosts, daily leaderboards, solver validation — and fairness. |

## 6. The core experience (never violated)

```
SEE OBJECTS → UNDERSTAND THE RULE → SORT THEM → WATCH THE PHYSICS / CHAOS
      → FEEL SATISFIED → COMPLETE → SEE THE NEXT LEVEL → "ONE MORE."
```

Target numbers that make this loop work:

| Metric | Target |
|---|---|
| Time from app open to first drag (returning player) | ≤ 4 s |
| Time from fail to playable retry | ≤ 1.0 s |
| Time from level complete to next level playable | ≤ 5 s (incl. reward screen) |
| Median level length | 25–40 s (Ch1–3), 40–70 s (later), 90–150 s ("Big Mess" levels) |
| Median D0 session | 18–25 min, ~20–30 levels |

## 7. KPI targets (hybrid-casual benchmarks, to be validated in soft launch)

| KPI | Soft-launch "go" bar | Global-launch target |
|---|---|---|
| D1 retention | ≥ 40% | 45%+ |
| D7 retention | ≥ 12% | 16%+ |
| D30 retention | ≥ 4% | 6%+ |
| D0 playtime | ≥ 20 min | 25 min |
| Levels per DAU | ≥ 15 | 20 |
| ARPDAU (blended) | ≥ $0.08 | $0.12–0.18 |
| IAP payer conversion (D30) | ≥ 1.0% | 2%+ |
| CPI (US, video creatives) | ≤ $0.80 | ≤ $0.60 |

These are typical ranges for the genre, not guarantees; the experimentation plan in Chapter 11 says how each is measured and what we change if we miss.
