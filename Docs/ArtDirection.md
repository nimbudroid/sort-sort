# Art direction: Premium Cozy Stylized 3D

## Current object art: modeled 3D props, baked to sprites

The 18 kitchen objects are modeled as real 3D props in code, so their geometry and materials are designed rather than painted on. They are baked into the game's existing object sprites, which means the runtime keeps one 2D pipeline and pays nothing for 3D.

- **Models:** `Scripts/Editor/Art3D/ObjectModels.cs`, built with `MeshKit` (turned profiles, swept tubes, sculpted organic forms, beveled boxes, leaves, embossed symbols). Details are modeled, not painted: bottle cap ribs and neck ring, carton gable, fin and spout, an embossed orange slice, can lid and tab, ceramic rims, fruit stems, leaves and seeds.
- **Materials:** one shared stylized shader (`Art3D/BakeStylized.shader`) with per-family settings:
  - produce: matte or glossy, with light wrap and organic tone variation;
  - ceramic: glazed, tight highlight;
  - paper and cardboard: matte;
  - plastic: soft highlight;
  - clear plastic: real transparency with the water visible inside;
  - painted metal and polished metal: reflections of a warm room;
  - wood: grain tone.
- **Lighting:** one soft top-left key light, a gentle fill, warm core shadows (never grey), baked ambient occlusion, controlled highlights and a soft rim. There is no bloom and no glare spots. Contact shadows are added in-game by `ObjectView`, so the sprites carry none.
- **Camera:** one shared orthographic, slightly elevated three-quarter view. Every prop is framed at 84% of its square.
- **Outline:** a thin warm-brown outline, matching the containers and props.
- **Bake:** **Sort Everything ▸ Bake 3D Object Art** writes `Resources/SortEverything/Art/<id>.png` at 512×512. These sprites replace the code-drawn versions automatically. Command line:
  `-executeMethod SortEverything.Editor.Art3D.ArtBaker.BakeCommandLine [-only apple,mug]`. Run it without `-nographics`, and add `--burst-disable-compilation` on this Mac.
- **Review:** check `Docs/Screenshots/art3d/`, which has the props at a large size and at gameplay size on the counter. Judge each prop against the brief's acceptance questions at real size.

Object IDs, categories, `relativeSize` and other metadata are unchanged. To swap in hand-made art, overwrite `Art/<id>.png`.

---

## Earlier 2.5D direction (kept for reference)

# Art direction: Cozy Playful 2.5D

The 18 kitchen objects follow this direction in two ways:

1. **In code (now).** `SpriteFactory` draws every object in this style, with these settings:
   - Rounded volumes lit from the upper-left.
   - Warm shadows and soft ambient occlusion.
   - Material highlights: glossy fruit, matte vegetables, plastic bottles, metal utensils, ceramic.
   - Thin warm-brown outlines and three-quarter views.
   - 256×256 sprites.

   You can preview them with **Sort Everything ▸ Asset Gallery**.
2. **AI-generated or hand-painted art (optional, replaces the code version).** Generate an image with the prompt below, then save it as `Assets/SortEverything/Resources/SortEverything/Art/<assetId>.png`. It imports as a sprite automatically and the game uses it in place of the drawn one; no code changes are needed. **Sort Everything ▸ Open Final Art Folder** opens that folder.

## Workflow for generated art

1. Run **Sort Everything ▸ Export Placeholder Art**. It writes the current drawings to `Assets/SortEverything/Art/PlaceholderExport/`.
2. Give your image tool the placeholder PNG as the reference image, plus the **Base prompt** and that object's line from the table below.
3. Export the result as a square PNG, at least 512×512, with a transparent background and the object centered, leaving about 8% padding around it.
4. Save it as `Resources/SortEverything/Art/<assetId>.png`, for example `apple.png`.
5. Check it in **Asset Gallery** and in game at real size: 44 units, about the width of a fingertip. It has to stay recognizable that small.

Rules from the GDD that apply to every object:
- No text, labels or branding baked into the art.
- Drinks keep a readable symbol: a water drop, a milk drop, an orange slice, or bubbles.
- The mug is shown empty.
- The objects share one consistent scale and lighting so they look right together.

## Base prompt

> Redesign the provided game asset into a polished, high-quality Cozy Playful 2.5D casual mobile game art style. Charming handcrafted cartoon aesthetic with dimensional shapes, soft shading, warm colors and playful proportions, like a premium casual mobile puzzle game. Soft, rounded, slightly exaggerated, chunky silhouettes that stay instantly recognizable, with subtle handcrafted asymmetry and believable volume. Soft 2.5D painterly rendering with subtle gradients, layered shading, gentle material highlights, soft ambient occlusion and a subtle contact shadow. Consistent soft lighting from the upper-left, warm highlights, vibrant but slightly warm harmonious palette (never neon). Subtle, soft, consistent dark-brown outlines only where needed. Three-quarter view from a slightly elevated viewpoint. Single centered isolated asset, fully visible, transparent background, no environment, props, text, labels or watermark. Readable at small mobile UI sizes. Keep the object's identity exactly as in the reference.

## Per-object lines (append to the base prompt)

| assetId | Category | Object, material and key features |
|---|---|---|
| apple | Fruit | Red apple, glossy skin, short brown stem, one green leaf |
| banana | Fruit | Single curved yellow banana, smooth satin peel, brown stem and tip |
| orange | Fruit | Round orange, dimpled matte-satin peel, small green leaf |
| strawberry | Fruit | Red strawberry, glossy, yellow seeds, green leafy crown |
| carrot | Vegetables | Tapered orange carrot, matte, faint ridges, green leafy top |
| pepper | Vegetables | Red bell pepper, very glossy, lobed shape, green stem |
| broccoli | Vegetables | Broccoli head, matte green florets, pale green stalk |
| corn | Vegetables | Corn cob, yellow kernels, partial green husk at the base |
| water_bottle | Drinks | Clear plastic water bottle, blue cap, blue water-drop symbol (no text) |
| milk_carton | Drinks | White gable-top milk carton, blue band with a white milk-drop symbol (no text) |
| juice_carton | Drinks | Orange juice box with pink straw, orange-slice symbol (no text) |
| soda_can | Drinks | Short red soda can, metal lid and tab, white bubble symbol (no text) |
| spoon | Kitchenware | Polished steel spoon, chunky handle, reflective metal |
| fork | Kitchenware | Polished steel fork, four chunky tines, reflective metal |
| plate | Kitchenware | Round ceramic plate, cream glaze, blue rim line, empty |
| mug | Kitchenware | Empty blue ceramic mug with a large handle, dark empty interior visible |
| spatula | Kitchenware | Slotted dark spatula head with wooden handle and a metal collar |
| pan | Kitchenware | Shallow dark non-stick frying pan with a long handle, seen from three-quarter |

Target and environment art (baskets, crate, drawer, tray, counter, shelves, room corners) follows the same direction. Its code versions live in `KitchenArt.cs`.
