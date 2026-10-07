# 04 — Progression, Difficulty, Tutorial & Failure

[← Index](README.md)

---

## 4.1 Structural principle: two axes

| Axis | Changes | Purpose |
|---|---|---|
| **Theme** (what you sort) | Almost every level | Novelty, humour, "what's next?" curiosity, Museum collection |
| **Mechanic** (how sorting goes wrong) | Every chapter (15 levels) | Learning, mastery, depth |

A chapter is named after its *mechanic*, not its theme ("Chapter 3: Heavy Stuff"). Inside it, the player might sort a gym, a blacksmith's forge, a wedding cake shop and an elephant's handbag. This is the key change from a classic "Candy World → Kitchen World" structure: themes never get stale because no theme overstays, and mechanics get enough repetitions to be learned.

## 4.2 The chapter rhythm (15 levels)

Every chapter after Chapter 1 follows the same internal rhythm. This is a production template: designers fill slots, the generator fills candidates.

| Slot | Role | Difficulty label | What happens |
|---|---|---|---|
| 1 | **Meet it** | Normal | New mechanic in safe context |
| 2 | **Use it** | Normal | Mechanic is the whole level |
| 3 | Breather | Normal | Old mechanics, new funny theme |
| 4 | **Respect it** | Normal | New mechanic + 1 familiar mechanic, mistakes cost |
| 5 | **Hard** | 🔥 Hard | First hard level of chapter |
| 6 | Second mechanic: Meet it | Normal | Chapters introduce 2–4 mechanics; second one starts |
| 7 | Use it | Normal | |
| 8 | **Twist showcase** | Normal | A level built entirely around a great mid-level twist |
| 9 | Combine | Normal | Both new mechanics together |
| 10 | **Hard** | 🔥 Hard | |
| 11 | Third mechanic or variant | Normal | |
| 12 | Breather / comedy | Normal | Short, very funny, very easy — a reward |
| 13 | Remix with an old chapter's mechanic | Normal / Hard | Recall + combination |
| 14 | **Super Hard** | 💀 Super Hard | Skill check before the finale |
| 15 | **BIG MESS** (finale) | 🧹 Big Mess | A huge, scrolling cleanup using everything from the chapter. Long, spectacular, generous Oops (5) |

**Resulting curve inside a chapter (sawtooth):**

```
difficulty
  ▲                                          💀
  │                     🔥             🔥      ╲   🧹
  │                    ╱  ╲           ╱  ╲      ╲ ╱
  │          ╱╲      ╱     ╲   ╱╲   ╱     ╲╱╲   ╳
  │   ╱╲   ╱   ╲   ╱        ╲╱   ╲╱           ╲╱
  │ ╱    ╲╱     ╲╱
  └─────────────────────────────────────────────────▶ level
    1  2  3  4  5  6  7  8  9  10 11 12 13 14 15
```

**Across chapters**, the baseline rises slowly; the peaks rise faster. The first level of each chapter is always easier than the last level of the previous one (relief + learning).

## 4.3 Campaign progression — what the player learns

| Chapter | Levels | Name | New mechanics (level #) | What the player learns | The "aha!" |
|---|---|---|---|---|---|
| 1 | 1–10 | **First Day** | Drag (1), shape (3), category (4), mid-level twist (5), capacity (6), combo (7), digging (8), gift box/poke (8), odd-one-out (9) | The containers *are* the rules; levels change mid-way; speed is rewarded | "The bins tell me everything." |
| 2 | 11–25 | **Rules Are Rules** | Size (11), heavy as juice (12), two-attribute (13), shape slot (14), pairs (15), **Oops pips (16)**, owner (17), jar (18), **first timer (20)**, count (21), **inferred rules (23)** | Read multiple attributes; deduce rules from feedback | "I can figure out what the bin wants by watching what it spits out." |
| 3 | 26–40 | **Heavy Stuff** | Weight limit (26), scale (30), fragile (33), breaks-as-fail (36), creature container (38) | How you place matters, not just where | "Heavy first, eggs on top!" |
| 4 | 41–55 | **Throw It** | Flick taught (41), bouncy (44), lid timer (46), rolling (50), slippery (53) | Skill expression; speed vs. control | "I can throw things!" |
| 5 | 56–70 | **Not What It Seems** | Disguised + Inspect (56), invisible (60), nested (64), mimic (65), sequence (68) | Don't trust appearances; Hold to inspect | "That banana has a moustache." |
| 6 | 71–85 | **It's Alive** | Living (71), sleeping (75), shy (79), hungry (83) | Prioritise; the mess moves | "Sort the hamster last, when its bin is almost full." |
| 7 | 86–100 | **Moving Day** | Moving container (86), swapping (90), key/locked (93), conveyor (94), VIP (97), customer rush (99), **L100 milestone** | Timing; spatial memory | "Wait for the bins to swap back." |
| 8 | 101–115 | **Best Before** | Perishable (101), frozen (105), hot + fireproof (109), flammable (112) | Triage; objects as tools | "Put the hot pan next to the ice — free thaw!" |
| 9 | 116–130 | **Sticky Situation** | Sticky (116), magnet (121), floaty (126) | Tool use; clumps | "The magnet does the work for me." |
| 10 | 131–145 | **Hazmat** | Radioactive (131), mold (136), bad apple (141) | Spatial avoidance; the board changes under you | "Lift it *over* things, don't drag it *through* them." |
| 11 | 146–160 | **Rough Seas** | Tilt (146), tippy containers (148), quake (150), lights out (155), leaky (157) | Ride the wave; timing on environment cycles | "Place on the still point of the wave." |
| 12 | 161–175 | **Kaboom** | Fuse/explosive (161), chain reactions (166), shrinking bins (168), splitter (171) | Emergency prioritisation; use explosions to clear piles | "Blow up the junk pile to un-bury the key." |
| 13 | 176–190 | **Rule Change** | Mid-level rule change (176), opposite day + mirror labels (181), sort by shadow (186) | Re-read the board; flexibility | "Everything I knew is wrong — on purpose." |
| 14 | 191–205 | **Ghost Shift** | Ghost (191), chameleon (196), clone (201) | Timing on object cycles | "Drop it while it's solid." |
| 15–27 | 206–400 | **Remix chapters** | Each chapter: 2 prior mechanics fused + 1 variant (teleport pairs 210, gravity flip 224, quantum 260) | Mastery through combination | "Frozen explosives? Oh no." |
| ∞ | 401+ | **The Infinite Facility** | Curated generated levels (see Ch. 05) + monthly hand-made "headline" chapter | — | — |

**Why physics arrives at L26 (not L50):** a D0 player clears ~20–30 levels. The game's differentiator — physics chaos — must be *felt* in the first session or the game reads as "another Ball Sort" and D1 suffers. Physics is present as juice from L1 (pour-in, stacking, the heavy thud from L12) and becomes a rule at L26, so the first session ends on "this game is more than I thought."

## 4.4 Difficulty budget

Difficulty is **not** object count. Every level component carries points on three axes:

- **CL — Cognitive Load:** how much the player must think/remember.
- **EL — Execution Load:** how much dexterity/precision is needed.
- **PL — Pressure Load:** how much time/resource pressure exists.

### Component costs (excerpt — full table lives in the generator config)

| Component | CL | EL | PL |
|---|---|---|---|
| Each container beyond 2 | 1 | 0 | 0 |
| Every 6 objects beyond 6 | 0.5 | 0.5 | 0 |
| Colour / shape rule | 0 | 0 | 0 |
| Category rule | 1 | 0 | 0 |
| Two-attribute rule | 2 | 0 | 0 |
| Inferred rule | 3 | 0 | 0 |
| Sequence rule | 2 | 1 | 0 |
| Opposite day | 3 | 0 | 0 |
| Heavy + weight limit | 1 | 1 | 1 |
| Fragile | 0.5 | 2 | 0 |
| Bouncy | 0 | 2 | 0 |
| Disguised (per 3 objects) | 2 | 0 | 0 |
| Living | 1 | 1 | 1 |
| Perishable | 1 | 0 | 2 |
| Radioactive | 2 | 2 | 0 |
| Explosive (per fuse) | 1 | 1 | 2 |
| Moving / swapping containers | 1 | 2 | 1 |
| Tilt | 0 | 3 | 1 |
| Lights out | 2 | 1 | 1 |
| Timer, generous (≥ 2.0× bot time) | 0 | 0 | 1 |
| Timer, tight (1.4–2.0× bot time) | 0 | 0 | 3 |
| Mechanic introduced < 5 levels ago | +1 | +1 | 0 |

### Tier caps

| Tier (label) | Total budget | Max active *systems*\* | Max pressure sources | Unfamiliar elements | Target first-try win rate | Target win within 3 tries |
|---|---|---|---|---|---|---|
| **Easy** (Normal) | ≤ 4 | 1 | 0 | ≤ 1 | 90–98% | 99% |
| **Medium** (Normal) | ≤ 8 | 2 | 1 (generous) | ≤ 1 | 75–90% | 97% |
| **Hard** (🔥) | ≤ 12 | 2 | 1 | 0 | 50–65% | 90% |
| **Extreme** (💀 Super Hard) | ≤ 16 | 3 | 1 | 0 | 30–45% | 80% |
| **Chaos** (🌀 Chaos — chapters 15+) | ≤ 22 | 3–4 | 2 | 0 | 20–35% | 70% (within 5: 90%) |
| **Big Mess** (🧹) | ≤ 14 | chapter's mechanics | 1 | 0 | 60–75% | 95% |

\* A *system* = a rule type, an object behaviour group, a container modifier group, or an environment event.

### What each tier feels like

| Tier | Recipe | Example |
|---|---|---|
| **Easy** | One rule, no pressure. | Sort socks by colour into 3 baskets. |
| **Medium** | Two categories or one rule + one familiar mechanic. | Sort the gym: heavy weights into a 20 kg rack, towels into a hamper. |
| **Hard** | Two systems interacting + one pressure. | Eggs (fragile) and cast-iron pans (heavy) into a moving crate, 45 s. |
| **Extreme** | Three interacting systems. | Radioactive lunch tray, a hungry goat, and a weight limit on the hazard bin. |
| **Chaos** | Multiple systems + environment + time — but every system is mastered. | Pirate ship rocking, a lit powder keg, mimic treasure chests, 60 s. |

## 4.5 How complexity increases without frustration

1. **One unfamiliar element per level** (Pillar 3). The generator enforces this.
2. **Mastered = 10 levels.** A mechanic can appear in Extreme/Chaos only if it was introduced ≥ 10 levels ago and appeared in ≥ 4 levels since.
3. **Hard levels never introduce.** A 🔥 level only uses known mechanics.
4. **Every Hard level is followed by a Normal level** (relief; protects the "one more" loop at the moment players are most likely to churn).
5. **Combinations reuse readable tells.** Every behaviour's tell (sparkle, glow, fuse) is preserved when combined; we never hide one tell behind another.
6. **Difficulty labels set expectations.** A player who fails a level labelled 🔥 Hard blames the level ("it's hard!") and retries; a player who fails an unlabelled level blames themselves or the game.
7. **Dynamic difficulty assistance (quiet).** After 3 failures on the same level: next attempt gets +15% time and the drop-assist radius grows by 25%. After 5: one free "Magnifier" or "Undo". Never announced, never applied to Daily Sort (leaderboard integrity). Remote-configurable and A/B tested.
8. **Skip ticket.** After 6 failures, a "Skip" button appears (costs a rewarded ad or 1 Skip ticket; tickets come from chapter rewards). Skipped levels can be returned to for stars.

## 4.6 How the game prevents repetition

| Mechanism | How |
|---|---|
| **Theme rotation** | No theme repeats within 5 levels; no theme family (e.g. "food") repeats within 3 |
| **Twist pool** | Each level has a twist slot; the same twist type can't repeat within 4 levels |
| **Layout variety** | 12 Mess Zone layouts (table, shelf, floor, conveyor, shaft, ship deck, seesaw, conveyor-under-pile…) rotate |
| **Container count rhythm** | Alternates 2–3–4–3–5–2… so levels feel different in shape |
| **Novelty score** | The generator scores each candidate's distance from the last 10 levels on a tag vector; < threshold → rejected |
| **Comedy levels** | Slot 12 of each chapter is a pure joke level ("Sort the sorting machine's parts") |
| **Escalating absurdity** | The theme catalogue is ordered by absurdity tier (see Ch. 09); later chapters draw from weirder tiers |

## 4.7 Tutorial: the first 10 levels (teaching without tutorials)

No text boxes, no "tap here" overlays beyond a single animated hand in L1. Every lesson is taught by **the first natural mistake** or by **level geometry**.

| Level | Title | Setup | What's taught | How it's taught (no text) | Twist |
|---|---|---|---|---|---|
| **1** | "Two Colours" | 6 balls (3 red, 3 blue), 2 bins with red/blue swatches | Drag & drop; bins = rules | A ghost hand drags one ball for 1.5 s, then vanishes the moment the player touches anything. Correct bin glows (only in L1–3) | None. Player wins in ~8 s. Huge celebration: confetti, "SORTED!", 3 stars guaranteed |
| **2** | "Three Colours" | 9 objects (balls, cubes) in red/blue/yellow, 3 bins | Colour, not shape, matters | Mixed shapes are present but bins only show colour | None |
| **3** | "Shapes" | All objects are the same green; bins show silhouettes ● ■ ★ | Read the bin's icon, not just colour | Colour no longer helps — the brain switches automatically. Dropping a cube in the ● bin → first **spit-back** (no penalty). The player learns rejection | None |
| **4** | "Fruit or Veg?" | 10 real-ish objects (apple, carrot, banana, broccoli…), 2 bins with fruit/veg icons | Semantic categories | Icons on bins are the category's most obvious member. Tomato is deliberately absent (fair) | None |
| **5** | "Sock Drawer" | 8 socks, 2 drawers (striped/dotted) | **The mid-level twist** | When 6/8 are sorted, a chute rumbles and pours in 4 **shoes**, and a third box pops up with a 👟 icon. Pure surprise, zero difficulty | Pour-in + container pop-up |
| **6** | "Egg-cellent Cartons" (not fragile yet) | 12 coloured eggs, 3 cartons with **slots (0/4)** | Capacity | Cartons visibly fill; a full carton closes its lid and refuses more (spit). Rubber eggs — they bounce harmlessly, a hint of future physics | Last 2 eggs are hidden under a towel (lift it — it's an object too) |
| **7** | "Candy Rush" | 20 small candies, 3 jars | **Combo** | Small objects + short drags = natural fast play; combo counter appears at ×2, pitch rises, coins multiply. The player discovers speed is rewarded | Combo ×5 triggers a "sugar rush" sparkle |
| **8** | "Birthday Pile" | 14 toys in a deep pile, 3 bins, **1 gift box** on top | Digging; Poke | Bottom objects aren't reachable until the top is sorted. The gift box sits on top, wiggles — players instinctively tap → it bursts open with 3 more toys | Gift box contents |
| **9** | "Spot the Odd One" | 9 kitchen utensils + 1 rubber chicken; 2 bins: 🍴 and **?** | The odd-one-out bin; the game is funny | The "?" bin is shaped like a question mark. Rubber chicken squeaks when touched | The rubber chicken, when sorted, honks and the "?" bin laughs |
| **10** | **BIG MESS: "A Kid's Bedroom"** | 32 objects (toys, clothes, books, a sandwich), 4 bins, scrolling pile | Everything at once, at scale; **the payoff** | Long, satisfying cleanup. A clean-room "before/after" wipe at the end. Unlocks the **Museum** and shows the first chapter reward | Halfway through, a sleeping cat is revealed under the clothes — it's just a cat (it walks into the bed). Foreshadows Living objects |

**First-session milestones (D0):**
- L1 complete: < 15 s after install opens.
- L10: ~8–10 min. Museum unlocked (meta hook).
- L16: Oops pips arrive — game has stakes.
- L20: first timer.
- L26–30: first physics rule (weight limit, scale). The "oh, it's *that* kind of game" moment.
- End of D0 target: L25–35.

## 4.8 Failure states

| Fail state | Trigger | Introduced | Failure animation (≤ 1.5 s, comedic) |
|---|---|---|---|
| **Out of Oops** | 3 wrong placements | L16 | All containers turn to stare at the player, then sigh in unison; one shrugs |
| **Time up** | Timer reaches 0 | L20 | An alarm clock rings, jumps, and every remaining object flops over "asleep" |
| **Overflow** | Object forced into full container (only on "overflow-fail" levels) | L22 | Container bulges, pops its lid, sprays contents like a party popper |
| **Weight limit** | Gauge exceeds limit | L26 | Bin's legs buckle, it collapses to the floor with a *crunch* and a dust cloud; its eyes become ✕ ✕ |
| **Broken** | Fragile object broken (on "no breaks" levels) | L36 | Slow-mo shatter, then a single shard spins and falls over |
| **Escaped** | Living object leaves the screen (on escape-fail levels) | L74 | The hamster waves goodbye wearing a tiny backpack |
| **Contamination** | Contaminated object count > limit / radioactive touches the "no contamination" bin | L133 | Everything glows green, a Geiger counter goes crazy, a tiny hazmat drone flies in and sprays the screen |
| **Explosion** | Fuse runs out outside the blast bin (on "no explosions" levels) | L163 | Big boom, everything flies up, then *rains back down* into a random pile; one object lands perfectly in the correct bin (ironic) |
| **Wrong sequence** | Item out of order (sequence/VIP/queue rules) | L68 | The customer/king throws a tantrum and the queue resets |
| **Eaten** | Hungry creature eats a required object | L83 | The goat burps up the receipt |

Failure should look like a **sitcom punchline**, not a punishment. All fail animations are skippable by tapping (go straight to retry).

## 4.9 The retry loop

### Fail screen (appears 1.5 s after fail, or instantly on tap)

```
┌───────────────────────────────┐
│                                │
│       SO CLOSE!                │  ← headline changes with progress
│   ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░  23/24  │  ← progress bar: how much was sorted
│     "1 object away!"           │
│                                │
│   ┌─────────────────────┐      │
│   │  ▶ INSTANT REPLAY    │      │  ← auto-plays last 3 s in slow-mo (loops)
│   │   (cause circled)    │      │
│   └─────────────────────┘      │
│                                │
│   [ 🎬 +15s & keep going ]     │  ← Rewarded continue (once per attempt)
│                                │
│        [  ⟳  RETRY  ]          │  ← BIG, thumb zone, default action
│                                │
│   share 📤        home ⌂       │  ← small
└───────────────────────────────┘
```

### Retry rules
- **RETRY** is the largest button and sits exactly where the player's thumb already is (the NEXT button position). Muscle memory = "tap the same spot again."
- **Retry is instant:** ≤ 1.0 s to playable. The level does *not* re-run its pour-in animation at full length on retry; objects snap into their start positions with a fast 0.3 s pour.
- The level is **deterministic**: the same layout every retry, so learning carries over.
- **Never** an interstitial between fail and retry.
- Retry doesn't cost anything (no lives).

### Headline by progress (near-miss framing)

| Progress at fail | Headline | Sub-line |
|---|---|---|
| ≥ 95% or 1 object left | "SO CLOSE!" | "1 object away!" |
| 80–94% | "Almost!" | "Only 3 left!" |
| 50–79% | "Halfway there!" | "Try throwing the light stuff." (contextual hint) |
| < 50% | "Messy!" | Contextual hint based on cause of failure |

### Contextual hints (shown on fail, < 50% progress or after 2nd fail)
Generated from the fail cause — e.g., cause = fragile broken by heavy → "Tip: heavy things go in first." Cause = weight limit → "Tip: check the gauge before you drop." Hints never reveal the solution, only the principle.

### Near-miss moments during play
- When 1 object remains and ≤ 5 s are left: music drops to just a heartbeat; time slows by 10% (cosmetic, not mechanical — the timer runs normally).
- When the player saves a falling fragile object mid-air: "NICE SAVE!" toast + bonus coins.
- When a weight gauge is within 5% of the limit at level end: "Close one! 9.8 / 10 kg" badge on the result screen — players love surviving by a hair.
