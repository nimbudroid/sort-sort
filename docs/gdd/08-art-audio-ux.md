# 08 — Visual Style, Audio & UX/UI

[← Index](README.md)

---

## 8.1 Visual style: "Toybox Diorama"

### The look in one sentence
**Chunky, soft-matte vinyl toys on a clean pastel stage, with googly-eyed bins that have opinions.**

### Visual pillars

| Pillar | Rule | Example |
|---|---|---|
| **Silhouette first** | Every object must be identifiable as a solid black silhouette at 56 dp | A sock reads as a sock even in pure black |
| **Sticker outline** | 3–4 px dark outline (darkened object colour, not black) around every interactive object | Objects pop off any background |
| **Saturated objects, quiet stage** | Objects: high saturation, high value contrast. Backgrounds: desaturated pastel with one theme hue | Candy-pink objects on a soft mint kitchen |
| **Toy proportions** | Everything is slightly chubby and rounded: corners rounded at ≥ 15% of size, proportions 1.2× wider than reality | An anvil looks like a toy anvil |
| **Faces only on containers (and living things)** | Objects don't have faces unless they're alive; containers always do | Instantly separates "things" from "places" |
| **Material = colour-free cue** | Glass sparkles, metal has hard specular, fabric has a soft fuzz rim, goo is glossy and wobbly | Players read material (and thus behaviour) without colour |
| **Colour-blind safe** | Every colour category also has a pattern (stripes, dots, zigzag, solid, checker) visible on the object and the rule chip | Mandatory (validator V4) |

### Rendering approach
- 3D models, low-poly (300–1,500 tris per object), toon-ish shading with a single soft key light and a strong rim light, rendered through an orthographic camera at an 8° downward tilt.
- Physics is 2D; visuals are 3D — objects can rotate around their Z axis only (with optional cosmetic wobble on X/Y).
- Background: layered 2.5D diorama with gentle parallax on device tilt (cosmetic, off by default for motion sensitivity).

### Theme palettes
Each theme is defined by a **stage hue**, an **accent hue**, and an **object palette constraint** (theme objects stay inside 6 hues + neutrals). Example:

| Theme | Stage | Accent | Object hues |
|---|---|---|---|
| Kitchen | Mint #CDEBDD | Tomato #F2563A | Red, yellow, cream, green, brown, steel |
| Haunted House | Dusky lilac #4A3F6B | Ghost green #9CFFB0 | Purple, bone, black-cherry, pumpkin, mist, gold |
| Space | Navy #1E2A55 | Neon cyan #3FE6F5 | Planet colours, chrome, white, magenta |
| Pirate Ship | Sandy #F2DDB0 | Ocean teal #1E9AA0 | Gold, wood, rum-brown, red, white, black |

### Motion language (numbers for animators)

| Action | Animation |
|---|---|
| Pickup | Scale 1.0 → 1.15 (80 ms, ease-out-back), shadow fades in, slight tilt toward drag direction (max 12°) |
| Drag | Follows with spring; heavy objects sag and swing; light ones flutter |
| Drop into container | Object squash (1.2, 0.8) on contact 60 ms → spring back; container gulp (1.12, 0.88) 180 ms |
| Correct | 6–10 particles in container colour; tiny "+1" chip pop |
| Wrong | Container face disgust (100 ms), spit arc 450 ms, red outline pulse ×2 |
| Container full | Lid snaps shut 120 ms, container hops 8 dp, sparkle ring |
| Level complete | Lid ripple (80 ms stagger), "SORTED!" stamp (scale 2.0 → 1.0 with 4 px shake), confetti in theme colours |
| Break | Shatter into 4–8 pre-cut fragments, 3 frames hit-stop (50 ms) |
| Explosion | 6 frames hit-stop, white flash 1 frame (respecting photosensitivity setting), radial ring, objects fly |

**Screen shake budget:** max 6 px, max 200 ms, max one shake per second. Off in Reduce Motion mode.

### Container characters ("Binbuddies")
- 5 base expressions: idle-blink, curious (watching dragged object), happy-gulp, disgusted-spit, worried (when near weight limit or a fuse is near).
- Containers **look at** the object being dragged (eyes track). This alone makes the screen feel alive.
- Skins change the container's body and face style (e.g. "Robot", "Frog", "Treasure Chest", "Cardboard Box with a Tiny Moustache").

### What makes screenshots recognisable
1. Googly-eyed bins in a row at the bottom.
2. A chaotic, colourful pile above.
3. One "wrong" absurd object in the middle of the pile (a radioactive banana, a moustached fruit).
4. The giant red "SORTED!" stamp on completion.

These four elements appear in every store screenshot and every UA video's first frame.

## 8.2 Audio

### Direction
**Tactile, toy-like, musical.** Every sound is either a *material* sound (what the object is made of) or a *musical* sound (part of the level's song). No generic UI blips.

### The "Sort Melody" system (signature)
- Each level has a short melody (8–16 notes) in a pentatonic scale, generated from the theme's music key.
- **Every correct sort plays the next note** of the melody on the theme's lead instrument (marimba for kitchen, theremin for haunted, steel drum for pirate…).
- Combo raises the octave/adds harmony. A fast, flawless run plays the full melody in rhythm — the level *sings back* to a good player.
- Level complete plays the **cadence**: the melody's final phrase on the lid ripple.
- Because the scale is pentatonic, any sorting rhythm sounds musical; there are no "wrong notes".

### Sound library

| Category | Sounds | Notes |
|---|---|---|
| **Pickup** | Soft "pop" by material (plastic pop, glass tink, metal clink, fabric fwump, goo squelch) | ≤ 80 ms, pitch varies ±5% |
| **Drag** | Subtle movement whoosh for fast drags; heavy objects creak | Throttled |
| **Correct placement** | Melody note + material landing sound + container "gulp" | Three layers, mixed so the note dominates |
| **Wrong placement** | Container "blegh" → "PTOO!" spit → comedic "bwomp" (descending tuba) | Funny, never harsh |
| **Combo** | Each step adds a shaker layer; ×5 adds a choir "ooh" | |
| **Container full** | Lid "clack" + bell ding | Very satisfying, slightly louder |
| **Completion** | Cadence + stamp "THUNK" + crowd "yay" (tiny toy crowd) | 1.5 s |
| **Failure** | Short comedic sting per fail type (sad trombone for Oops, alarm-clock for time, crunch for weight, glass for break, boom for explosion) | ≤ 1.2 s, never a "game over" drone |
| **Physics** | Impacts by material pair and impulse (soft/medium/hard), rolling loops, bounces with rising "boing", tipping creak | Impulse-scaled volume; voice-limited to 8 simultaneous impacts |
| **Special objects** | Geiger clicks (radioactive, faster near objects), fuse hiss + tick, ice crackle on tap, sizzle (hot), snoring (sleeping), skitter (shy), drip (slippery), ghostly whoosh on phase, mimic tongue-slurp | Each has a unique, recognisable **audio tell** that works with the screen off — accessibility and readability |
| **Environment** | Ship creak 1 s before tilt; rumble 1 s before quake; flicker buzz before lights out | Telegraphs (fairness rule) |

### Music
- **Style:** Bouncy lo-fi toy-pop: marimba, pizzicato, kalimba, upright bass, soft drums, with per-theme lead instruments.
- **Adaptive layers:** Base loop (calm) → +percussion when a timer is under 15 s or a fuse is lit → +brass stabs in Chaos tier. Music ducks 4 dB under Sort Melody notes.
- **Near-miss:** Last object + low time → music drops to a heartbeat kick.
- **Menus:** A single, very hummable theme ("The Sorting Song") — the audio logo appears in the app open and in every ad.
- **Haptics** (iOS Core Haptics / Android): light tick on correct, medium on wrong, heavy on fail, rhythmic pulses for the completion cadence.

## 8.3 UX flow

```
App open
  │
  ├─ (first launch) Consent (GDPR/CCPA where required, one screen) ──┐
  │                                                                  ▼
  │                                              LEVEL 1 starts immediately
  │                                              (no title screen, no menu, no login)
  │                                                                  │
  │                                              L1–10 tutorial chapter
  │                                                                  │
  │                          after L5: ATT prompt (iOS) with a friendly pre-prompt
  │                          after L10: Museum unlocked → mini-sort → Home screen revealed
  │
  └─ (returning) Home screen ── big PLAY (Level N) button
                    │
     ┌──────────────┼──────────────┬──────────────┬─────────────┐
     ▼              ▼              ▼              ▼             ▼
  Level map    Daily Sort     Museum          Shop        Settings
  (scroll)     (from L20)     (from L10)      (from L15)
```

### First launch (exact sequence)
1. Splash: the logo — a bin gulps the word "EVERYTHING" (1.5 s, also preloads L1).
2. Consent screen (only where legally required).
3. **Level 1 is on screen.** No name entry, no account, no "choose your avatar".
4. Home screen appears only after L10 (the player has been in "just one more" mode for 8 minutes). Before L10, NEXT always goes straight to the next level.

### Home screen
```
┌───────────────────────────────┐
│ 🪙 1,240          ⚙  🛒        │
│                                │
│   (animated: the next level's  │
│    pile and bins, waiting)     │
│                                │
│     NEXT UP: LEVEL 38          │
│    "Feed the Bin Monster"      │
│                                │
│      [   ▶  PLAY   ]           │  ← 70% width, thumb zone
│                                │
│ [📅 Daily] [🏛 Museum] [🗺 Map] │  ← red dot only when actionable
└───────────────────────────────┘
```
- The Home screen *is* the teaser: it shows the next level's pile, live, with bins blinking.
- Max one notification dot visible at a time (priority: Daily > Museum crate > Shop free gift).

### Level map
- Vertical scroll path, chapter banners every 15 levels, stars under each node, 🔥/💀/🧹 icons on nodes.
- Tapping a completed level replays it (for stars). No interstitials in replay.

### Daily Sort screen
- Today's puzzle card (title, difficulty, theme silhouette), countdown to the next one, streak calendar, your best time today, percentile, Leaderboard tab (Friends / Global / Country). Details in Ch. 09.

### Settings
Sound, Music, Haptics, Reduce Motion (no screen shake, no parallax, no flashes), Colour-blind patterns (on by default — they're part of the art), Left-handed mode (mirrors booster tray & secondary buttons), Language, Notifications, Restore Purchases, Privacy, Support, Credits.

### Rewards screen (level complete)
```
┌───────────────────────────────┐
│          SORTED!               │  ← stamp
│         ★   ★   ★              │
│    0:24   ✓ no mistakes        │
│    combo ×6 → 🪙 45            │
│                                │
│ ┌────────────────────────────┐ │
│ │ NEXT UP:                   │ │  ← teaser card (silhouettes)
│ │ "A Vampire's Fridge"  🔥   │ │
│ └────────────────────────────┘ │
│                                │
│  [🎬 ×2 coins]                 │  ← secondary, left
│        [   NEXT  ▶   ]         │  ← primary, centre-bottom
└───────────────────────────────┘
```

### Shop
Tabs: **Featured** (rotating cosmetic bundle + free daily bag), **Skins** (container sets), **Boosters**, **Coins**, **No Ads**. All cosmetics are previewed *on a live mini-level* (you can drag objects into the skinned containers) — the preview is itself satisfying.

### In-level pause menu
Resume · Restart · Settings · Home. No ads. No offers.

## 8.4 Accessibility checklist (launch)
- Patterns on all colour categories (mandatory).
- Reduce Motion mode.
- Photosensitivity: no full-screen flashes > 3 Hz; explosion flash optional.
- Audio tells for all special objects (helps low vision); visual tells for all audio cues (helps deaf/hard of hearing).
- Hit areas ≥ 56 dp; drop assist radius adjustable (Standard / Large).
- Timer extension option ("Relaxed timers": +50% time, disables 3rd star, excluded from leaderboards).
- One-handed play throughout; no required multi-touch.
