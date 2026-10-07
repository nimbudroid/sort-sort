# 01 — Core Gameplay & Interaction

[← Index](README.md)

---

## 1.1 The gameplay screen

Portrait, front-view "diorama". Gravity points down. The player's thumb lives in the bottom half, so the **containers are at the bottom** (drop targets in the thumb zone) and the **pile is in the middle** (short drags, always downward with gravity).

```
┌───────────────────────────────┐
│ ⏸   LEVEL 37        ⏱ 0:42   │  ← HUD strip (8% height). Pause, level number, timer only if level has one
│        ★ ★ ☆   ❌❌○           │  ← Stars-in-progress + Oops pips (only shown when relevant)
├───────────────────────────────┤
│  "A Wizard's Laundry"          │  ← Level title (fades after 1.5 s)
│                                │
│        ___ MESS ZONE ___       │
│     🧦  🎩   🧦 🪄  👘          │  ← The pile: objects resting on a table/shelf/floor
│   🎩 🧦  🪄   👘  🧦  🎩       │     with real physics (stacked, overlapping)
│  ▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔  │  (45% of height)
│                                │
│  ┌─────┐  ┌─────┐  ┌─────┐     │
│  │ 🧦  │  │ 🎩  │  │ 🪄  │     │  ← Rule chips on each container (icon, not text)
│  │ 0/5 │  │ 0/4 │  │ 0/3 │     │  ← Capacity counter (only when capacity matters)
│  │ ◉ ◉ │  │ ◉ ◉ │  │ ◉ ◉ │     │  ← Containers with faces (30% of height)
│  └─────┘  └─────┘  └─────┘     │
│                                │
│     [🔍 2]   [⏪ 1]   [❄ 0]     │  ← Booster tray (collapsed by default; hidden in Ch1)
└───────────────────────────────┘
```

**Hard UI rules:**

- No text is required to play. Text (title, rule banner) is flavour and reinforcement only.
- Maximum 3 HUD elements visible at once in the top strip.
- Containers never sit closer than 16 dp to the screen edge (gesture-navigation conflicts on Android/iOS).
- Minimum object size on screen: **56 dp** on the shortest side (≈ 9 mm). If a level needs more objects than fit at 56 dp, it scrolls the pile *vertically* in a shaft (Big Mess levels), never shrinks objects.
- Maximum containers: **5** in a row (≥ 64 dp each). A 6th+ container uses a second row or a moving conveyor, never a smaller bin.

## 1.2 Level start sequence (what the player sees)

| Time | Event | Purpose |
|---|---|---|
| 0.00 s | Screen wipe from previous level (the teaser card slides up and becomes the title). | Continuity — the curiosity paid off |
| 0.10 s | Containers pop up from below, one by one (60 ms stagger), squash on landing, blink their eyes. | Introduces the targets first |
| 0.35 s | Rule chips "stamp" onto each container (icon + colour). | The rule is the containers |
| 0.50 s | Objects **pour** into the Mess Zone from the top (like tipping out a bag), tumble, settle. | Physics is visible from second one; the "mess" fantasy |
| 0.50 s | **Input is live.** The player may grab objects while they are still falling. | Experts never wait |
| 1.50 s | Title text fades. If the level has a timer, it starts on the **first grab** or at 3.0 s, whichever comes first. | No unfair timer drain while reading |

**Rule banner (only when needed):** For levels whose rule cannot be expressed by container icons alone (e.g., "Don't let anything touch the banana"), a one-line banner with an icon appears under the HUD for 2.5 s, then collapses to a small icon the player can tap to re-read. Maximum 6 words. If a rule needs more than 6 words, the rule is redesigned.

## 1.3 What's in a level

### Objects
- **Count by tier:** Easy 6–12, Medium 10–18, Hard 14–24, Extreme 16–28, Chaos 18–32, Big Mess 30–60 (scrolling shaft).
- **Every object has:** a silhouette, a primary colour, a material (plastic/glass/metal/organic/fabric/paper/goo/stone), a size class (S/M/L), a mass class (1–5), and a list of category tags (e.g. `food`, `fruit`, `red`, `round`, `edible`, `belongs:grandma`).
- Objects are always **whole, toy-like, and named** (each has a one-line joke description for the Museum).

### Containers
Containers are what the player is sorting *into*. They define the rule visually. Base types:

| Container | Visual | Typical use |
|---|---|---|
| **Bin** | Open-top box with a face | Default, all chapters |
| **Jar** | Narrow mouth | Small objects only (size rule built into geometry) |
| **Shelf slot** | Outline-shaped slot | Exact-match / shape sorting |
| **Scale pan** | Two-pan balance | Weight puzzles (Ch3) |
| **Crate on wheels** | Bin that moves | Moving targets (Ch7) |
| **Mouth / creature** | A monster that eats | Living containers that react, burp, reject |
| **Conveyor box** | Arrives, accepts, leaves | Time windows / sequence |
| **Hazard bin** | Yellow-black striped, lead-lined | Radioactive / explosive disposal |

### Sorting rules
The rule is expressed by **rule chips** on containers. A chip is an icon (colour swatch, silhouette, category glyph, number, weight icon, "?" for inferred rules). Rule types and their full list are in [Chapter 03](03-special-objects.md#34-sorting-rule-types).

## 1.4 Interaction model

### Options compared

| Input | Speed | Tactility | Precision | Physics expression | Skill ceiling | Verdict |
|---|---|---|---|---|---|---|
| **Tap object → tap container** | ★★★★★ | ★☆☆☆☆ | ★★★★★ | None | Low | Reject as primary — sorting becomes bookkeeping |
| **Drag & drop** | ★★★☆☆ | ★★★★★ | ★★★★☆ | Medium (drop height, placement) | Medium | **PRIMARY** |
| **Swipe object toward container** | ★★★★☆ | ★★☆☆☆ | ★★☆☆☆ | Low | Low | Reject — mis-sorts feel unfair |
| **Flick / throw** | ★★★★★ | ★★★★☆ | ★★☆☆☆ | High (arc, impact, bounce) | High | **SECONDARY, emerges from drag** |
| **Hold** | — | ★★★☆☆ | — | None | — | **UTILITY: Inspect** (Ch5+) |
| **Tap (on object)** | — | ★★★☆☆ | — | Low | — | **UTILITY: activate** (crack ice, open box, wake) |
| Multi-touch | ★★★★★ | ★★★★☆ | ★★★☆☆ | Medium | High | Allowed, never required |
| Rotate (two-finger) | ★☆☆☆☆ | ★★★☆☆ | ★★★☆☆ | Medium | Medium | Cut — awkward one-handed, conflicts with drag |
| Stack (as player verb) | — | — | — | — | — | Not a verb; stacking *happens* via physics in containers |

### Recommendation: **one gesture with four readings**

The player only ever learns "touch the object". What they do next determines the action:

| Reading | Gesture | Unlock | Notes |
|---|---|---|---|
| **Drag** | Touch, move, release over container | L1 | 95% of all actions. |
| **Flick (throw)** | Release while moving > 1,400 dp/s | Works from L1, *taught* at L41 | Becomes a ballistic throw. Expert players discover it early by themselves — that's a reward, not a bug. |
| **Inspect** | Touch and hold still for 350 ms | L56 | Object lifts, zooms 1.6×, X-ray reveals hidden properties. Hold before L56 does nothing special. |
| **Poke** | Tap without moving (< 150 ms) | L8 (on gift boxes) | Only does something on objects with a tap-behaviour; otherwise the object does a little hop (juice, never punishes). |

### Drag feel specification

These numbers are the single most important tuning in the game. Prototype them first (see Ch. 11 experiment E1).

| Parameter | Value | Why |
|---|---|---|
| Pickup hit area | Visual bounds + 12 dp | Fat-finger forgiveness |
| Pick priority when overlapping | Topmost visible object under finger, ties → closest centre | Players grab what they see |
| **Lift offset** | Object floats **40 dp above** the touch point | The finger never hides the object or the target |
| Follow spring | Critically damped, 25 ms for mass 1, up to 110 ms for mass 5 | **Weight is felt in the drag**: heavy objects lag and sag; light ones snap |
| Sag | Mass 4–5 objects hang 6–12 dp below the lift point and swing | Heavy without text |
| Pickup feedback | Scale 1.0 → 1.15 over 80 ms, shadow appears under it, soft "pop" SFX, light haptic | Instant tactile confirmation |
| Drop shadow / landing guide | A soft dot projects straight down onto whatever is beneath | Players aim at the container mouth, not the sky |
| Container hover | Target container opens its mouth, eyes look at the object | Confirms target, **never** reveals right/wrong (except tutorial L1–3) |
| Drop assist ("magnet") | Released within 24 dp of a mouth sensor → object is guided into the mouth | Kills "I clearly dropped it in!" frustration |
| Release elsewhere | Object drops with physics back onto the pile | No penalty; never lost (invisible walls at screen edges) |
| Max simultaneous held objects | 2 (multi-touch) | Enough for experts, not chaos |

### Flick feel specification
- Throw velocity = release velocity × 0.85, clamped to a max so objects never leave the screen.
- **Aim assist:** if the throw vector is within 6° of a container mouth, bend the trajectory toward it. Off at "Extreme" difficulty for leaderboard daily modes (fair for all).
- Thrown objects carry momentum: fragile objects can break on impact, bouncy objects bounce out, heavy objects knock things over. **This is where the controlled chaos lives.**

## 1.5 Correct placement — what happens

Sequence lasts ~350 ms and must never block the next input.

1. Object crosses the container's mouth sensor → **counted immediately** (no waiting for it to settle).
2. Container does a **gulp**: squash to (1.12, 0.88), spring back over 180 ms. Eyes close happily.
3. **Note SFX:** each correct sort plays the next note of the level's melody (see [Ch. 08 audio](08-art-audio-ux.md#82-audio)). A clean run plays the whole jingle.
4. Counter chip ticks (`3/5 → 4/5`), with a small number pop.
5. 6–10 particles in the container's colour puff out of the mouth.
6. Light haptic (iOS `UIImpactFeedbackGenerator.light` / Android `EFFECT_TICK`).
7. **Combo:** if this sort happened within 1.2 s of the previous correct sort, the combo counter increments (×2, ×3…); the note pitch climbs; at ×5 the screen edge gets a gentle warm glow. Combo multiplies the coin reward (capped ×5).
8. The object **physically stays** in the container and stacks with the others — the bin visibly fills. When a container reaches capacity, its lid snaps shut with a satisfying *clack* and a cheerful wiggle.

## 1.6 Incorrect placement — what happens

1. The object enters the mouth → container's face goes **disgusted** (squint, tongue out).
2. 150 ms later: **"PTOO!"** — the container spits the object back onto the pile on a visible arc (0.45 s). The object lands with physics (so a spit can knock other things around — mild chaos, always recoverable).
3. Red outline flash on the container, a comedic "bwomp" SFX, medium haptic.
4. Combo resets.
5. **Oops pip:** from L16 onward, levels have **3 Oops**. A wrong placement costs one. Losing all three fails the level. Before L16 mistakes are free (the game teaches by rejection, not punishment).
6. The 3-star "No Mistakes" condition is lost for this attempt (shown by the star dimming — the player knows instantly).

**Never punish an ambiguous object.** If playtests show >10% of players mis-sort a specific object on first sight, that object's art is fixed — the level is not "hard", it is broken.

## 1.7 Level complete — what happens

| Time | Beat |
|---|---|
| 0.00 s | Last correct sort → 0.25 s slow-motion on the gulp |
| 0.25 s | All container lids close left-to-right in a ripple; each plays an ascending note (the "cadence") |
| 0.80 s | Rubber stamp **"SORTED!"** slams in the centre (screen shake 4 px, 120 ms) |
| 1.10 s | Stars fill one at a time (★ Complete, ★ No Mistakes, ★ Under par time) |
| 1.60 s | Coins fly into the counter; combo bonus shown as a multiplier |
| 2.00 s | **"NEXT UP" teaser card** slides up from the bottom: silhouette of the next level's objects + its title ("NEXT: A Vampire's Fridge") |
| 2.00 s | **NEXT** button active (big, bottom-centre, thumb zone). Secondary: "×2 coins" rewarded-ad button (smaller, never where NEXT is) |

The teaser card is mandatory on every level. It is the hook. (See [Ch. 07](07-meta-economy-monetization.md#71-the-one-more-level-loop).)

## 1.8 Level length targets

| Level type | Target median (first clear) | Expert time (par for 3rd star) |
|---|---|---|
| Ch1 tutorial | 12–25 s | 8–15 s |
| Easy | 20–30 s | 12–18 s |
| Medium | 30–45 s | 20–30 s |
| Hard | 40–60 s | 28–40 s |
| Extreme / Chaos | 50–80 s | 35–55 s |
| Big Mess (chapter finale) | 90–150 s | 60–100 s |
| Daily Sort | 45–75 s | — (leaderboard) |

If the median exceeds the band by more than 30% in analytics, the level is flagged for rebalancing.

## 1.9 Between levels

The space between levels should cost **at most 5 seconds** and always end on the NEXT button.

- **Every level:** reward screen + teaser card (≤ 4 s).
- **Every 5 levels:** "Shelf moment" — one new object is added to the Museum with its joke description (2 s, skippable, one tap).
- **Chapter end (every 15 levels):** chapter-clear celebration, Museum shelf completes, unlocks a container skin, introduces next chapter's name ("CHAPTER 4: THROW IT").
- **Interstitial ad** (after L12 only; frequency rules in [Ch. 07](07-meta-economy-monetization.md#77-monetization)) appears **after** the reward screen and **before** the next level loads — never between fail and retry.
