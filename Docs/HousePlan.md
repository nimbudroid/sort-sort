# House-first direction: status

This tracks the House-first amendment against what is built.

## Built: the two representative rooms the GDD asks to validate first

**Room rules (data).** Every room after the Kitchen has three chapters and two formal milestones with unique IDs and explicit prerequisite scenes.
- **Chapter recognition** (`chapter:<area>:<n>`): granted when the chapter's ordinary scenes are sorted and any planning puzzle in it is solved or left for later.
- **Milestones:** the midpoint after Chapter 2, and the final one with the room.
- **Area Ready** (`area_ready:<area>`): granted when every ordinary scene is sorted.
- **What doesn't count:** planning puzzles never count toward milestones or Area Ready, and replays, coins and cosmetics never grant anything.
- **Existing saves** catch up at boot.

**Living Room: 8 scenes (3 + 3 + 2)**, opens after Kitchen 5.

| Chapter | Scenes |
|---|---|
| 1 · Clear the Space | 1 Tidy the sofa; 2 Clear the coffee table; 3 Share the storage bench (planning) |
| 2 · Make It Cosy | 4 Style the bookshelf; 5 Make it cosy (composed); 6 Sort the game shelf → midpoint milestone |
| 3 · Family Time | 7 Set up family game night; 8 Finish the reading corner (composed) → final milestone + Living Room ready |

**Dining Room: 6 scenes (2 + 2 + 2)**, opens after Kitchen 10.

| Chapter | Scenes |
|---|---|
| 1 · Clear the Table | 1 Clear the dining table; 2 Sort the sideboard |
| 2 · Set the Places | 3 Share the sideboard shelf (planning); 4 Fill the sideboard → midpoint milestone |
| 3 · Dinner Is Served | 5 Prepare the centrepiece; 6 Dinner is served (composed two-place setting) → final milestone + Dining Room ready |

**New objects:** 8 modeled 3D objects (board game, jigsaw, cards, glass, water jug, cloth napkin, placemat, bud vase) and three categories (Games & puzzles, Glasses & jugs, Table linens). The Dining Room reuses the Kitchen's plates, mugs and cutlery.

**The Dining Room's look:** terracotta wallpaper, a walnut table chosen for contrast, a sideboard, a pendant lamp, a glass cabinet and a linen basket.

**Unchanged:** the Kitchen (20 scenes / 21 boards), its IDs, rules, saves, economy and four milestones.

**Next:** play these two rooms with fresh players before producing the other 7 rooms (GDD §11).

## Decisions needed

1. **Coins for new rooms.** Living Room scenes grant **0 coins**. The GDD says later milestones need a separately authored economy, and the amendment forbids new currencies. Choose a per-scene coin amount, or keep 0.
2. **When rooms open.** Living Room opens after Kitchen 5. The amendment says "meaningful progress"; confirm this, or choose another milestone.
3. **Lessons in new rooms.** The planning lesson (shared space) still demonstrates with kitchen produce. A room-themed demo could follow.
4. **Milestone thresholds:** room milestones are recorded but unlock nothing. GDD §7 asks for the global threshold and destination balance to be reviewed separately.
5. **Not yet:** room keepsakes and the House Ready finale (needs all ten rooms).

## Planned (from the amendment), not built

- Bedroom, Kids' Room, Home Garden (8 scenes each); Attic Study (`house_attic_study`), Bathroom, Laundry Room, Garage (6 each).
- New task types beyond Sort and Spatial planning: Arrange, Match, Group, Fold and stack, Complete a set. Each is a new rule shape in the existing evaluator and needs approval.
- House finale; optional systems A–E (collections, little projects, mastery, personal touches, return experience).
