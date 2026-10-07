# 07 — Retention, Meta, Rewards & Monetization

[← Index](README.md)

---

## 7.1 The "one more level" loop

```
        ┌──────────────────────────────────────────────────────────────┐
        ▼                                                              │
   PLAY (≤1.5 s to first drag)                                         │
        │                                                              │
   SORT ── combo notes climb ── twist hits ── "oh no" ── recover       │
        │                                                              │
   COMPLETE ── lids close in a melody ── "SORTED!" stamp               │
        │                                                              │
   REWARD ── stars, coins (combo-multiplied)                           │
        │                                                              │
   UNLOCK ── every 5th: Museum object · every 15th: chapter skin       │
        │                                                              │
   SEE NEXT ── "NEXT UP: A Vampire's Fridge" (silhouette teaser) ──────┘
                 ▲
                 └── the curiosity gap: the single strongest hook
```

### Why players continue (the five forces, in priority order)

| # | Force | Mechanism | Owner system |
|---|---|---|---|
| 1 | **Curiosity** | The teaser card names the next absurd thing to sort. Players want to see "what's a dragon's tax return look like?" | Content variety, teaser card |
| 2 | **Completion urge** | Mess → order. The room is cleaned; the bins are shut. The Zeigarnik effect works on the *next* mess too | Core loop |
| 3 | **Low cost of continuing** | A level is 30–60 s; NEXT is under your thumb; no lives, no waits | UX, no energy system |
| 4 | **Near-miss** | "1 object away!" — retry is instant and free | Fail loop |
| 5 | **Collection & progress** | Museum shelves fill; chapter progress bar; level number climbs | Meta |

The economy is deliberately **force #5**, not #1. If the meta ever has to carry retention, the content is failing.

## 7.2 Progress signals

| System | Design | Launch? |
|---|---|---|
| **Level numbering** | Big, single, ever-increasing number ("LEVEL 137"). No world resets. Chapter shown as a subtitle | ✅ |
| **Difficulty labels** | Normal (no badge), 🔥 Hard, 💀 Super Hard, 🧹 Big Mess, 🌀 Chaos. Shown on the level button and the teaser card. Hard+ levels give +50% coins | ✅ |
| **Stars (3)** | ★ Complete · ★ No mistakes (0 Oops) · ★ Under par time. Stars are *bragging*, not gates — no star gates on progression | ✅ |
| **Score** | Only in Daily Sort and events (time + mistakes + combo). Campaign uses stars, not score — score adds noise to the core screen | Daily only |
| **Coins** | Soft currency (see economy) | ✅ |
| **Chapter progress bar** | 15 pips on the level map; chapter reward visible at the end | ✅ |
| **Streaks** | **Hot Hands** win streak (see §7.5) + Daily Sort streak calendar | ✅ (Hot Hands), Daily streak at soft launch |
| **Achievements** | ~40 "Sorting Badges" (e.g. "Gentle Giant: sort 100 fragile objects without breaking one") | Month 1 |
| **Milestones** | L25, 50, 100, 200, 300, 400 — special level + big reward + shareable card | ✅ |

## 7.3 Meta layer: The Museum of Sorted Things

**Fantasy:** You run a ridiculous museum of every kind of object you've ever sorted. And — the joke — **the museum itself is a sorting puzzle.**

### How it works
- Every time you sort a *new* object type in the campaign, it's added to your **Museum crate** (inbox).
- Opening the Museum shows shelves organised by theme ("The Kitchen Wing", "The Haunted Hall", "The Space Annex"). Each shelf has labelled silhouette slots.
- The player **drags the new objects from the crate onto the correct shelf slots** — a calm, zero-pressure, 10-second sorting moment (no fail, no timer). It uses the core verb and the same satisfying gulp/clack feedback.
- Each object has a **joke museum label** when you tap it ("**Left Sock (Lonely).** Last seen 2019. Partner presumed in a parallel dimension.").
- **Completing a shelf** (6–10 objects) grants: a container skin, a coin bundle, and the shelf "lights up" (a little animated diorama).
- **Completing a wing** (5–8 shelves) unlocks a **themed container skin set** and a Museum decoration.
- **Rare variants** (golden, glitched, tiny, giant) of objects appear 1-in-40 in campaign levels; sorting one sends it to the Museum's "Oddities" room. This makes every level a lottery ticket without adding RNG to gameplay.

### Why the Museum (and not alternatives)

| Meta option | Production cost | Reinforces core? | Risk | Decision |
|---|---|---|---|---|
| **Museum (collection + mini-sort)** | Low (reuses object art) | **Yes — it is sorting; collecting drives curiosity about new objects** | Could feel shallow | **Launch** |
| Home decoration (Homescapes-style) | High (new art pipeline, narrative) | No (different fantasy) | Distracts from core | Reject |
| Character upgrades / power progression | Medium | Breaks puzzle fairness | Pay-to-win perception | Reject |
| Sorting Facility tycoon (idle) | High | Partially | Idle meta pulls attention from levels | Test post-launch only if D30 is weak |
| Pure level map | Very low | Neutral | Weak D7+ | Insufficient alone |

**KPI hypothesis:** The Museum raises D7 retention by ≥ 1.5 points and session count per day by ≥ 0.2 versus a no-meta control (test in soft launch, Ch. 11 experiment E5).

## 7.4 Rewards

### Per-level rewards

| Achievement | Reward | Feel |
|---|---|---|
| Complete | 20 coins (Normal), 30 (🔥), 40 (💀), 60 (🧹) | Coins burst from the bins |
| ★ No mistakes | +10 coins | "PERFECT!" stamp variant |
| ★ Under par | +10 coins | Stopwatch dings |
| Combo | Multiplier on base coins (×1.0 to ×1.5 at max combo ×5+) | Coin pitch climbs |
| "Nice save" (catching a falling fragile object, bank shots, etc.) | +5 each, max 3/level | Pop-up toast |
| Rare variant sorted | Museum Oddity + 25 coins | Sparkle and a unique chime |
| Rewarded ad ×2 on result screen | Doubles base coins | Optional, never pre-selected |

### Periodic rewards

| Trigger | Reward |
|---|---|
| Every 5 levels | 1 Museum object reveal + small booster (alternating Magnifier / Undo / Freeze) |
| Chapter complete (15) | Container skin + 150 coins + 1 Skip ticket |
| Milestone (L25/50/100/200/…) | Exclusive skin set + 500 coins + shareable milestone card |
| Hot Hands streak 3/5/10 | See §7.5 |
| Daily Sort completed | 50 coins + streak progress |
| Daily Sort 7-day streak | Mystery crate (guaranteed rare Museum oddity + cosmetic) |
| Weekly Challenge (Month 1+) | Top-tier container skin |

### Mystery rewards
- **Mystery Crate** — appears on the result screen 1 in every ~8 levels (pity timer: guaranteed by 12). Opening it is a 2-second physical moment: the crate is a *container that spits out its contents* — coins, a booster, or a cosmetic. Never contains gameplay-affecting items beyond basic boosters. Option to open a second crate with a rewarded ad.

## 7.5 Streaks: "Hot Hands"

- Win levels on the **first attempt** in a row to build a streak (the hands icon on the level button glows hotter).
- Streak 3: +25% coins per level. Streak 5: +50% and a free Undo at the start of each level. Streak 10: "On Fire" — flaming trail cosmetic on dragged objects for the streak's duration + 100% coins.
- **A loss resets the streak.** But the player can **protect** it once per streak with a rewarded ad ("Keep your streak?") — high-value rewarded placement with real player motivation.
- Hot Hands doesn't apply to Daily Sort (separate streak).

**Why:** Gives the first-try attempt emotional weight without punishing a loss beyond the streak; drives the "careful, I'm on a streak" feeling and a natural rewarded-ad moment. Streak bonuses are cosmetic/economic only; they never make levels easier except the single Undo at streak 5.

## 7.6 Economy

**Currencies at launch:** **Coins only.** No premium currency at launch.

| Source | Approx. coins/day for a median D7 player (25 levels/day) |
|---|---|
| Level completion + stars + combo | ~900 |
| Mystery crates | ~150 |
| Daily Sort | 50 |
| Rewarded ad doublers (if used) | ~200–400 |
| **Total** | **~1,100–1,500** |

| Sink | Price |
|---|---|
| Booster: Magnifier (reveal disguises & hidden rules for 10 s) | 150 |
| Booster: Undo (take back last placement, refunds Oops) | 100 |
| Booster: Freeze (pause timer & all clocks 8 s) | 200 |
| Booster: Auto-Sort (3 objects sorted correctly) | 300 |
| Continue after fail (+15 s / +1 Oops / remove the bomb) | 300 coins **or** rewarded ad (first continue per level is ad or coins; the second is coins only, 600) |
| Skip level | 1 Skip ticket or a rewarded ad |
| Container skins (shop rotation) | 1,500–4,000 |
| Object trail effects | 2,000 |
| Background themes for the Museum | 3,000–5,000 |

**Design intent:**
- A median player can afford ~1 booster per 3 levels from free income. Boosters are a *convenience* and a *comeback tool*, not a requirement — every level must be validated as solvable without boosters (V2 runs without boosters).
- Cosmetics are the long-term sink; they take 1–3 days of free income each — aspirational but reachable.
- **Boosters are disabled in Daily Sort leaderboard runs** (practice runs allow them).

**Premium currency** ("Gems") is deliberately deferred: if month 2–3 data shows IAP conversion below 1.5% and players hitting the coin ceiling, introduce gems tied to the season pass only.

## 7.7 Monetization

### Principles
1. **Never interrupt sorting.** No ad, offer, or popup during a level.
2. **Never between fail and retry.** That's the moment of highest motivation; an ad there converts frustration into churn.
3. **Offer value at moments of desire,** not moments of pain: after a near-miss, after a great streak, when a new cosmetic is seen.
4. **Ad-first, IAP-deepening.** Expect ~65–75% ad revenue for the first 6 months.

### Placements

| Placement | Type | Rules |
|---|---|---|
| Post-level | **Interstitial** | Not before **L12 and 8 minutes of first-session playtime**. Then: max 1 every 3 levels *and* ≥ 90 s since the last one (remote-configurable). Skipped after a 🔥/💀/🧹 win (reward the achievement) and after a level that took > 120 s. Never on the first level of a new session |
| Result screen | **Rewarded** ×2 coins | Optional button, never pre-selected, never where NEXT is |
| Fail screen | **Rewarded** continue | Once per attempt |
| Hot Hands | **Rewarded** streak protect | Once per streak |
| Mystery crate | **Rewarded** second crate | |
| Skip level | **Rewarded** | After 6 fails |
| Shop | **Rewarded** free daily coin bag | 1/day |
| Banner | **None** | Banners eat the bottom of the screen = the container row (thumb zone). Not worth the eCPM |

### IAP catalogue (launch)

| Product | Price (USD) | Contents | When it's surfaced |
|---|---|---|---|
| **Remove Ads** | $4.99 | Removes interstitials forever; rewarded ads stay optional; +1,000 coins bonus | Shop always; offered once after the 3rd interstitial and again at L50 |
| **Starter Pack** | $1.99 (one-time) | 3 of each booster, 1,500 coins, exclusive "Rubber Duck" container skin set | After L15, 48 h availability |
| **Remove Ads + Starter Bundle** | $6.99 | Both (save ~$1) | On first Remove Ads view |
| Coin packs | $0.99 / $4.99 / $9.99 / $19.99 | 1,000 / 6,000 / 14,000 / 32,000 | Shop |
| Cosmetic bundles | $2.99–$5.99 | Themed container sets + trails | Shop rotation, event-tied |
| **Sort Pass** (Month 2+) | $4.99 / season (4 weeks) | Premium track: exclusive skins, Museum backgrounds, boosters, coins | Season start + mid-season |

### Monetization strategy by player age

| | **Day 1** | **Day 7** | **Day 30** |
|---|---|---|---|
| **Goal** | Fall in love. Retention > revenue | Habit + first purchase | Long-term value, collection & status |
| **Interstitials** | None until L12 / 8 min; then 1 per 3 levels | 1 per 3 levels (A/B: 1 per 2) | 1 per 2–3 levels; ad-intolerant segment (fast churn after ads) → back off to 1 per 4 |
| **Rewarded** | Introduced on fail screen at L16 (first continue is free once, to teach the button) | Full set; ~3–5 rewarded views/DAU | ~4–6/DAU; daily free bag in shop |
| **IAP surfaced** | Starter Pack after L15 (one time, dismissible); nothing else pushed | Remove Ads offer after the 3rd interstitial in a session; first cosmetic offer tied to Museum shelf completion | Sort Pass; event cosmetic bundles; coin packs for booster-heavy players (segmented) |
| **Expected ARPDAU** | $0.04–0.07 | $0.10–0.14 | $0.14–0.20 |
| **Watch metric** | D1 retention by interstitial exposure cohort | Remove Ads conversion (target 2–4% of D7 retained) | Pass conversion (target 3–5% of D30 active) |

### Segmentation (remote config)
| Segment | Signal | Treatment |
|---|---|---|
| **Ad-sensitive** | Session ends within 10 s of an interstitial ≥ 2× | Reduce interstitial frequency 40%, surface Remove Ads |
| **Rewarded enthusiasts** | ≥ 6 rewarded/day | Add an extra rewarded placement (pre-level free booster) |
| **Payers** | Any IAP | Never show the Starter Pack again; cosmetic-forward offers; interstitials off if Remove Ads |
| **Struggling** | 3+ fails on the same level twice in 5 levels | Soft difficulty assist; booster discount offer; no Remove Ads push |

## 7.8 What we deliberately don't do

| Pattern | Why not |
|---|---|
| Lives / energy | Breaks "one more level" |
| Spin wheels on every level | Slows the loop; feels manipulative |
| Pay-to-skip as the main IAP | Implies levels are unfair |
| Star-gated progression | Punishes casual players |
| Fake "near-miss" rigging | Players notice; trust collapses. Near-misses are real or not at all |
| Interstitials after fails | Highest-churn moment |
| Loot boxes for money | Regulatory risk and brand risk; mystery crates are earned only |
