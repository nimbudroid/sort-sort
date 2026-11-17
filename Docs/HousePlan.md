# House-first direction: status

This tracks the House-first amendment against what is built.

## Built

- **House structure:** Home shows "Your House" with every room in order. Each room shows its saved overview, progress and actions; locked rooms say how they open. The scene list groups scenes by chapter.
- **Living Room, Chapter 1 "Clear the Space"** (five scenes):
  1. Tidy the sofa
  2. Clear the coffee table (mugs and plates go "back to the kitchen")
  3. Share the storage bench (optional planning puzzle with shared capacity)
  4. Style the bookshelf
  5. Make it cosy (composed finale: "The living room is cosy")
- **New content for the room:**
  - four categories: Books & paper, Soft things, Gadgets, Decor;
  - 12 modeled 3D objects;
  - room containers: blanket basket, bookshelf, media drawer, decor shelf, storage bench, back-to-kitchen tray;
  - a rug, sage wallpaper and living room props.
- **Opening the room:** the Living Room opens when Kitchen 5 (the breakfast corner milestone) is first completed. Existing saves that already qualify open it at boot. The result screen says "New room open".
- **Room overview from the save:** each region is untidy, then gains a trace per completed scene, then shows its composed restored state. Chapters that aren't authored yet say "Coming later".
- **House collection** grouped by room.
- **Unchanged:** the Kitchen (20 scenes / 21 boards), its IDs, rules, saves and economy.

## Decisions needed

1. **Coins for new rooms.** Living Room scenes grant **0 coins**. The GDD says later milestones need a separately authored economy, and the amendment forbids new currencies. Choose a per-scene coin amount, or keep 0.
2. **When rooms open.** Living Room opens after Kitchen 5. The amendment says "meaningful progress"; confirm this, or choose another milestone.
3. **Lessons in new rooms.** The planning lesson (shared space) still demonstrates with kitchen produce. A room-themed demo could follow.
4. **Room milestones and rewards** (Room Ready, keepsakes, House finale) are not implemented. They touch progression and save IDs, so they need agreement first.

## Planned (from the amendment), not built

- Living Room chapters 2–3 (Make It Cosy, Family Time).
- Bedroom, Kids' Room, Study, Home Garden (15 scenes each); Dining Room, Bathroom, Laundry Room, Garage (10 each).
- New task types beyond Sort and Spatial planning: Arrange, Match, Group, Fold and stack, Complete a set. Each is a new rule shape in the existing evaluator and needs approval.
- House finale; optional systems A–E (collections, little projects, mastery, personal touches, return experience).
