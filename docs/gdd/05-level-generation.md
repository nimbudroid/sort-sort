# 05 — Level Generation System

[← Index](README.md)

---

## 5.1 Philosophy

> **Generator proposes. Validator filters. Designer disposes.**

Procedural generation is a *candidate factory*, not a level designer. It produces many valid levels; humans pick the good ones for the main path. Automatic validation alone is trusted only for content where variety matters more than authorship (Daily Sort, Infinite Facility, event filler).

## 5.2 What is generated, what is hand-made, what is validated

| Content | Produced by | Validated by | Rationale |
|---|---|---|---|
| **Core mechanics / rule types / behaviours** | Engineers + designers (code) | Playtest | Systems are the game. Never AI-generated. |
| **Campaign L1–100** | Hand-designed in the level editor (generator used as a sketchpad) | Designer + playtest + solver | First impressions decide retention. |
| **Campaign L101–400** | Generator candidates from designer-written chapter briefs → designer picks & edits | Solver + designer review + soft-launch data | ~3 designer-hours per chapter instead of ~3 days |
| **Infinite Facility (L401+)** | Generator, pre-baked in packs of 50 | Solver + automated quality scoring + designer spot-check (10%) | Volume |
| **Daily Sort** | Generator, 60 days ahead | Solver + automated scoring + designer approval of the week's batch (15 min/week) | Global fairness for leaderboards |
| **Object concepts** | AI-assisted ideation (lists of objects per theme, joke descriptions) → designer curation | Art director | AI is fast at "50 things in a wizard's laundry"; humans pick the funny ones |
| **Object art (3D/2D)** | Artists, optionally AI-assisted concept sketches and texture drafts | Art director against style guide + readability test | Silhouette readability and style consistency are non-negotiable |
| **Object tags/metadata** | AI-drafted from name + image, designer-reviewed | Tag-consistency linter | Wrong tags = unfair levels |
| **Theme definitions** (palette, background, music stem) | Hand-designed | Art/audio lead | Brand quality |
| **Level titles / flavour text** | AI-drafted variants → writer edits | Writer, loc review | Jokes need a human ear |
| **SFX** | Library + procedural variation (pitch/filter per material) | Audio lead | Variation is cheap; identity is hand-made |
| **Localization** | Machine translation + human review for top 12 languages | LQA | Humour doesn't survive raw MT |
| **Seasonal events** | Hand-designed headline levels + generated filler from event object packs | Designer | Events are marketing moments |

**Rule of thumb for AI usage:** AI may *draft* anything the player reads or sees; a human must *approve* everything the player reads or sees; nothing AI-produced ships without passing the same validators as human content.

## 5.3 The level grammar

A level is a data record. Every field maps to a reusable system.

| Parameter | Type | Example values | Notes |
|---|---|---|---|
| `theme` | ThemeId | `wizard_laundry`, `pirate_ship`, `vampire_fridge` | Sets background, palette, music stem, object pool |
| `objectSet` | List of ObjectDef refs + counts | `{sock_striped:4, hat_pointy:3, wand:3, robe:2}` | Drawn from theme pool + optional "guest" objects |
| `containerSet` | List of ContainerDef + rule chips | `[bin(sock), bin(hat), jar(wand)]` | 2–5 containers |
| `sortRule` | RuleType + params | `category`, `twoAttribute(color=red, shape=round)`, `inferred(hidden=metal)` | Primary rule |
| `specialRules` | List of behaviour assignments | `fragile on [potion*]`, `disguised: 2 of sock → hat` | 0–3 entries |
| `containerMods` | List | `weightLimit(bin2, 12)`, `moving(bin1, speed=0.4)` | |
| `physicsProfile` | Enum + params | `standard`, `slippery_floor`, `tilt(amp=8°, period=6s)`, `lowGravity` | |
| `environmentEvent` | Event + trigger | `lightsOut(at=50%)`, `quake(every=12s)` | Max 1 below Chaos |
| `timeLimit` | seconds or null | `null`, `45` | Derived from solver time (see §5.6) |
| `oops` | int | 3 (default), 5 (Big Mess), 1 ("Perfectionist" twist) | |
| `failConditions` | List | `oops`, `time`, `weight`, `break`, `contamination>2`, `explode` | |
| `twist` | TwistDef + trigger | `pourIn(objects=[shoe×4], at=60%)`, `ruleChange(at=50%)`, `revealLiving(cat)` | One per level (from L5) |
| `layout` | LayoutId | `table`, `shelf3`, `shaft`, `deck`, `conveyor` | 12 layouts |
| `difficultyTier` | Enum | `easy/medium/hard/extreme/chaos/bigMess` | Budget cap |
| `seed` | int | `884213` | Deterministic pile placement and gift contents |
| `tags` | Derived | `[food, fragile, timer, twist:pourIn]` | For novelty scoring and analytics |

### Example (data file)

```json
{
  "id": "L0137",
  "theme": "power_plant_lunchroom",
  "layout": "table",
  "difficultyTier": "medium",
  "seed": 52217,
  "objectSet": { "sandwich": 3, "apple": 3, "juicebox": 3, "banana_glowing": 1 },
  "containers": [
    { "type": "bin",      "rule": { "category": "food_solid" } },
    { "type": "bin",      "rule": { "category": "drink" } },
    { "type": "hazard_bin","rule": { "state": "contaminated_or_radioactive" }, "mods": [{ "capacity": 4 }] }
  ],
  "specialRules": [ { "behaviour": "radioactive", "targets": ["banana_glowing"] } ],
  "physicsProfile": "standard",
  "timeLimit": null,
  "oops": 3,
  "failConditions": ["oops", "hazardOverflow"],
  "twist": { "type": "pourIn", "at": 0.6, "objects": { "apple": 2, "banana_glowing": 1 } },
  "titleKey": "lvl.power_plant_lunchroom.title"
}
```

## 5.4 Generator pipeline

```
 Chapter brief (designer)                       Theme catalogue    Object library
 "Ch10 Hazmat, slot 4, Medium,                        │                 │
  new: radioactive, familiar: capacity"               │                 │
            │                                          ▼                 ▼
            └──────────▶ [1] TEMPLATE PICK ──▶ [2] THEME + OBJECT FILL ──▶ [3] RULE BINDING
                         (slot role → level                (theme compatible       (choose rule that the
                          archetype)                         with mechanic tags)     object set can express)
                                                                    │
            ┌───────────────────────────────────────────────────────┘
            ▼
     [4] MODIFIER ASSIGN ──▶ [5] PILE LAYOUT (seeded physics drop) ──▶ [6] TWIST SELECT
     (budget-aware)                                                        │
            ┌─────────────────────────────────────────────────────────────┘
            ▼
     [7] VALIDATION GAUNTLET  ──fail──▶ discard / mutate (≤ 5 retries)
            │ pass
            ▼
     [8] SCORING (fun proxies) ──▶ [9] DESIGNER QUEUE (campaign) / AUTO-PUBLISH (daily, infinite)
```

**Step details:**

1. **Template pick.** 40 level *archetypes* (e.g. "pile + 3 bins + mid pour-in", "conveyor triage", "scale puzzle", "two-stage leaky sort", "rescue the VIP"). Each slot role in the chapter rhythm maps to allowed archetypes.
2. **Theme + object fill.** Theme must have ≥ N objects that carry the tags the mechanic needs (e.g. Fragile requires ≥ 4 objects tagged `breakable`). A theme's absurdity tier must be ≤ chapter's allowed tier.
3. **Rule binding.** Choose a sorting rule that partitions the object set cleanly: every object maps to exactly one container under the rule. Objects that map to 0 or 2+ are removed or the rule is rejected.
4. **Modifier assign.** Add special rules until the difficulty budget target (±1) is reached, respecting the compatibility matrix (Ch. 03 §3.6) and "one unfamiliar element".
5. **Pile layout.** Drop objects into the Mess Zone with a seeded physics simulation; enforce ≥ 40% graspable at start, no required object buried > 3 layers deep, no object initially overlapping a container mouth.
6. **Twist select.** From the twist pool, filter by archetype + recency (not used in last 4 levels).
7. **Validation gauntlet** (§5.5).
8. **Scoring** (§5.7).
9. **Publishing.**

## 5.5 Validation gauntlet (automatic)

Every generated level must pass all checks. Each check has a named failure reason logged for generator tuning.

| # | Check | Method | Rejects |
|---|---|---|---|
| V1 | **Solvable** | Planner bot with perfect information solves it (A* over abstract sort actions with physics simulated per action) | Impossible levels |
| V2 | **Robustly solvable** | "Human-like" bot: perception noise (misreads 3% of disguised objects without inspecting), drag imprecision (±10 dp), reaction delay 250 ms, 300 ms per action. Run 200 sims with jitter. Win rate must be within tier band (e.g. Hard: 45–75%) | Levels that are only solvable with frame-perfect play |
| V3 | **Unique mapping** | Every object maps to exactly one container under the active rule at every moment (including after contamination/spoil transitions) | Ambiguous levels |
| V4 | **Readability** | Silhouette distinctness score between objects in different containers ≥ threshold (IoU of silhouettes + colour distance ΔE ≥ 25); colour-blind simulation (deuteranopia/protanopia/tritanopia) still separates colour-rule groups (patterns required) | Confusing visuals |
| V5 | **Screen fit** | All objects ≥ 56 dp; ≤ 5 containers per row; HUD doesn't overlap pile | Cramped levels |
| V6 | **Budget** | CL/EL/PL totals within tier cap; ≤ 1 unfamiliar element; pressure sources ≤ cap | Overloaded levels |
| V7 | **Fair time** | If timed: time limit ≥ 1.4× human-like-bot median (Hard), ≥ 2.0× (Easy/Medium) | Unfair timers |
| V8 | **Telegraphed** | Every environment event has a ≥ 1.0 s telegraph; every disguised object has a tell flag | Gotchas |
| V9 | **No softlock** | Simulate random play for 60 s ×100: the level must never reach a state where completion is impossible without the fail state triggering (e.g. capacity filled with wrong-but-accepted items) | Hidden dead ends |
| V10 | **Physics stability** | Pile settles < 1.2 s; no object jitter > 2 dp at rest; nothing tunnels through walls in 200 sims | Broken physics |
| V11 | **Not trivial** | Human-like bot completion must require ≥ N meaningful decisions (tier-based) and the twist must change the optimal plan in ≥ 50% of sims | Boring levels |
| V12 | **Novelty** | Tag-vector cosine distance from each of the last 10 levels in the sequence ≥ 0.25 | Repetition |

## 5.6 Deriving time limits and par times

- **Par time (3rd star)** = 60th percentile of human-like bot completion times × 1.1.
- **Time limit** = human-like bot *median* × tier multiplier (Easy/Medium: 2.0–2.5×, Hard: 1.6×, Extreme: 1.45×, Chaos: 1.4×).
- After soft launch, both values are re-fitted from real player data: target par = 25th–30th percentile of real first-clear times; time limit tuned so time-up fails are ≤ 40% of all fails on that level.

## 5.7 Scoring candidates (fun proxies)

Validators keep levels from being *bad*; scoring tries to surface the *good* ones for designers.

| Signal | Proxy | Weight |
|---|---|---|
| **Twist impact** | How much the twist changes the bot's optimal plan (plan edit distance) | High |
| **Decision density** | Meaningful decisions per 10 seconds | High |
| **Comedy potential** | Theme absurdity × number of "funny" tagged objects (designer-tagged, e.g. rubber chicken, toupee) | Medium |
| **Physics spectacle** | Expected number of physics events (bounces, breaks, tips) in human-like sims — but capped (too many = chaos noise) | Medium |
| **Near-miss rate** | % of bot losses with ≥ 90% progress (good: creates "so close!") | Medium |
| **Visual harmony** | Palette conformity score vs. theme palette | Low |

Designers see the top 20 candidates per slot in the editor, ranked, with a 10-second bot-play preview GIF for each.

## 5.8 How we avoid the four bad-level types

| Bad type | Cause | Prevention |
|---|---|---|
| **Boring** | No twist impact, trivial mapping, repeated theme | V11, V12, scoring, theme rotation rules, mandatory twist slot from L5 |
| **Impossible** | Capacity < objects, unbalanced scales, timers too tight | V1, V2, V7, V9 |
| **Repetitive** | Same archetype/twist/theme family in a row | Recency rules: archetype not within 3 levels, twist type not within 4, theme family not within 3, novelty distance (V12) |
| **Unfair** | Hidden information, ambiguous art, untelegraphed events, environment-caused Oops | V3, V4, V8, physics fairness rules (Ch. 02 §2.5), environmental accidents never cost Oops |

Post-launch, the **level health dashboard** (Ch. 10) closes the loop: any level with first-try win rate outside its band by > 15 points, or a churn spike > 2× chapter average, is auto-flagged for review or replacement via remote config.

## 5.9 Thirty-six generation recipes

Each row is one generator recipe (archetype + parameter ranges). One recipe yields hundreds of distinct levels by swapping theme, objects, seed and twist.

| # | Theme (example) | Object set | Containers | Sort rule | Special rule | Physics | Time | Objs / Bins | Fail | Tier | Twist |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Toy box | Blocks, balls | 3 bins | Colour | — | Standard | — | 9 / 3 | — | Easy | Pour-in of a 4th colour + new bin |
| 2 | Bakery | Cupcakes, donuts, cookies | 3 trays w/ capacity | Category | — | Standard | — | 12 / 3 | Overflow | Easy | Tray capacities shrink by 1 at 50% |
| 3 | Laundromat | Socks | 4 baskets | Pairs | — | Standard | — | 16 / 4 | Oops | Medium | Two socks are actually puppets (living) |
| 4 | Gym | Dumbbells, towels, bottles | 3 racks, 1 weight-limited | Category | Heavy | Standard | — | 12 / 3 | Weight | Medium | A kettlebell drops in late (5 kg) |
| 5 | Farmers market | Eggs, melons, potatoes | 3 crates | Category | Fragile eggs | Standard | 60 | 14 / 3 | Break | Medium | Melons roll when the table bumps |
| 6 | Wizard school | Potions, wands, books | 3 shelves | Owner (house colours) | Fragile potions | Standard | — | 15 / 3 | Break | Hard | One potion is disguised (it's a frog) |
| 7 | Grandma's attic | Doilies, vinyl, skateboards | Grandma / Teen box | Owner | Disguised | Standard | — | 16 / 2 | Oops | Medium | A third owner (the cat) appears |
| 8 | Fruit stand | Fruit with moustaches | Fruit / Impostor | Category | Disguised ×3 | Standard | 45 | 14 / 2 | Oops | Medium | Impostors swap disguises at 50% |
| 9 | Pet shop | Hamsters, fish bags, bird | Cages | Species | Living | Standard | — | 10 / 3 | Escape | Medium | Cage doors open every 8 s |
| 10 | Dragon nursery | Eggs, gold, baby dragon | Nest / Hoard / Crib | Category | Sleeping + Fragile | Standard | — | 14 / 3 | Wake + Break | Hard | A second baby is hidden in the gold |
| 11 | Sushi bar | Fish, rolls, wasabi | Conveyor plates | Request queue | Perishable | Conveyor | — | 18 / 4 | Spoil | Hard | Rush hour: conveyor speed ×1.5 |
| 12 | Ice-cream truck | Cones, popsicles | Freezer / Bin | State | Perishable (melting) | Standard | 50 | 14 / 2 | Spoil | Medium | The freezer door jams closed for 6 s |
| 13 | Dragon kitchen | Frozen meat, pans, oven mitts | Fireproof / Fridge / Drawer | Category | Frozen + Hot | Standard | — | 15 / 3 | Oops | Hard | The dragon sneezes fire (one-time heat wave) |
| 14 | Junk drawer | Keys, clips, batteries, rubber bands | Metal / Other | Material | Magnet | Standard | — | 20 / 2 | Oops | Medium | A second magnet appears in the "other" bin |
| 15 | Toddler lunch | Jam toast, toys, crayons | Kitchen / Toybox / Art | Category | Sticky | Standard | — | 15 / 3 | Oops | Medium | Toddler throws another jam toast in |
| 16 | Party | Balloons, gifts, cake | Lidded box / Table / Fridge | Category | Floaty + Fragile | Standard | — | 14 / 3 | Break | Hard | Pin falls in: popping one balloon pushes others |
| 17 | Power plant | Lunch food, radioactive banana | Food / Drinks / Hazard | Category + state | Radioactive | Standard | — | 12 / 3 | Contamination | Medium | A second radioactive item rolls in |
| 18 | Science fair | Specimen jars, mold bread | Display / Trash | State | Mold | Standard | 60 | 16 / 2 | Contamination | Hard | Fan turns on and blows spores |
| 19 | Pirate ship | Gold, rum, cannonballs, parrot | Chest / Barrel / Rack | Category | Heavy + Living (parrot) | Tilt 8° | — | 16 / 3 | Weight | Hard | Storm: tilt amplitude doubles |
| 20 | Space station | Tools, snacks, astronaut socks | Lockers | Category | Floaty (zero-G) | Low gravity | — | 15 / 4 | Oops | Hard | Gravity turns on briefly |
| 21 | Haunted attic | Ghost-objects, dolls | Blessed / Normal | Category | Ghost + Shy | Standard | — | 12 / 2 | Oops | Hard | Lights out at 50% |
| 22 | Fireworks factory | Rockets, sparklers, matches | Blast bin / Crate | Category | Fuse ×2 | Standard | — | 12 / 2 | Explode | Hard | Matches + hot → new fuse lights |
| 23 | Recycling plant | Glass, paper, plastic, metal | 4 bins on moving rail | Material | Fragile glass | Moving bins | 60 | 20 / 4 | Break, Time | Hard | Bins swap order once (telegraphed) |
| 24 | Airport security | Suitcase contents | Allowed / Confiscated | Hidden rule (inferred) | Nested (bags) | Conveyor | — | 18 / 2 | Oops | Hard | Rule changes: "liquids allowed now" |
| 25 | Monster daycare | Monsters | Species pens | Species | Living + Hungry | Standard | — | 12 / 3 | Eaten | Hard | One monster is a mimic of another species |
| 26 | Museum heist | Artefacts (some fake) | Real / Fake / Bag | Authenticity | Disguised + Fragile | Standard | 45 | 15 / 3 | Break, Time | Extreme | Lights out (guard flashlight passes) |
| 27 | Alien suitcase | Unknown objects | Picky bins (alien glyphs) | Inferred | Chameleon | Low gravity | — | 15 / 3 | Oops | Extreme | Glyphs rotate (rule change) at 50% |
| 28 | Volcano lab | Lava rocks (hot), ice samples, paper notes | Fireproof / Freezer / Folder | Category | Hot + Frozen + Flammable | Quake | — | 16 / 3 | Fire spread | Extreme | Eruption: hot rocks pour in |
| 29 | Mad scientist | Slimes, beakers | Colour jars | Colour | Splitter + Fragile | Standard | 60 | 12 / 4 | Overflow | Extreme | Slimes merge into a new colour |
| 30 | Opposite day office | Office supplies | Mirror bins | Opposite day | Moving | Standard | — | 15 / 3 | Oops | Extreme | Rule flips back to normal at 60% |
| 31 | Shadow theatre | Invisible props | Silhouette slots | Shadow shape | Invisible | Standard | — | 12 / 4 | Oops | Hard | The lamp moves → shadows stretch |
| 32 | Royal banquet | Dishes, crown | Tables in order | Sequence (VIP first) | Fragile + VIP | Standard | 60 | 14 / 3 | Wrong sequence | Hard | A second VIP (the queen) arrives |
| 33 | Zombie grocery | Food, zombie hands | Fresh / Spoiled / Zombie | State | Perishable + Living | Standard | 60 | 18 / 3 | Spoil, Eaten | Chaos | Lights flicker; spoil rate doubles |
| 34 | Black hole post office | Parcels | Planet mailboxes | Destination (size) | Heavy + Gravity well (pulls light objects) | Custom gravity | — | 16 / 4 | Oops | Chaos | Gravity flip for 3 s |
| 35 | Bomb-disposal pizzeria | Pizza toppings, a pizza-shaped bomb | Pizza boxes / Blast bin | Category | Fuse + Disguised + Sticky cheese | Tilt (delivery scooter) | 50 | 18 / 3 | Explode, Time | Chaos | A second pizza is also a bomb |
| 36 | The Sorting Machine itself | Its own parts, cogs, a tiny manager | Labelled by the machine | Inferred + rule change | Magnet + Clone | Quake | 75 | 20 / 4 | Oops | Chaos | The machine starts sorting *you* (containers move toward your finger) |

**Combinatorics:** 40 archetypes × 70 themes × 32 behaviours (choose ≤ 3 within compatibility rules ≈ 2,000 legal combos) × 10 environment events × twist pool (25) → well over **10⁶ raw candidates**. After validation and novelty filtering, a single chapter brief typically yields 100–300 shippable candidates per slot. Content supply is not the bottleneck; curation and object art are.
