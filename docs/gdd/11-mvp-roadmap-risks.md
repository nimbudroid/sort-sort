# 11 — MVP, Roadmap, Competitive Positioning, Risks, Experiments & The Magic

[← Index](README.md)

---

## 11.1 Prototype first (before the MVP)

Three cheap prototypes decide whether the MVP is worth building. Each is 1–2 weeks with 1–2 people, built in Unity, tested on phones with 10+ external players.

| # | Prototype | Question it answers | Kill / go criteria |
|---|---|---|---|
| **P1** | **Drag feel** — one empty level, 10 objects, 3 bins, juice only | Is moving one object into one bin satisfying on a phone? | ≥ 7/10 testers keep playing past 3 min unprompted; median rating "satisfying" ≥ 4/5. If not, iterate on feel before anything else |
| **P2** | **Twist beat** — 10 levels with and without mid-level twists | Does the two-beat structure create "one more"? | Twist version: ≥ 25% more levels played voluntarily in 10 min |
| **P3** | **Physics fairness** — weight, fragile, bouncy levels | Does physics feel like *controlled* chaos or random? | ≥ 80% of testers can explain their fail cause after watching the replay; < 15% call a fail "unfair" |

Plus a **CPI test** in parallel: 3–5 video creatives (fail clips, "now sort a radioactive banana", trick shots) on a fake store page or a P1/P2 build. Target US CPI ≤ $0.80 on video networks; if all creatives exceed $1.20, revisit the hook before the MVP.

## 11.2 MVP definition

**Purpose:** prove D1/D7 retention and the core loop with real players in soft launch. Not to prove the full content ambition.

**Team (indicative):** 1 producer, 1 lead designer, 1 level designer, 3 engineers (gameplay, tools/content pipeline, services/monetization), 2 artists (3D objects + UI/VFX), 1 audio (contract), 1 QA (+ outsourced device QA). **Timeline:** ~5–6 months after prototypes.

| Area | MVP scope |
|---|---|
| **Levels** | **100 campaign levels** (Chapters 1–7, up to the L100 milestone), all hand-designed with generator assist |
| **Themes** | **10 themes** (Toy room, Kitchen, Bedroom, Candy, Office, Gym/Construction, Farm, Pets, Wizard school, Airport) |
| **Object types** | **~180** (≈ 18 per theme; each reused across levels) |
| **Mechanics** | Rule types: colour, shape, category, size, two-attribute, pairs, owner, count, odd-one-out, inferred, sequence (11). Object behaviours: heavy, fragile, bouncy, round, slippery, disguised, invisible, nested/gift, mimic, living, sleeping, shy, hungry, key, VIP (15). Container mods: capacity, weight limit, scale, jar, shape slot, lid timer, creature, moving, swapping, conveyor, locked (11). Environment: pour-in, container pop-up, customer rush (3) |
| **Physics systems** | **4**: gravity/stacking, weight, fragility, bounce (+ rolling as a property). No tilt, explosions, sticky, magnetism, buoyancy |
| **Meta** | Level map, stars, coins, Hot Hands streak, Museum v1 (shelves + labels, no Oddities), 15 container skins |
| **Daily** | **Not in MVP** (added for soft-launch phase 2 — needs the validator pipeline hardened first) |
| **Monetization** | Interstitial + rewarded (all placements), Remove Ads, Starter Pack, 4 boosters, coin packs |
| **Tutorial** | L1–10 as specified (Ch. 04 §4.7) |
| **UI** | Gameplay HUD, result screen with teaser, fail screen with instant replay, home, level map, Museum, shop, settings, pause |
| **Tech** | Addressables + remote catalog, remote config, analytics, crash reporting, local save (cloud save optional login), level editor, validator (V1–V10) |
| **Accessibility** | Patterns, Reduce Motion, haptics toggle, left-handed mode |

### Deliberately NOT in the MVP

| Not built | Why |
|---|---|
| Daily Sort + leaderboards | Needs server verification and generator maturity; first prove the campaign |
| Hazards (radioactive, explosive, mold), fire/ice, sticky, magnet, tilt, lights out | Ship the core physics well first; these are Months 1–3 content beats and keep the roadmap exciting |
| Season pass, premium currency | No content cadence yet to justify a pass |
| Friend challenges, ghosts, replay video export | Social is a multiplier on a good game, not a fix for a bad one |
| Infinite Facility / auto-published generated levels | Curation quality must be proven on the main path first |
| Achievements, weekly challenges | Low impact on D1/D7 |
| Cloud save login prompts | Friction on first launch |
| Localization beyond EN + 4 languages (ES, PT-BR, FR, DE) | Soft launch markets only |

## 11.3 Roadmap

| Stage | Timing | Goals | Adds |
|---|---|---|---|
| **MVP / Soft launch phase 1** | Month 0 | Prove retention: D1 ≥ 40%, D7 ≥ 12%, D0 playtime ≥ 20 min | MVP scope; markets: Philippines, Indonesia (cheap installs for retention), then Canada/Australia (monetisation proxy) |
| **Soft launch phase 2** | +6–10 weeks | Prove monetisation & tune difficulty: ARPDAU ≥ $0.08 in tier-1 test markets | L101–160 (Ch8–11: perishable, frozen/hot, sticky, magnet, radioactive, tilt), **Daily Sort v1** (no global leaderboard, friend + percentile only), fail-clip sharing, ad frequency A/B tests, Museum Oddities |
| **Global launch** | +3–4 months after SL1 | Scale UA profitably: D7 ≥ 15%, CPI ≤ $0.60, D7 ROAS on track for payback ≤ 120 days | L1–200, 25 themes, all 12 launch languages, global leaderboards for Daily, Weekend Mess events, achievements, cloud save |
| **Month 1** (post-launch) | | Retain launch cohort; first event | First seasonal event (calendar-dependent), Theme Drops ×1, Ch14–15 (+30 levels), trick-shot clip sharing, Museum flex cards |
| **Month 2** | | Introduce depth monetisation | **Sort Pass** season 1, Mechanic Weeks, friend challenges (async ghost on campaign levels), +30 levels |
| **Month 3** | | Broaden content | Ocean world with buoyancy/flood mechanic, Daily Chaos tier (L150+), explosive chapter, +30 levels |
| **Month 6** | | Long-term engine | **Infinite Facility** (curated generated levels after L400; campaign reaches ~400), community "Sort *what*?" theme voting, wind mechanic, web-playable friend challenge links, premium currency only if data supports it |
| **Month 12** | | Platform for years | 600+ campaign levels, 80+ themes, 4–6 seasonal events/year on a reusable event kit, licensed crossover packs, optional light social (teams for weekly Weekend Mess) only if Daily social data justifies it, level editor "Sort Studio" for players (UGC) as a test |

## 11.4 Competitive positioning

| Category | Examples | What they do well | What they lack | Our difference |
|---|---|---|---|---|
| **Traditional sort puzzles** | Ball Sort, Water Sort, Nuts & Bolts sort | Instant readability, logic purity, low CPI | Static, abstract, no surprise; content is "more tubes" | Same readability, but every level is a new *thing* and a new *twist*; physics and humour |
| **Organizing / ASMR games** | Unpacking, Fill the Fridge-style, "organize it" hypercasuals | Tactile satisfaction, cozy fantasy | No challenge curve; short content life | We keep the ASMR fill satisfaction but add stakes and escalation |
| **Match-3** | Royal Match, Candy Crush | Deep meta, LiveOps, monetisation | Learning curve for the board, boosters-as-monetisation pressure | No swaps/grids: direct manipulation; physics instead of cascades; much shorter levels |
| **Triple-match 3D (goods sort)** | Match Factory, Triple Match 3D | Pile-clearing satisfaction, proven hybrid monetisation | Rule never changes; objects are interchangeable props | Objects *matter* (they have behaviours); rules change every chapter |
| **Physics puzzles** | Cut the Rope, Angry Birds, Suika | Physics delight, skill | One mechanic family; slow level authoring | A *sorting* puzzle where physics is one of many systems, with a generator pipeline |
| **Hyper-casual** | Countless one-mechanic titles | Instant hook, cheap CPI | Retention collapses after day 2 | The one-mechanic hook, but a content and mechanic engine underneath |
| **Hybrid-casual puzzle** | Many "simple core + light meta" titles | Balanced retention & monetisation | Often a generic core with a meta glued on | The meta (Museum) *is* the core verb; the hook is curiosity, not the economy |

### Strongest unique selling proposition

> **"The rules change."** — A sorting game where the next level is never what you expect: same simple drag, endlessly new things to sort, and a new way for sorting to go wrong every chapter.

It combines three things no single competitor owns together: **sort-puzzle readability**, **physics comedy**, and **WarioWare-style surprise cadence**.

## 11.5 Core design risks

| # | Risk | Likelihood | Impact | Mitigation | Early warning metric |
|---|---|---|---|---|---|
| 1 | **Sorting becomes repetitive** | High | High | Two-axis structure (themes change every level, mechanics every chapter); twist slot mandatory from L5; novelty validator (V12); comedy slot in each chapter | Levels/session declines > 20% between D1 and D7 cohorts |
| 2 | **Too much randomness** | Medium | High | Deterministic physics, seeded levels, identical retries, environment accidents never cost Oops, all events telegraphed | "Unfair" rating in surveys; retry rate after fail < 70% |
| 3 | **Physics becomes frustrating** | High | High | Drop assist, counted-on-entry, settle tuning, physics budget caps, instant replay on fail, P3 prototype gate | Fail-cause mix: physics fails > 50% of fails on a level → review |
| 4 | **Levels become visually confusing** | Medium | High | 56 dp min size, ≤ 5 containers, silhouette/colour validators, patterns, compatibility matrix exclusions | Misplacement > 10% on any object |
| 5 | **Procedural generation creates bad levels** | High (if unchecked) | Medium | Gauntlet V1–V12, designer curation for the campaign, level health dashboard, remote replacement | Generated levels' churn > hand-made levels' churn by > 30% |
| 6 | **Too many mechanics** | Medium | High | One unfamiliar element per level; mastered = 10 levels; ≤ 3 systems below Chaos; cut any mechanic that tests poorly | First-try win rate on "Meet it" levels < 85% |
| 7 | **Monetization damages retention** | Medium | High | No ads before L12/8 min, never after fail, no banners, segmentation, A/B ad frequency with retention as guardrail | D1 retention delta between ad-frequency cells > 1.5 pts |
| 8 | **Becomes a generic puzzle game** | Medium | High | Protect the humour and surprise: writer on team; teaser card every level; absurdity tiers; mascot bins; marketing built on fail clips | Teaser-to-NEXT rate < 80%; share rate falling |
| 9 | **Content cost (object art) outpaces revenue** | Medium | Medium | Objects reused across many levels; themes ≤ 25 objects; AI-assisted concepting; style guide that is fast to produce (low-poly, toon) | Art cost per theme > 2.5 artist-weeks |
| 10 | **Hook doesn't sell in ads (high CPI)** | Medium | Very high | CPI test before MVP; creatives built around the most absurd sorts and fail clips | CPI > $1.20 across all creatives |
| 11 | **Early game too slow for hyper-casual audiences, too simple for puzzle fans** | Medium | Medium | L1–10 in 8 minutes with an early "wow" (L5 twist, L10 Big Mess); 3-star and Daily Sort give puzzle fans depth | Drop-off between L1 and L5 > 15% |
| 12 | **Low-end Android performance with physics + 3D** | Medium | Medium | Tiered quality, 30 fps lock on low tier, 40-object cap, profiling from P1 | fps p10 < 25 on low tier |

## 11.6 Experimentation plan

| # | Hypothesis | Test | Primary metric | Guardrail |
|---|---|---|---|---|
| E1 | Tuned drag feel (lift offset, weight lag) increases play length | P1 variants: offset 0 vs. 40 dp; weight lag on/off | Minutes played in a 10-min window | Mis-drop rate |
| E2 | The NEXT UP teaser card increases levels per session | Teaser vs. plain "Next" | Levels/session, D1 | — |
| E3 | Interstitial start at L12 vs. L20 | Soft launch A/B | D7 retention × ad ARPDAU (LTV D7) | D1 not lower by > 1 pt |
| E4 | Interstitial every 3 levels vs. every 2 | A/B | LTV D30 | D7 retention |
| E5 | Museum meta raises D7 | Museum on vs. off (level map only) | D7, sessions/day | — |
| E6 | Hot Hands streak increases rewarded views without hurting retention | On vs. off | Rewarded/DAU | D7 |
| E7 | Physics-rule chapter at L26 vs. L50 | Reorder chapters 3–4 after Ch5 | D1, D3 | L26–50 churn |
| E8 | Dynamic difficulty assist after 3 fails | On vs. off | Churn on 🔥 levels | Star rate (should not inflate) |
| E9 | Daily Sort raises D30 | Available at L20 vs. L60 | D14/D30 | D7 |

## 11.7 The Magic

What turns this from "a game where you sort objects" into a game people can't put down.

### 1. The core satisfying action
**The gulp.** You drag a chunky toy object, it lags with its weight, you drop it into a bin, and the bin *swallows* it — squash, note, sparkle, click. Then the lid snaps shut on a full bin. A clean run plays the level's melody back to you. The game rewards your hand before it rewards your brain.

### 2. The emotional payoff
**Mess → order, against the odds.** A screen of chaos becomes a row of closed, smiling bins — while the ship was rocking and the dragon was waking up. The payoff is the *contrast*: the more chaos the level threw at you, the better "SORTED!" feels.

### 3. The moment that makes players laugh
**The spit-back and the reveal.** You confidently drop a banana into the fruit bin; the bin squints, yells "IMPOSTOR!", and spits back a banana with a tiny moustache. Or a sleeping cat stretches out of a laundry pile and walks off with a sock. The game is funny *at the moment of your mistake*, so failure becomes entertainment.

### 4. The moment that makes players say "WTF?"
**The teaser card.** "NEXT UP: A Dragon's Tax Return." "NEXT UP: The Afterlife Lost Luggage." "NEXT UP: Opposite Day." And then Level 400 asks you to sort your own phone's notifications. The player never knows what they're going to be asked to sort next — that's the brand.

### 5. The moment that makes players restart immediately
**"1 object away!"** + the instant replay. You see, in slow motion, exactly how your egg got crushed by the anvil you dropped too early. You *know* what you'd do differently, the retry button is under your thumb, and the level is back in under a second. Fair, fast, and personal.

### 6. The mechanic that can create viral gameplay
**Physics fails with characters.** A radioactive banana contaminating an entire lunch tray in a spreading green wave. A fuse running out and the pile raining back down — with one object landing perfectly in the right bin. Six-second, auto-captured, vertical clips with googly-eyed bins reacting. Plus the meme template: "Sort Everything — now sort *___*."

### 7. The mechanic that gives the game long-term depth
**Composable behaviours under one predicate engine.** Thirty-two object behaviours, eighteen container modifiers, ten environment events, seventeen rule types — each simple alone — combining into problems that require *real* planning: lift the radioactive object over the pile, not through it; put the hot pan next to the frozen steak; feed the goat the moldy bread so it stays away from the cake. Mastery is learning how systems interact, not memorising levels.

### 8. Why a player is still playing after 500 levels
- **They still don't know what's next.** Themes rotate through 80+ worlds getting more absurd; every month adds a new theme and a new mechanic.
- **The daily ritual.** One shared puzzle, one share card, one streak — a 60-second habit.
- **Mastery is visible.** Stars, par times, Daily percentile, trick-shot clips, Oddities in the Museum.
- **The game respects them.** No lives, no waiting, no ads between fail and retry, no unfair physics. The player always believes the next level is fair — and that they'll laugh at it.

---

> **The game starts as "Sort Everything."**
> **By Level 400, the player's only thought is: "I have no idea what they're going to make me sort next — but I'm sorting it."**
