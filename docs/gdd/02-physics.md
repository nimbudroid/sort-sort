# 02 — Physics System

[← Index](README.md)

---

## 2.1 Role of physics

Physics is what turns "a sorting game" into "controlled chaos". But physics is also the fastest way to make a puzzle game feel unfair. The design stance:

> **Physics is a toy first, a rule second, a threat third — in that order, and each stage is earned.**

1. **Toy (L1+)** — Everything has gravity, mass and material from the first second. Objects tumble out of the bag, stack in bins, clack together. This is pure juice.
2. **Rule (L26+)** — A physical property becomes the sorting constraint: "this bin holds 10 kg", "eggs break if dropped from height".
3. **Threat (L146+)** — The *environment* moves: the ship rocks, the floor tilts, an explosion scatters the pile.

## 2.2 Mechanic selection

We evaluated 16 candidate physics mechanics on four criteria: **readability** (can the player see it?), **agency** (does the player's skill affect it?), **combinability** (does it make other mechanics more interesting?), and **cost** (engineering + tuning).

| Mechanic | Readability | Agency | Combinability | Cost | Decision | Where |
|---|---|---|---|---|---|---|
| **Gravity + stacking** | ★★★★★ | ★★★★☆ | ★★★★★ | Low | **CORE** | Always on |
| **Weight (mass classes)** | ★★★★☆ | ★★★★☆ | ★★★★★ | Low | **CORE** | Rule from Ch3 |
| **Fragility (impact break)** | ★★★★★ | ★★★★★ | ★★★★★ | Low | **CORE** | Ch3 |
| **Bounce** | ★★★★★ | ★★★★☆ | ★★★★☆ | Low | **CORE** | Ch4 |
| **Rolling (round objects)** | ★★★★☆ | ★★★☆☆ | ★★★★☆ | Low | **CORE** | Ch4 |
| **Balance / tipping** (containers, scales, see-saws) | ★★★★☆ | ★★★★☆ | ★★★★☆ | Med | **CORE** | Ch3 (scales), Ch11 (tipping) |
| **Environment tilt** (rocking ship, earthquakes) | ★★★★☆ | ★★★☆☆ | ★★★★★ | Med | **CORE** | Ch11 |
| **Sticking** (sticky objects glue together) | ★★★★☆ | ★★★★☆ | ★★★★☆ | Med | **SECONDARY** | Ch9 |
| **Magnetism** | ★★★★☆ | ★★★★★ | ★★★★☆ | Med | **SECONDARY** | Ch9 |
| **Explosions (radial impulse)** | ★★★★★ | ★★☆☆☆ | ★★★★☆ | Med | **SECONDARY, rationed** | Ch12 |
| **Floating (anti-gravity balloons)** | ★★★★☆ | ★★★☆☆ | ★★★☆☆ | Low | **SECONDARY** | Ch9 |
| **Buoyancy (water tank)** | ★★★☆☆ | ★★☆☆☆ | ★★★☆☆ | High | **LATER** (Ocean world, post-launch) | Month 3+ |
| **Wind / airflow** | ★★★☆☆ | ★★☆☆☆ | ★★★☆☆ | Med | **LATER** | Month 6+ |
| Friction as a mechanic | ★★☆☆☆ | ★☆☆☆☆ | ★★☆☆☆ | Low | **CUT** — only as a property (ice/soap are "slippery") | — |
| Momentum as a mechanic | ★★☆☆☆ | ★★★☆☆ | ★★☆☆☆ | — | **CUT** — implicit in flick | — |
| Breaking containers | ★★★★☆ | ★★★☆☆ | ★★☆☆☆ | Med | **Fail animation only**, not a mechanic | — |
| Soft-body / liquids | ★★★☆☆ | ★★☆☆☆ | ★★☆☆☆ | Very high | **CUT** | — |

**Result: 7 core physics mechanics at launch scope (MVP uses 4), 4 secondary, 2 later.**

## 2.3 How each core physics mechanic creates gameplay

### Gravity + stacking
- **What it does:** Objects fall, rest, and pile up. Containers fill bottom-up.
- **Gameplay it creates:** *Digging.* The pile hides objects under other objects. Players must sort the top layer to reveal what's beneath — a natural "reveal" pacing tool. In containers, stacking order matters once fragility or weight is involved.
- **Tuning:** Objects in the pile are placed by the generator in "layers" so at least 40% of objects are graspable at start.

### Weight
- **What it does:** Each object has a mass class 1–5 (shown via drag lag, sag and impact sound — and a small weight glyph on Inspect).
- **Gameplay it creates:**
  - **Weight-limited containers:** a bin shows a needle gauge; exceeding it collapses the bin (fail) or tips it.
  - **Scales:** "Make both pans equal." Combinatorial puzzle using physical objects.
  - **Crushing:** heavy objects landing on fragile ones break them → **order matters** (heavy first, fragile on top).
- **Why it works:** Weight converts a classification puzzle into a *packing* puzzle without adding a new verb.

### Fragility
- **What it does:** Fragile objects (eggs, vases, crystal skulls, snow globes) break if hit with impulse above a threshold — dropped from height, thrown, landed on by a mass-4+ object, or knocked by a spit-back.
- **Gameplay it creates:** Care vs. speed. Players must *lower* fragile objects into bins (release near the bottom) instead of dropping from above. Combined with the timer, it's the cleanest skill tension in the game.
- **Readability:** Fragile objects have a subtle sparkle and a "tink" when touched; a crack decal appears on near-misses (impulse 70–99% of threshold) — the warning before the break.
- **Rule:** A broken object is a fail *only* if the level says so (rule chip "🥚 no breaks"); otherwise the shards become a new sortable object ("broken stuff → trash"). This gives designers two difficulty settings from one mechanic.

### Bounce
- **What it does:** Bouncy objects (rubber balls, jelly, frogs) have high restitution and can bounce **out** of an open container.
- **Gameplay it creates:** Place bouncy objects *after* the bin has some contents (dampening) or drop them gently; use lids; or deliberately bank-shot them off walls into bins (expert play, viral clips).

### Rolling
- **What it does:** Round objects roll on sloped surfaces and off the edge of the Mess Zone onto the floor (still reachable — the floor is a "spillage" area in front of the containers).
- **Gameplay it creates:** Urgency without a timer: the pile disperses if ignored; rolling objects can roll into the *wrong* open container (counts as a mistake only if it was the player's throw, not if the environment rolled it — that just spits back, no Oops). Fairness rule: **environmental accidents never cost Oops.**

### Balance / tipping
- **What it does:** Some containers (tall vases, see-saw bins, buckets on a beam) tip if loaded unevenly. A tipped container spills its contents back into the play area.
- **Gameplay it creates:** Load-balancing: alternate sides of a see-saw, put heavy items in the middle.

### Environment tilt
- **What it does:** The whole scene rotates ±4–12° on a slow, readable wave (ship rocking, earthquake pulses, a giant's hand tilting the table). Telegraphed by a 1-second creak sound and a wave icon in the HUD.
- **Gameplay it creates:** Timing — throw on the downswing, place fragile objects at the still point of the wave. Turns every other mechanic into a harder version of itself without a new rule.

## 2.4 Secondary physics (summary)

| Mechanic | Behaviour | Gameplay |
|---|---|---|
| **Sticky** | Contact joint forms with any object it touches; joint breaks if dragged with a sharp shake (velocity reversal > 2,000 dp/s) | Untangling; "sort the clump" decisions |
| **Magnetism** | Metal-tagged objects within 120 dp are pulled toward a magnet object | Tool use: drag the magnet through the pile to collect all metal, drop the cluster in one go |
| **Explosion** | Radial impulse, radius 150–250 dp | Clears/scatters the pile; also breaks fragile objects. Always telegraphed with a fuse |
| **Floating** | Negative gravity scale; drifts upward and out of open containers | Must be weighed down (put something heavy on top) or put in a lidded container |

## 2.5 The rules of "controlled chaos"

These are non-negotiable constraints for every physics behaviour.

1. **Nothing is ever lost.** Invisible walls on screen edges; the floor in front of the containers is a reachable spill area. Objects that would leave the screen are respawned at the top with a "poof" (and that is never a penalty).
2. **Counted on entry.** An object counts as sorted the moment it crosses the mouth sensor, not when it settles. Once counted, it can only be un-counted by a *visible* event (a bounce-out, a tip, an explosion) — never by jitter.
3. **Environment accidents are free.** If the environment (tilt, roll, explosion) pushes an object into the wrong container, it is spat back with **no Oops cost**. Only player placements cost Oops.
4. **Telegraph everything over 0.5 s.** Any event that will move the board (tilt, quake, container swap, explosion) gets ≥ 1.0 s warning: sound + visual tell + HUD icon.
5. **Determinism.** Fixed timestep (60 Hz), seeded RNG, no frame-rate-dependent behaviour. The same inputs produce the same outcome — required for replays, ghosts, solver validation and daily leaderboards.
6. **Settle quickly.** Physics piles must reach rest within 1.2 s of a disturbance (high angular damping, sleep thresholds tuned). Jittering piles look broken.
7. **Physics difficulty is capped per tier** (see [Ch. 04 difficulty budget](04-progression-difficulty.md#44-difficulty-budget)). No more than **one environmental physics threat** active at a time below Chaos tier.
8. **Every fail has a replay.** The last 3 seconds before a physics fail are shown in slow-motion on the fail screen, so the cause is always understood.

## 2.6 How physics transforms the game — the escalation ladder

Each rung is a different *feeling* built from the same drag-and-drop:

| Rung | Player feeling | Example |
|---|---|---|
| Juice | "That's satisfying." | Bins fill with a clatter of candy |
| Constraint | "I need to think about *how* I put things in." | 10 kg weight limit; anvil vs. feathers |
| Care | "Gently… gently…" | Eggs into a crate while a timer runs |
| Skill | "Did you see that throw?" | Bank-shotting a bouncy frog into a jar |
| Tool | "Oh! I can use the magnet *for* me." | Sweeping a magnet through a junk pile |
| Weather | "The whole world is against me." | Sorting fragile potions on a rocking ship |
| Chaos | "WHAT JUST HAPPENED." | A bomb in the pile, sticky slime, ship rocking — and it's still fair |

## 2.7 Technical approach (summary — full detail in Ch. 10)

**Decision: Unity Physics 2D (Box2D) with 3D-rendered visuals**, not 3D physics.

| | 2D physics + 3D visuals | 3D physics |
|---|---|---|
| Readability | Excellent (side view, no depth ambiguity) | Objects hide behind each other |
| Determinism | Achievable with fixed step + custom solver settings | Harder (PhysX non-determinism across devices) |
| Performance on low-end Android | 60 objects at 60 fps comfortable | 60 rigidbodies + rendering is risky |
| Look | Toy-like 3D meshes with orthographic camera and slight 8° tilt for depth | Richer but noisier |

Objects use simplified convex compound colliders (≤ 4 shapes per object). Visual mesh may be more detailed than the collider; colliders are authored or auto-generated and then reviewed.
