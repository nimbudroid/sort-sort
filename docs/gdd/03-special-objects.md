# 03 — Special Objects, Containers, Environments & Rules

[← Index](README.md)

---

The game's depth comes from a **library of composable modifiers**. Every level is built from four kinds of modifier:

| Kind | Lives on | Examples |
|---|---|---|
| **Object behaviours** | Individual objects | Fragile, Sticky, Radioactive, Living |
| **Container modifiers** | Containers | Weight limit, Moving, Lid timer, Picky |
| **Environment events** | The whole scene | Tilt, Lights out, Container swap, Conveyor |
| **Rule types** | The level's sorting logic | By colour, by owner, inferred, opposite day |

Each modifier is one self-contained component in code (see [Ch. 10](10-tech-analytics.md#103-object-system)), so any combination is technically possible. Which combinations are *allowed* is governed by the compatibility matrix in §3.6 and the difficulty budget in [Ch. 04](04-progression-difficulty.md#44-difficulty-budget).

---

## 3.1 Object behaviour library (32 behaviours)

**How to read each card:**
- **Does** — the rule in one line.
- **Changes sorting** — the new decision it forces.
- **Introduced** — level number and the teaching setup.
- **Combos** — the strongest pairings.
- **Fun because** — the emotional reason it exists.
- **Tell** — how the player recognises it without text.

### Physical behaviours

#### 1. HEAVY
- **Does:** Mass class 4–5. Lags and sags when dragged. Thuds. Crushes fragile objects it lands on.
- **Changes sorting:** Weight limits and stacking order now matter.
- **Introduced:** As juice from L12 (anvils and bowling balls thud). As a rule at **L26**: one bin has a 10 kg gauge; three anvils + feathers.
- **Combos:** + Fragile (order puzzles) · + Scale (balancing) · + Tilt (heavy objects slide and slam) · + Floating (use heavy to pin balloons down).
- **Fun because:** You *feel* it in your thumb — the drag lag is a physical joke.
- **Tell:** Thick, dark material; dust puff on landing; deep "thunk".

#### 2. FRAGILE
- **Does:** Breaks above an impact threshold.
- **Changes sorting:** You must *lower* objects, not drop them; heavy objects go in first.
- **Introduced:** **L33** — "Sort eggs into cartons." The first egg most players drop from high will crack (warning decal), the second break teaches it. Level has no Oops cost for breaks — broken eggs go in a "scrambled" bin. Breaks become a fail from L36.
- **Combos:** + Heavy · + Flick (throwing is now risky) · + Tilt · + Explosive · + Bouncy (bouncy object hitting fragile ones).
- **Fun because:** Pure tension — "gently… gently…" — and the crash is hilarious.
- **Tell:** Glassy sheen, sparkle, "tink" on touch.

#### 3. BOUNCY
- **Does:** High restitution; can bounce out of open containers.
- **Changes sorting:** Drop gently, use dampening (put it on soft things), or bank-shot it.
- **Introduced:** **L44** — "Sort the rubber duck squad." Empty bins send the first duck flying out; bins with a towel in them don't.
- **Combos:** + Fragile · + Lid timers · + Moving containers (trick shots).
- **Fun because:** Unpredictable-looking but learnable; produces the best trick-shot clips.
- **Tell:** Squashes visibly on any contact; "boing".

#### 4. ROUND (ROLLER)
- **Does:** Rolls on slopes and when bumped.
- **Changes sorting:** Ignore it and it wanders off the table into the spill area or wrong bins.
- **Introduced:** **L50** — marbles on a slightly tilted table.
- **Combos:** + Tilt (heavy synergy) · + Moving containers.
- **Fun because:** Light urgency without a clock.
- **Tell:** Spherical silhouette; rolls a few pixels on spawn.

#### 5. SLIPPERY
- **Does:** Slips out of the player's grip if dragged faster than 900 dp/s.
- **Changes sorting:** Fish and soap must be moved deliberately; speed players get punished with a comic slip.
- **Introduced:** **L53** — "Sort the fish market" (fish wriggle and slip).
- **Combos:** + Timer (speed vs. control) · + Living.
- **Fun because:** Slapstick — the fish flies out of your hand.
- **Tell:** Wet shine, drip particles.

#### 6. FLOATY
- **Does:** Negative gravity; drifts up and out of open containers.
- **Changes sorting:** Must be pinned with a heavy object or placed in a lidded container; order matters.
- **Introduced:** **L126** — party balloons drifting to the ceiling.
- **Combos:** + Heavy · + Sticky (a sticky balloon carries other objects away!) · + Explosive (popping).
- **Fun because:** Inverts the core assumption that things fall.
- **Tell:** Bobbing idle animation; string.

#### 7. STICKY
- **Does:** Glues to whatever it touches. Dragging one drags the clump. A sharp shake breaks the joint.
- **Changes sorting:** Avoid letting sticky things touch; or sort the clump as one if all pieces belong together.
- **Introduced:** **L116** — "Sort the toddler's lunch" (a jam sandwich glues to toys).
- **Combos:** + Radioactive (sticky contamination is brutal — Chaos tier only) · + Floating · + Magnet.
- **Fun because:** Physical comedy; the "shake it off" gesture is satisfying.
- **Tell:** Glistening goo, stretchy strands when near other objects.

#### 8. MAGNETIC
- **Does:** Pulls metal-tagged objects within 120 dp; they cling to it.
- **Changes sorting:** A tool. Drag the magnet through the pile to harvest all metal, drop the cluster in the metal bin. But it also pulls metal *out* of the correct bins if you swing it near them.
- **Introduced:** **L121** — "Sort the junk drawer": one magnet, lots of paperclips and keys.
- **Combos:** + Sticky · + Explosive (bomb with a metal casing gets pulled to you…) · + Mimic.
- **Fun because:** The first "tool" — players feel clever.
- **Tell:** Horseshoe shape; faint field lines on hold.

#### 9. SPLITTER
- **Does:** Splits into two smaller copies when dropped hard (slime, amoeba, gremlin-like jelly).
- **Changes sorting:** Total object count can increase if you're careless.
- **Introduced:** **L171** — "Sort the lab slimes."
- **Combos:** + Capacity · + Timer · + Bouncy.
- **Fun because:** The pile multiplying is a laugh-out-loud failure spiral that's still recoverable.
- **Tell:** Jiggle, visible cell nucleus.

#### 10. NESTED (MATRYOSHKA)
- **Does:** Contains other objects. Poke (tap) to open; contents spill out and must be sorted too.
- **Changes sorting:** Hidden objects; containers may already be near capacity when the surprise comes out.
- **Introduced:** **L8** (a gift box — the earliest special object, low stakes) and fully at **L64** with nested dolls.
- **Combos:** + Disguised · + Explosive (the box contains a bomb) · + Capacity.
- **Fun because:** Unboxing; surprise.
- **Tell:** Ribbon/lid/seam.

### Perception behaviours

#### 11. DISGUISED
- **Does:** Looks like category A; is actually category B. Hold (Inspect) reveals truth.
- **Changes sorting:** Trust but verify. Not every object needs inspecting — only those with a *tell*.
- **Introduced:** **L56** — "Sort the fruit bowl": one banana has a tiny moustache. Placing it in "fruit" makes the bin spit it out yelling "IMPOSTOR". Next levels teach Hold.
- **Combos:** + Timer (inspection costs time) · + Living · + Radioactive (a disguised radioactive object…).
- **Fun because:** "Gotcha!" moments; players start seeing suspects everywhere.
- **Tell:** One wrong detail (moustache, sunglasses, zipper, eyebrows). **A disguised object must always have a tell.**

#### 12. INVISIBLE
- **Does:** Only its shadow / outline / dust silhouette is visible.
- **Changes sorting:** Identify by shadow shape.
- **Introduced:** **L60** — "Sort the invisible man's laundry."
- **Combos:** + Lights out · + Living (an invisible pet walks around — you see footprints).
- **Fun because:** A pure perception puzzle inside a physical game.
- **Tell:** Floating shadow; dust motes.

#### 13. MIMIC
- **Does:** Every 4 seconds, copies the appearance of the nearest object. Its true identity shows briefly (0.3 s flicker) at each transformation.
- **Changes sorting:** Watch for the flicker; sort it into the *mimic bin*. Putting a mimic in a normal bin: it bites the bin (Oops) and jumps out.
- **Introduced:** **L65** — "Sort the treasure chest" (a classic chest-mimic gag).
- **Combos:** + Disguised · + Living · + Chameleon.
- **Fun because:** It's a monster hunt hidden inside a tidy-up.
- **Tell:** Flicker + a tiny tongue.

#### 14. CHAMELEON
- **Does:** Cycles colour every 3 s on a visible cycle (colour ring shows the upcoming colour).
- **Changes sorting:** Colour rule depends on *when* you drop it. Timing puzzle.
- **Introduced:** **L196**.
- **Combos:** + Colour rules (obviously) · + Moving containers · + Combo chains.
- **Fun because:** Rhythm-like timing.
- **Tell:** Colour ring.

#### 15. QUANTUM (late-game joke)
- **Does:** Its category is undecided until you Inspect it; inspecting fixes it randomly between two options (shown as a split icon).
- **Changes sorting:** Inspect when the container it would fit has room.
- **Introduced:** **L260+**, Chaos chapters only.
- **Fun because:** The "WTF" factor, clearly signposted.
- **Tell:** Half-and-half texture, Schrödinger box sticker.

### Living behaviours

#### 16. LIVING (WANDERER)
- **Does:** Walks around the Mess Zone on simple paths; climbs out of containers after 5 s unless lidded or the container is full.
- **Changes sorting:** Prioritisation — sort living things last (when their bin is nearly full) or first (before they spread out).
- **Introduced:** **L71** — "Sort the hamsters."
- **Combos:** + Sleeping · + Shy · + Capacity · + Radioactive (a radioactive rat walks around contaminating things!).
- **Fun because:** The mess fights back, cutely.
- **Tell:** Idle animations, eyes.

#### 17. SLEEPING
- **Does:** Living object asleep. Loud impacts (heavy landings, breaks) nearby wake it, and it becomes a Wanderer.
- **Changes sorting:** Be quiet. Move gently around it, sort it first, or sort heavy things away from it.
- **Introduced:** **L75** — "Sort the nursery without waking the baby dragon."
- **Combos:** + Heavy (noise) · + Fragile · + Explosive.
- **Fun because:** Stealth tension in a sorting game.
- **Tell:** Zzz bubbles; snoring SFX.

#### 18. SHY
- **Does:** Runs away from the finger if approached faster than a threshold. Slow approach = can grab.
- **Changes sorting:** Patience. Or trap it in a corner with other objects.
- **Introduced:** **L79** — "Sort the haunted dolls" (they skitter away).
- **Combos:** + Timer · + Living.
- **Fun because:** A little chase game.
- **Tell:** Nervous darting eyes.

#### 19. HUNGRY
- **Does:** Eats one adjacent small food-tagged object every 4 s. Eaten objects are gone (count reduced) — if a level needs "all food sorted", hungry objects can make you fail.
- **Changes sorting:** Sort the food near the hungry creature first, or feed it deliberately (feeding sometimes *is* the solution: the creature grows heavier).
- **Introduced:** **L83** — "Sort the picnic before the goat eats it."
- **Combos:** + Perishable · + Living · + Weight limits.
- **Fun because:** Panic and comedy.
- **Tell:** Chomping animation, drool.

#### 20. VIP / ROYAL
- **Does:** Must be sorted first (or must be sorted last). Others are rejected until VIP is done.
- **Changes sorting:** Order puzzle; the VIP is often buried in the pile.
- **Introduced:** **L97**.
- **Combos:** + Buried pile · + Living (the VIP walks around).
- **Tell:** Crown, red carpet under it.

### Time behaviours

#### 21. PERISHABLE
- **Does:** Has a visible freshness ring that drains (8–20 s). When it spoils it changes type (fresh fish → stinky fish) and must go to a different bin (compost/trash).
- **Changes sorting:** Priority — sort the most perishable first, or *wait* for it to spoil if the fresh bin is full.
- **Introduced:** **L101** — "Stock the fridge before the milk turns."
- **Combos:** + Frozen (frozen items don't spoil) · + Hungry · + Timer.
- **Fun because:** Many small clocks instead of one big clock — triage.
- **Tell:** Freshness ring; stink lines when spoiled.

#### 22. FROZEN
- **Does:** Encased in ice. Can't be identified (blurred inside) until thawed — tap 3× to crack, or place near a hot object. Ice is slippery.
- **Changes sorting:** Choice between spending taps (time) or using heat (positioning).
- **Introduced:** **L105** — "Sort the freezer."
- **Combos:** + Hot · + Perishable · + Slippery.
- **Fun because:** Smashing ice is tactile; using heat is clever.
- **Tell:** Ice block.

#### 23. HOT
- **Does:** Melts frozen objects it touches; ignites Flammable objects; burns Paper. Must go into fireproof containers.
- **Changes sorting:** Positioning — hot objects are both tools and hazards.
- **Introduced:** **L109** — "Sort the dragon's kitchen."
- **Combos:** + Frozen (tool) · + Flammable (hazard) · + Explosive (chain reaction).
- **Tell:** Heat shimmer, glow.

#### 24. FLAMMABLE
- **Does:** Catches fire from Hot objects; burning objects burn neighbours after 2 s and become Ash (sortable as trash).
- **Introduced:** **L112**.
- **Combos:** + Hot · + Explosive · + Sticky.

#### 25. FUSE (TIME-BOMB / EXPLOSIVE)
- **Does:** Has a visible fuse timer (10–25 s, starts when touched or on level start). Must reach the blast bin before it explodes. Explosion = radial impulse, breaks fragile objects, scatters the pile. Fail only if the explosion is outside the blast bin *and* the level says "no explosions"; otherwise it's chaos to recover from.
- **Changes sorting:** Immediate prioritisation + spatial thinking (what's near the bomb?).
- **Introduced:** **L161** — "Sort the fireworks factory."
- **Combos:** + Fragile · + Magnet · + Sticky (sticky bomb!) · + Living (a creature carrying a bomb).
- **Fun because:** Huge visual payoff; best fail clips.
- **Tell:** Lit fuse, ticking SFX, flashing at 3 s.

### Hazard behaviours

#### 26. RADIOACTIVE
- **Does:** Any object it touches becomes **contaminated** (glows green) and must go to the hazard bin instead of its normal bin. Radioactive object itself goes to the lead box.
- **Changes sorting:** Spatial avoidance. Lifting the radioactive object over other objects is safe; dragging it *through* them contaminates. Contamination changes the sort target of other objects mid-level — the board changes under you.
- **Introduced:** **L131** — "Sort the power plant lunchroom." One glowing banana; touching anything else with it turns that green. The hazard bin has plenty of room the first time.
- **Combos:** + Living (radioactive rat) · + Sticky · + Capacity of the hazard bin (the real constraint) · + Tilt (radioactive object rolls).
- **Fun because:** A new layer of spatial thinking; the "green spreading" is visually striking.
- **Tell:** Green glow, Geiger click that speeds up when near objects.

#### 27. MOLD (CONTAGIOUS)
- **Does:** Spreads to an adjacent object every 5 s if left in the pile. Moldy objects go to the trash.
- **Changes sorting:** Get the mold out fast, or sort clean objects away from it.
- **Introduced:** **L136**.
- **Combos:** + Perishable · + Hungry (the goat eats mold and gets sick!).

#### 28. BAD APPLE
- **Does:** One object that, if placed in a container with others, ruins the whole container at level end (all contents count as wrong). Must be isolated.
- **Introduced:** **L141** — "One bad apple."
- **Combos:** + Disguised (which apple is bad?) · + Capacity.
- **Fun because:** A last-second gotcha the player learns to check for.

### Rule-bending behaviours

#### 29. CLONE
- **Does:** If placed in the wrong container, duplicates instead of being spat back (now two to sort).
- **Introduced:** **L201**.
- **Combos:** + Capacity · + Timer.

#### 30. GHOST
- **Does:** Phases out every 4 s (becomes intangible — passes through container walls and floors). Must be dropped while solid, or into a "blessed" bin.
- **Introduced:** **L191** — "Sort the haunted attic."
- **Combos:** + Shy · + Lights out.

#### 31. KEY
- **Does:** Unlocks a locked container when sorted into it.
- **Introduced:** **L93**.
- **Combos:** + Buried pile · + Living (a dog has the key in its mouth).

#### 32. GIFT / MYSTERY
- **Does:** Poke to open; reveals a random object from a weighted set *chosen by the generator at level creation* (deterministic per level seed).
- **Introduced:** **L8** (low stakes).
- **Combos:** + Nested · + Explosive.

---

## 3.2 Container modifier library (18 modifiers)

| # | Modifier | Does | Introduced | Best combos |
|---|---|---|---|---|
| C1 | **Capacity** | Holds N objects (shown as slots) | L6 | Clone, Splitter, Nested |
| C2 | **Weight limit** | Gauge; collapses above limit | L26 | Heavy, Hungry, Scale |
| C3 | **Scale (two-pan)** | Must balance within tolerance | L30 | Heavy, Floaty |
| C4 | **Jar (narrow mouth)** | Only S-size fits | L18 | Bouncy, Flick |
| C5 | **Shape slot** | Exact silhouette match | L14 | Disguised, Chameleon |
| C6 | **Lid timer** | Opens/closes on a visible cycle | L46 | Bouncy, Flick, Living |
| C7 | **Moving** | Slides left-right on a rail | L86 | Flick, Fragile |
| C8 | **Swapping** | Two containers swap places every N s (telegraphed) | L90 | Timer, Chameleon |
| C9 | **Conveyor** | Containers arrive and leave on a belt | L94 | Perishable, Sequence rules |
| C10 | **Locked** | Requires Key | L93 | Key, Living |
| C11 | **Picky (hidden rule)** | Accepts by a rule you must infer from accept/reject feedback | L23 | Inference levels |
| C12 | **Hungry container (creature)** | A creature-mouth that rejects with more personality; can also be fed to unlock | L38 | Living, Hungry |
| C13 | **Tippy** | Tips if unevenly loaded | L148 | Heavy, Tilt |
| C14 | **Leaky** | Bottom opens every N s, dropping contents onto a lower bin | L157 | Sequence, Two-stage sorting |
| C15 | **Shrinking** | Capacity reduces every 10 s | L168 | Timer pressure |
| C16 | **Mirror label** | Shows its rule mirrored/reversed (Opposite day) | L181 | Rule change |
| C17 | **Fireproof / Lead-lined** | Only container that accepts Hot / Radioactive | L109 / L131 | Hazards |
| C18 | **Teleport pair** | Dropping into A delivers to B | L210+ | Sequence, Moving |

## 3.3 Environment events (10 events)

Only one environment event is active per level below Chaos tier.

| # | Event | Does | Telegraph | Introduced |
|---|---|---|---|---|
| E1 | **Pour-in** | New wave of objects pours in mid-level | Bag/chute shakes 1 s | L5 (the first twist) |
| E2 | **Container pop-up** | A new container appears mid-level with a new rule | Ground rumble | L5 |
| E3 | **Tilt / Rocking** | Scene rotates on a sine wave | Creak + wave icon | L146 |
| E4 | **Quake** | Short random shake that loosens the pile | Rumble 1 s | L150 |
| E5 | **Lights out** | Screen goes dark; flashlight cone follows the finger | Bulb flickers 1 s | L155 |
| E6 | **Rule change** | Container rules re-stamp mid-level | Stamp-lift animation, alarm | L176 |
| E7 | **Wind gust** | Pushes light objects sideways | Leaves blow in 1 s ahead | Post-launch |
| E8 | **Gravity flip (space)** | Gravity reverses for 3 s | Warning siren | L224+ |
| E9 | **Customer rush** | A queue of "customers" requests specific objects (sequence) | Bell | L99 |
| E10 | **Flood** | Water rises; floaty objects float, others sink (buoyancy) | Dripping | Post-launch (Ocean world) |

## 3.4 Sorting rule types

| # | Rule type | Example | Cognitive load | Introduced |
|---|---|---|---|---|
| R1 | **Colour** | Red / blue / yellow | 1 | L1 |
| R2 | **Shape** | Round / square / star | 1 | L3 |
| R3 | **Category (semantic)** | Fruit vs. veg; socks vs. shoes | 2 | L4 |
| R4 | **Size** | S / M / L | 2 | L11 |
| R5 | **Two-attribute** | "Red AND round" vs. "blue OR square" | 3 | L13 |
| R6 | **Owner** | Grandma's stuff vs. the punk teen's stuff (style read) | 2 | L17 |
| R7 | **Odd one out** | "?" bin takes anything that doesn't belong | 2 | L9 |
| R8 | **Inferred / Picky** | Bins show one example only; deduce from feedback | 3 | L23 |
| R9 | **Count / Quantity** | Each bin needs exactly N | 2 | L21 |
| R10 | **Weight** | Heavier than / lighter than; balance | 3 | L30 |
| R11 | **Sequence** | Put in order (smallest first, A–Z, by date) | 3 | L68 |
| R12 | **Pairs** | Match pairs into the same bin (socks!) | 2 | L15 |
| R13 | **State-based** | Fresh vs. spoiled; clean vs. contaminated | 3 | L101 |
| R14 | **Request queue** | Customers ask for items in a sequence | 3 | L99 |
| R15 | **Opposite day** | Put everything in the *wrong* bin | 4 | L181 |
| R16 | **Sort by shadow/reflection** | Bins show silhouettes; objects only shown as shadows | 3 | L186 |
| R17 | **Meta / joke rule** | "Sort by how much the object loves you" (hearts appear on Inspect) | 3 | Chaos chapters, ≤ 1 per chapter |

## 3.5 How mechanics are introduced: the 4-step teaching pattern

Every new mechanic follows the same pattern across ~4 levels. No text tutorials.

| Step | Level slot | Design rule | Example (Fragile) |
|---|---|---|---|
| **1. Meet it** | 1st level | The mechanic appears in a **safe** context; failing it has no cost; the *first natural mistake* produces the lesson | Eggs, no break penalty, broken eggs just go to a "scrambled" bin |
| **2. Use it** | 2nd level | The mechanic is the whole point; low pressure | Only fragile objects; cartons near the pile |
| **3. Respect it** | 3rd level | Failing it now has a cost; combine with one familiar mechanic | Eggs + anvils (heavy), breaks cost an Oops |
| **4. Be surprised by it** | Twist of a later level | The mechanic appears where you don't expect it | A "Sort the bakery" level where one cake is fragile and only revealed when you drop it |

## 3.6 Compatibility matrix (what can combine)

✅ = allowed and encouraged · ⚠️ = allowed only at Extreme/Chaos tier · ❌ = never (unreadable or degenerate)

| | Heavy | Fragile | Bouncy | Sticky | Magnet | Disguised | Living | Perishable | Frozen/Hot | Explosive | Radioactive | Tilt | Lights out |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Heavy** | — | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| **Fragile** | | — | ✅ | ⚠️ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ❌ |
| **Bouncy** | | | — | ⚠️ | ✅ | ✅ | ⚠️ | ✅ | ✅ | ⚠️ | ⚠️ | ⚠️ | ❌ |
| **Sticky** | | | | — | ✅ | ⚠️ | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | ✅ | ❌ |
| **Magnet** | | | | | — | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ⚠️ |
| **Disguised** | | | | | | — | ⚠️ | ✅ | ⚠️ | ✅ | ⚠️ | ✅ | ❌ |
| **Living** | | | | | | | — | ✅ | ✅ | ⚠️ | ✅ | ⚠️ | ⚠️ |
| **Perishable** | | | | | | | | — | ✅ | ✅ | ✅ | ✅ | ⚠️ |
| **Frozen/Hot** | | | | | | | | | — | ✅ | ⚠️ | ✅ | ⚠️ |
| **Explosive** | | | | | | | | | | — | ⚠️ | ⚠️ | ❌ |
| **Radioactive** | | | | | | | | | | | — | ⚠️ | ❌ |
| **Tilt** | | | | | | | | | | | | — | ❌ |

Key exclusions and why:
- **Lights out + any perception mechanic (Disguised, Invisible, Fragile tells)** — removes the information the player needs. Unfair.
- **Explosive + Radioactive below Chaos** — an explosion contaminating everything is an un-recoverable state.
- **Tilt + Lights out** — two "world" events at once; unreadable.
