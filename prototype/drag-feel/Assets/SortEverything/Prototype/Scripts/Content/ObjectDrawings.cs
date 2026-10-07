using System;
using System.Collections.Generic;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Procedural vector drawings, one function per object id. Plain C# (no UnityEngine).
    /// Canvas is -1..1 (y up). The bin colour of an object is always its biggest, most obvious area.
    /// </summary>
    public static class ObjectDrawings
    {
        // Bin palette — identical to RoundDirector.Palette so objects match their bins exactly.
        public static readonly Rgba Red = Rgba.Hex("F2563A");
        public static readonly Rgba Blue = Rgba.Hex("3D7BF2");
        public static readonly Rgba Yellow = Rgba.Hex("F5C327");
        public static readonly Rgba Green = Rgba.Hex("43C46B");
        public static readonly Rgba Purple = Rgba.Hex("9B5DE5");

        // Neutrals and accents (kept small so they never compete with the bin colour).
        static readonly Rgba White = Rgba.Hex("F7F4EE");
        static readonly Rgba Cream = Rgba.Hex("F3E3C3");
        static readonly Rgba Tan = Rgba.Hex("D9A066");
        static readonly Rgba Brown = Rgba.Hex("8D5A3B");
        static readonly Rgba DarkBrown = Rgba.Hex("5E3B27");
        static readonly Rgba Gray = Rgba.Hex("B9BCC6");
        static readonly Rgba Steel = Rgba.Hex("9AA0AE");
        static readonly Rgba DarkGray = Rgba.Hex("5B5E6B");
        static readonly Rgba Black = Rgba.Hex("34313D");
        static readonly Rgba Pink = Rgba.Hex("FF9EC2");
        static readonly Rgba Orange = Rgba.Hex("F59A2E");
        static readonly Rgba LeafGreen = Rgba.Hex("5DBB4A");
        static readonly Rgba LightGreen = Rgba.Hex("B6E38C");
        static readonly Rgba DarkGreen = Rgba.Hex("2E8F4E");
        static readonly Rgba Glass = Rgba.Hex("BFE3F5");
        static readonly Rgba Screen = Rgba.Hex("3A4A6B");

        static readonly Dictionary<string, Action<VectorPainter>> drawings = new Dictionary<string, Action<VectorPainter>>
        {
            // Core: food
            { "apple", Apple }, { "banana", Banana }, { "strawberry", Strawberry }, { "watermelon_slice", WatermelonSlice },
            { "orange", OrangeFruit }, { "cupcake", Cupcake }, { "pizza_slice", PizzaSlice }, { "donut", Donut },
            { "popsicle", Popsicle }, { "ketchup", Ketchup },
            // Core: office
            { "stapler", Stapler }, { "pencil", Pencil }, { "pen", Pen }, { "notebook", Notebook },
            { "paper_clip", PaperClip }, { "coffee_mug", CoffeeMug }, { "scissors", Scissors },
            { "sticky_note", StickyNote }, { "calculator", Calculator }, { "eraser", Eraser },
            // Core: everyday
            { "key", Key }, { "sunglasses", Sunglasses }, { "toothbrush", Toothbrush }, { "sock", Sock },
            { "shoe", Shoe }, { "umbrella", Umbrella }, { "toy_ball", ToyBall }, { "alarm_clock", AlarmClock },
            { "small_plant", SmallPlant }, { "gift_box", GiftBox },
            // Extended: food
            { "burger", Burger }, { "fries", Fries }, { "cookie", Cookie }, { "ice_cream", IceCream }, { "egg", Egg },
            { "cheese", Cheese }, { "milk_carton", MilkCarton }, { "lemon", Lemon }, { "carrot", Carrot },
            { "broccoli", Broccoli }, { "avocado", Avocado }, { "watermelon", Watermelon }, { "bread", Bread },
            { "cereal_box", CerealBox }, { "jam_jar", JamJar },
            // Extended: office
            { "ruler", Ruler }, { "folder", Folder }, { "tape_dispenser", TapeDispenser }, { "hole_punch", HolePunch },
            { "keyboard", Keyboard }, { "computer_mouse", ComputerMouse }, { "headphones", Headphones },
            { "desk_lamp", DeskLamp }, { "paper_sheet", PaperSheet }, { "clipboard", Clipboard }, { "marker", Marker },
            { "highlighter", Highlighter },
            // Extended: home
            { "pillow", Pillow }, { "book", Book }, { "remote_control", RemoteControl }, { "wallet", Wallet },
            { "phone", Telephone }, { "hairbrush", Hairbrush }, { "shampoo", Shampoo }, { "soap", Soap },
            { "towel", Towel }, { "hanger", Hanger }, { "laundry_basket", LaundryBasket }, { "tissue_box", TissueBox },
            // Extended: toys
            { "teddy_bear", TeddyBear }, { "toy_car", ToyCar }, { "building_block", BuildingBlock },
            { "toy_train", ToyTrain }, { "rubber_duck", RubberDuck }, { "yoyo", YoYo }, { "toy_robot", ToyRobot },
            // Extended: electronics
            { "smartphone", Smartphone }, { "tablet", Tablet }, { "camera", CameraBody },
            { "game_controller", GameController }, { "smartwatch", Smartwatch }, { "speaker", Speaker },
            // Extended: nature
            { "flower", Flower }, { "leaf", Leaf }, { "mushroom", Mushroom }, { "rock", Rock }, { "pine_cone", PineCone },
            // Later: animals
            { "fish", Fish }, { "butterfly", Butterfly }, { "frog", Frog }, { "bird", Bird }, { "turtle", Turtle },
            { "cat", Cat }, { "dog", Dog },
            // Later: fantasy
            { "monster", Monster }, { "alien", Alien }, { "ufo", Ufo }, { "treasure_chest", TreasureChest },
            { "magic_wand", MagicWand }, { "dragon_egg", DragonEgg }, { "wizard_hat", WizardHat },
            { "dinosaur_bone", DinosaurBone }, { "rocket", Rocket },
        };

        public static bool Has(string id) { return drawings.ContainsKey(id); }

        public static IEnumerable<string> Ids { get { return drawings.Keys; } }

        /// <summary>Build the painter for an object id (null if there is no drawing).</summary>
        public static VectorPainter Paint(string id)
        {
            Action<VectorPainter> draw;
            if (!drawings.TryGetValue(id, out draw)) return null;
            var p = new VectorPainter();
            draw(p);
            return p;
        }

        // =====================================================================================
        // Core: food
        // =====================================================================================

        static void Apple(VectorPainter p)
        {
            p.Capsule(0.02f, 0.4f, 0.1f, 0.82f, 0.06f, Brown);
            p.Add(Sd.Union(Sd.Circle(-0.25f, -0.08f, 0.6f), Sd.Circle(0.25f, -0.08f, 0.6f)), Red)
                .Minus(Sd.Circle(0f, 0.62f, 0.14f)).Minus(Sd.Circle(0f, -0.78f, 0.08f));
            p.Ellipse(0.36f, 0.66f, 0.26f, 0.12f, LeafGreen, 25f);
            p.Line(0.16f, 0.61f, 0.52f, 0.72f, 0.03f, DarkGreen);
            p.Shine(-0.38f, 0.12f, 0.1f, 0.2f, -20f);
        }

        static void Banana(VectorPainter p)
        {
            // Crescent: big circle minus an offset circle, clipped to remove the far tips.
            p.Add(Sd.Circle(0f, 0.55f, 0.98f), Yellow)
                .Minus(Sd.Circle(0.1f, 0.94f, 0.86f))
                .Clip(Sd.Box(0f, -0.1f, 0.86f, 0.6f));
            p.Capsule(-0.86f, 0.18f, -0.97f, 0.3f, 0.06f, DarkBrown);
            p.Circle(0.84f, 0.12f, 0.06f, DarkBrown).NoLine();
            p.Line(-0.55f, -0.08f, 0.5f, -0.1f, 0.03f, Yellow.Dark(0.25f)).Clip(Sd.Circle(0f, 0.6f, 0.86f));
            p.Shine(-0.25f, -0.25f, 0.3f, 0.05f, 8f, 0.4f);
        }

        static void Strawberry(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Union(Sd.Circle(-0.32f, 0.18f, 0.44f), Sd.Circle(0.32f, 0.18f, 0.44f)),
                Sd.Poly(0.2f, -0.6f, 0.22f, 0.6f, 0.22f, 0f, -0.7f)), Red);
            float[] seeds = { -0.4f, 0.2f, -0.1f, 0.28f, 0.22f, 0.22f, 0.45f, 0.08f, -0.28f, -0.08f, 0.05f, -0.02f,
                0.3f, -0.18f, -0.1f, -0.32f, 0.12f, -0.45f, -0.5f, 0.02f };
            for (int i = 0; i < seeds.Length; i += 2) p.Ellipse(seeds[i], seeds[i + 1], 0.035f, 0.055f, Cream).Detail();
            p.Star(0f, 0.55f, 0.36f, 0.14f, 5, LeafGreen, 0.03f, 180f);
            p.Capsule(0f, 0.6f, 0.05f, 0.86f, 0.05f, DarkGreen);
            p.Shine(-0.4f, 0.32f, 0.08f, 0.14f, 30f);
        }

        static void WatermelonSlice(VectorPainter p)
        {
            var below = Sd.HalfPlaneY(0.32f, true);
            p.Add(Sd.Circle(0f, 0.32f, 0.9f), Green).Clip(below);
            p.Add(Sd.Circle(0f, 0.32f, 0.8f), White.Light(0.2f)).Clip(below).NoLine();
            p.Add(Sd.Circle(0f, 0.32f, 0.72f), Red).Clip(below).NoLine();
            float[] seeds = { -0.4f, 0.02f, -0.12f, -0.12f, 0.2f, -0.05f, 0.42f, 0.08f, 0.02f, -0.36f, -0.25f, -0.24f, 0.26f, -0.3f };
            for (int i = 0; i < seeds.Length; i += 2) p.Ellipse(seeds[i], seeds[i + 1], 0.035f, 0.06f, Black).Detail();
        }

        static void OrangeFruit(VectorPainter p)
        {
            p.Circle(0f, -0.06f, 0.76f, Orange);
            for (int i = 0; i < 9; i++)
            {
                double a = i * 0.7;
                p.Dot((float)Math.Cos(a) * (0.2f + 0.05f * i), -0.06f + (float)Math.Sin(a) * (0.2f + 0.05f * i), 0.025f, Orange.Dark(0.18f));
            }
            p.Ellipse(0.24f, 0.74f, 0.22f, 0.1f, LeafGreen, 20f);
            p.Circle(0f, 0.68f, 0.07f, DarkGreen);
            p.Shine(-0.32f, 0.2f, 0.12f, 0.2f, -25f);
        }

        static void Cupcake(VectorPainter p)
        {
            p.Poly(Red.Light(0.35f), 0.04f, -0.56f, -0.1f, 0.56f, -0.1f, 0.4f, -0.86f, -0.4f, -0.86f);
            for (int i = -2; i <= 2; i++) p.Line(i * 0.2f, -0.14f, i * 0.15f, -0.82f, 0.04f, Red.Dark(0.1f));
            p.Add(Sd.Union(Sd.Union(Sd.Circle(-0.4f, 0.02f, 0.3f), Sd.Circle(0.4f, 0.02f, 0.3f)),
                Sd.Union(Sd.Circle(0f, 0.12f, 0.4f), Sd.Circle(0f, 0.42f, 0.3f))), Red);
            p.Line(-0.35f, 0.12f, 0.3f, 0.3f, 0.04f, Red.Dark(0.2f));
            p.Capsule(0.04f, 0.78f, 0.16f, 0.95f, 0.03f, DarkBrown);
            p.Circle(0f, 0.74f, 0.16f, Red.Dark(0.25f));
            p.Shine(-0.05f, 0.8f, 0.04f, 0.06f, 0f, 0.7f);
            p.Shine(-0.22f, 0.36f, 0.08f, 0.12f, -30f);
        }

        static void PizzaSlice(VectorPainter p)
        {
            p.Poly(Yellow, 0.06f, -0.72f, 0.5f, 0.72f, 0.5f, 0f, -0.86f);
            p.Capsule(-0.74f, 0.58f, 0.74f, 0.58f, 0.15f, Tan);
            p.Circle(-0.22f, 0.22f, 0.13f, Red).Flat();
            p.Circle(0.25f, 0.28f, 0.12f, Red).Flat();
            p.Circle(0.02f, -0.18f, 0.12f, Red).Flat();
            p.Circle(-0.04f, -0.55f, 0.07f, Red).Flat();
            p.Line(-0.4f, 0.36f, -0.35f, 0.05f, 0.05f, Yellow.Light(0.4f));
            p.Line(0.45f, 0.38f, 0.38f, 0.12f, 0.05f, Yellow.Light(0.4f));
        }

        static void Donut(VectorPainter p)
        {
            p.Circle(0f, 0f, 0.82f, Tan).Minus(Sd.Circle(0f, 0f, 0.24f));
            p.Add(Sd.Circle(0f, 0.04f, 0.7f), Purple).Minus(Sd.Circle(0f, 0f, 0.34f))
                .Minus(Sd.Circle(-0.62f, -0.42f, 0.16f)).Minus(Sd.Circle(0.6f, -0.46f, 0.14f)).Minus(Sd.Circle(0f, -0.74f, 0.16f));
            Rgba[] sprinkle = { White, Yellow, Pink, Glass };
            float[] s = { -0.4f, 0.4f, 0.1f, 0.55f, 0.45f, 0.3f, -0.55f, 0.05f, 0.52f, -0.05f, -0.2f, -0.45f, 0.25f, -0.48f, -0.1f, 0.42f };
            for (int i = 0; i < s.Length; i += 2)
            {
                float a = i * 0.9f;
                p.Line(s[i], s[i + 1], s[i] + (float)Math.Cos(a) * 0.09f, s[i + 1] + (float)Math.Sin(a) * 0.09f, 0.05f, sprinkle[(i / 2) % 4]);
            }
            p.Shine(-0.4f, 0.4f, 0.08f, 0.15f, 40f, 0.4f);
        }

        static void Popsicle(VectorPainter p)
        {
            p.Capsule(0f, -0.4f, 0f, -0.92f, 0.11f, Tan);
            p.Box(0f, 0.16f, 0.44f, 0.74f, Blue, 0.4f).Minus(Sd.Circle(0.4f, 0.86f, 0.2f));
            p.Line(-0.15f, 0.55f, -0.15f, -0.35f, 0.06f, Blue.Dark(0.2f));
            p.Line(0.15f, 0.45f, 0.15f, -0.35f, 0.06f, Blue.Dark(0.2f));
            p.Shine(-0.28f, 0.42f, 0.06f, 0.26f, 0f);
        }

        static void Ketchup(VectorPainter p)
        {
            p.Box(0f, 0.74f, 0.17f, 0.16f, White, 0.06f);
            p.Poly(Red, 0.05f, -0.2f, 0.62f, 0.2f, 0.62f, 0.42f, 0.3f, -0.42f, 0.3f);
            p.Box(0f, -0.25f, 0.44f, 0.62f, Red, 0.22f);
            p.Box(0f, -0.22f, 0.34f, 0.26f, White, 0.06f);
            p.Circle(0f, -0.2f, 0.13f, Red).Flat();
            p.Ellipse(0.04f, -0.08f, 0.06f, 0.03f, LeafGreen).Detail();
            p.Shine(-0.28f, 0.1f, 0.06f, 0.22f, 0f);
        }

        // =====================================================================================
        // Core: office
        // =====================================================================================

        static void Stapler(VectorPainter p)
        {
            p.Box(0.02f, -0.5f, 0.86f, 0.1f, DarkGray, 0.08f);
            p.Box(0.1f, -0.36f, 0.66f, 0.05f, Steel, 0.02f);
            p.Circle(-0.68f, -0.32f, 0.2f, DarkGray);
            p.Poly(Blue, 0.12f, -0.82f, -0.2f, -0.6f, 0.12f, 0.72f, 0.0f, 0.86f, -0.18f, 0.7f, -0.3f, -0.6f, -0.3f);
            p.Line(-0.45f, 0.0f, 0.5f, -0.08f, 0.05f, Blue.Light(0.35f));
            p.Circle(-0.68f, -0.32f, 0.07f, Steel).Detail();
        }

        static void Pencil(VectorPainter p)
        {
            p.Push(35f);
            p.Poly(Tan, 0.01f, 0.45f, 0.16f, 0.45f, -0.16f, 0.8f, 0f);
            p.Poly(Black, 0.01f, 0.68f, 0.06f, 0.68f, -0.06f, 0.82f, 0f).NoLine();
            p.Box(-0.05f, 0f, 0.52f, 0.16f, Yellow, 0.02f);
            p.Line(-0.55f, 0.05f, 0.45f, 0.05f, 0.03f, Yellow.Dark(0.2f));
            p.Line(-0.55f, -0.06f, 0.45f, -0.06f, 0.03f, Yellow.Dark(0.2f));
            p.Box(-0.62f, 0f, 0.07f, 0.17f, Steel, 0.01f);
            p.Box(-0.77f, 0f, 0.1f, 0.16f, Pink, 0.07f);
            p.Pop();
        }

        static void Pen(VectorPainter p)
        {
            p.Push(-35f);
            p.Poly(Steel, 0.01f, 0.5f, 0.12f, 0.5f, -0.12f, 0.86f, 0f);
            p.Box(0.02f, 0f, 0.56f, 0.17f, Purple, 0.15f);
            p.Box(0.42f, 0f, 0.12f, 0.17f, DarkGray, 0.06f);
            p.Box(-0.42f, 0f, 0.36f, 0.19f, Purple.Dark(0.2f), 0.15f);
            p.Box(-0.44f, 0.24f, 0.28f, 0.05f, Steel, 0.04f);
            p.Shine(0f, 0.07f, 0.4f, 0.035f);
            p.Pop();
        }

        static void Notebook(VectorPainter p)
        {
            p.Box(0.1f, 0f, 0.62f, 0.8f, White, 0.06f);
            p.Box(0.04f, 0f, 0.6f, 0.8f, Blue, 0.08f);
            for (int i = 0; i < 7; i++) p.Ring(-0.56f, 0.6f - i * 0.2f, 0.07f, 0.025f, Steel).NoLine();
            p.Box(0.12f, 0.38f, 0.32f, 0.13f, White, 0.04f);
            p.Line(-0.08f, 0.42f, 0.32f, 0.42f, 0.03f);
            p.Line(-0.08f, 0.33f, 0.22f, 0.33f, 0.03f);
            p.Shine(0.45f, -0.3f, 0.05f, 0.3f, 0f, 0.3f);
        }

        static void PaperClip(VectorPainter p)
        {
            p.OutlineWidth = 0.035f;
            p.Push(30f);
            p.Add(Sd.CapsuleRing(-0.48f, 0f, 0.5f, 0f, 0.32f, 0.075f), Purple)
                .Clip(Sd.Union(Sd.HalfPlaneX(0.25f, false), Sd.HalfPlaneY(-0.05f, false)));
            p.Add(Sd.CapsuleRing(-0.32f, -0.04f, 0.5f, -0.04f, 0.17f, 0.075f), Purple).Clip(Sd.HalfPlaneX(-0.42f, false));
            p.Capsule(-0.32f, -0.21f, 0.25f, -0.32f, 0.075f, Purple);
            p.Pop();
        }

        static void CoffeeMug(VectorPainter p)
        {
            p.Ring(0.44f, -0.08f, 0.28f, 0.1f, Blue);
            p.Box(-0.12f, -0.12f, 0.52f, 0.64f, Blue, 0.14f);
            p.Ellipse(-0.12f, 0.5f, 0.5f, 0.1f, Blue.Dark(0.15f)).NoLine();
            p.Ellipse(-0.12f, 0.5f, 0.42f, 0.07f, DarkBrown).NoLine();
            p.Box(-0.12f, -0.05f, 0.52f, 0.08f, White, 0f).Detail().Clip(Sd.Box(-0.12f, -0.12f, 0.5f, 0.62f, 0.12f));
            p.Line(-0.3f, 0.66f, -0.22f, 0.86f, 0.04f, Gray);
            p.Line(-0.02f, 0.66f, 0.06f, 0.9f, 0.04f, Gray);
            p.Shine(-0.48f, 0.05f, 0.06f, 0.28f);
        }

        static void Scissors(VectorPainter p)
        {
            p.Poly(Steel, 0.02f, -0.09f, 0.0f, 0.07f, 0.1f, 0.42f, 0.84f, 0.3f, 0.88f);
            p.Poly(Steel, 0.02f, 0.09f, 0.0f, -0.07f, 0.1f, -0.42f, 0.84f, -0.3f, 0.88f);
            p.Capsule(-0.06f, 0.0f, -0.26f, -0.3f, 0.12f, Green);
            p.Capsule(0.06f, 0.0f, 0.26f, -0.3f, 0.12f, Green);
            p.Circle(-0.38f, -0.56f, 0.32f, Green).Minus(Sd.Circle(-0.38f, -0.56f, 0.13f));
            p.Circle(0.38f, -0.56f, 0.32f, Green).Minus(Sd.Circle(0.38f, -0.56f, 0.13f));
            p.Circle(0f, 0.02f, 0.07f, DarkGray);
        }

        static void StickyNote(VectorPainter p)
        {
            p.Push(-6f);
            p.Box(0f, 0.02f, 0.74f, 0.74f, Green, 0.03f).Minus(Sd.Poly(0f, 0.36f, -0.8f, 0.8f, -0.8f, 0.8f, -0.36f));
            p.Poly(Green.Light(0.35f), 0.01f, 0.36f, -0.72f, 0.74f, -0.34f, 0.32f, -0.3f);
            p.Box(0f, 0.62f, 0.74f, 0.13f, Green.Dark(0.12f), 0.02f).Detail();
            p.Line(-0.5f, 0.2f, 0.45f, 0.2f, 0.035f, Green.Dark(0.35f));
            p.Line(-0.5f, -0.05f, 0.35f, -0.05f, 0.035f, Green.Dark(0.35f));
            p.Line(-0.5f, -0.3f, 0.05f, -0.3f, 0.035f, Green.Dark(0.35f));
            p.Pop();
        }

        static void Calculator(VectorPainter p)
        {
            p.Box(0f, 0f, 0.58f, 0.84f, Green, 0.14f);
            p.Box(0f, 0.5f, 0.44f, 0.18f, Rgba.Hex("D3EBC8"), 0.05f);
            p.Line(0.05f, 0.5f, 0.32f, 0.5f, 0.06f, Black);
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 3; c++)
                    p.Box(-0.3f + c * 0.3f, 0.12f - r * 0.24f, 0.11f, 0.08f, c == 2 && r == 3 ? Orange : White, 0.04f);
        }

        static void Eraser(VectorPainter p)
        {
            p.Push(-15f);
            p.Box(0f, 0f, 0.78f, 0.34f, White, 0.1f);
            p.Box(0.16f, 0f, 0.5f, 0.35f, Blue, 0.04f);
            p.Line(-0.16f, 0.2f, 0.5f, 0.2f, 0.04f, Blue.Light(0.4f));
            p.Pop();
            p.Dot(-0.8f, -0.25f, 0.04f, Gray);
            p.Dot(-0.88f, -0.1f, 0.03f, Gray);
        }

        // =====================================================================================
        // Core: everyday
        // =====================================================================================

        static void Key(VectorPainter p)
        {
            p.Push(-30f);
            p.Box(0.2f, 0f, 0.5f, 0.09f, Yellow, 0.04f);
            p.Box(0.6f, -0.15f, 0.06f, 0.1f, Yellow, 0.02f);
            p.Box(0.42f, -0.13f, 0.05f, 0.07f, Yellow, 0.02f);
            p.Box(0.26f, -0.15f, 0.05f, 0.1f, Yellow, 0.02f);
            p.Circle(-0.5f, 0f, 0.36f, Yellow).Minus(Sd.Circle(-0.56f, 0f, 0.13f));
            p.Shine(-0.6f, 0.18f, 0.07f, 0.1f, 30f);
            p.Pop();
        }

        static void Sunglasses(VectorPainter p)
        {
            p.Capsule(-0.78f, 0.12f, -0.94f, 0.22f, 0.05f, Purple);
            p.Capsule(0.78f, 0.12f, 0.94f, 0.22f, 0.05f, Purple);
            p.Capsule(-0.14f, 0.1f, 0.14f, 0.1f, 0.06f, Purple);
            p.Box(-0.44f, -0.02f, 0.36f, 0.3f, Purple, 0.2f);
            p.Box(0.44f, -0.02f, 0.36f, 0.3f, Purple, 0.2f);
            p.Box(-0.44f, -0.04f, 0.26f, 0.21f, Black, 0.15f).NoLine();
            p.Box(0.44f, -0.04f, 0.26f, 0.21f, Black, 0.15f).NoLine();
            p.Line(-0.58f, 0.04f, -0.42f, 0.13f, 0.05f, new Rgba(1f, 1f, 1f, 0.6f));
            p.Line(0.3f, 0.04f, 0.46f, 0.13f, 0.05f, new Rgba(1f, 1f, 1f, 0.6f));
        }

        static void Toothbrush(VectorPainter p)
        {
            p.Push(30f);
            p.Box(0.6f, 0.27f, 0.2f, 0.13f, White, 0.03f);
            for (int i = 0; i < 5; i++) p.Line(0.45f + i * 0.075f, 0.17f, 0.45f + i * 0.075f, 0.38f, 0.03f, Glass.Dark(0.2f));
            p.Box(0.6f, 0.06f, 0.24f, 0.11f, Green, 0.08f);
            p.Capsule(0.25f, 0f, 0.45f, 0.03f, 0.09f, Green);
            p.Capsule(-0.82f, 0f, 0.28f, 0f, 0.16f, Green);
            p.Capsule(-0.68f, 0f, -0.15f, 0f, 0.07f, LightGreen).Detail();
            p.Pop();
        }

        static void Sock(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Box(-0.18f, 0.3f, 0.32f, 0.52f, 0.12f), Sd.Capsule(-0.18f, -0.25f, 0.48f, -0.46f, 0.3f)), Red);
            p.Circle(0.58f, -0.5f, 0.2f, Red.Dark(0.2f)).Clip(Sd.Capsule(-0.18f, -0.25f, 0.48f, -0.46f, 0.3f)).NoLine();
            p.Circle(-0.3f, -0.36f, 0.22f, Red.Dark(0.2f)).Clip(Sd.Capsule(-0.18f, -0.25f, 0.48f, -0.46f, 0.3f)).NoLine();
            p.Box(-0.18f, 0.3f, 0.32f, 0.06f, White, 0f).Detail();
            p.Box(-0.18f, 0.08f, 0.32f, 0.06f, White, 0f).Detail();
            p.Box(-0.18f, 0.78f, 0.35f, 0.11f, White, 0.05f);
        }

        static void Shoe(VectorPainter p)
        {
            p.Box(0.04f, -0.56f, 0.9f, 0.11f, White, 0.1f);
            p.Poly(Blue, 0.12f, -0.82f, -0.48f, 0.9f, -0.48f, 0.86f, -0.22f, 0.3f, -0.02f, -0.1f, 0.36f, -0.78f, 0.36f);
            p.Poly(Blue.Light(0.25f), 0.06f, 0.42f, -0.46f, 0.88f, -0.46f, 0.84f, -0.24f, 0.46f, -0.12f).NoLine();
            p.Line(-0.16f, 0.22f, 0.06f, 0.06f, 0.05f, White);
            p.Line(-0.04f, 0.3f, 0.18f, 0.12f, 0.05f, White);
            p.Line(0.08f, 0.36f, 0.3f, 0.18f, 0.05f, White);
            p.Add(Sd.Ring(-0.1f, 0.2f, 0.5f, 0.05f), White).Clip(Sd.Box(-0.1f, -0.15f, 0.55f, 0.15f)).Detail();
        }

        static void Umbrella(VectorPainter p)
        {
            p.Capsule(0f, 0.08f, 0f, -0.62f, 0.045f, DarkGray);
            p.Add(Sd.Ring(0.17f, -0.62f, 0.17f, 0.045f), DarkGray).Clip(Sd.HalfPlaneY(-0.62f, true));
            p.Add(Sd.Circle(0f, 0.05f, 0.88f), Purple).Clip(Sd.HalfPlaneY(0.05f, false))
                .Minus(Sd.Circle(-0.6f, 0.05f, 0.29f)).Minus(Sd.Circle(0f, 0.05f, 0.29f)).Minus(Sd.Circle(0.6f, 0.05f, 0.29f));
            p.Line(0f, 0.9f, -0.3f, 0.32f, 0.03f, Purple.Dark(0.3f));
            p.Line(0f, 0.9f, 0.3f, 0.32f, 0.03f, Purple.Dark(0.3f));
            p.Capsule(0f, 0.88f, 0f, 0.98f, 0.04f, DarkGray);
            p.Shine(-0.45f, 0.5f, 0.08f, 0.2f, 40f, 0.4f);
        }

        static void ToyBall(VectorPainter p)
        {
            p.Circle(0f, 0f, 0.78f, Yellow);
            p.Add(Sd.Ring(0f, -1.3f, 1.25f, 0.09f), Yellow.Dark(0.2f)).Clip(Sd.Circle(0f, 0f, 0.74f)).Detail();
            p.Star(0f, 0.18f, 0.3f, 0.13f, 5, White, 0.02f);
            p.Shine(-0.36f, 0.36f, 0.12f, 0.18f, -40f);
        }

        static void AlarmClock(VectorPainter p)
        {
            p.Capsule(-0.36f, -0.55f, -0.52f, -0.86f, 0.06f, DarkGray);
            p.Capsule(0.36f, -0.55f, 0.52f, -0.86f, 0.06f, DarkGray);
            p.Capsule(0f, 0.6f, 0f, 0.84f, 0.04f, DarkGray);
            p.Circle(0f, 0.86f, 0.07f, DarkGray);
            p.Circle(-0.46f, 0.62f, 0.24f, Red.Dark(0.1f)).Clip(Sd.HalfPlaneY(0.55f, false));
            p.Circle(0.46f, 0.62f, 0.24f, Red.Dark(0.1f)).Clip(Sd.HalfPlaneY(0.55f, false));
            p.Circle(0f, -0.02f, 0.68f, Red);
            p.Circle(0f, -0.02f, 0.5f, White);
            for (int i = 0; i < 12; i++)
            {
                double a = i * Math.PI / 6;
                p.Dot((float)Math.Cos(a) * 0.4f, -0.02f + (float)Math.Sin(a) * 0.4f, 0.025f, Black);
            }
            p.Line(0f, -0.02f, 0f, 0.3f, 0.05f);
            p.Line(0f, -0.02f, 0.2f, -0.12f, 0.05f);
            p.Dot(0f, -0.02f, 0.05f, Red.Dark(0.3f));
        }

        static void SmallPlant(VectorPainter p)
        {
            p.Capsule(0f, -0.35f, 0f, 0.2f, 0.04f, DarkGreen);
            p.Ellipse(-0.42f, 0.12f, 0.27f, 0.5f, Green, 45f);
            p.Ellipse(0.42f, 0.12f, 0.27f, 0.5f, Green, -45f);
            p.Ellipse(0f, 0.4f, 0.26f, 0.52f, Green);
            p.Line(-0.15f, -0.1f, -0.66f, 0.36f, 0.03f, DarkGreen);
            p.Line(0.15f, -0.1f, 0.66f, 0.36f, 0.03f, DarkGreen);
            p.Line(0f, -0.02f, 0f, 0.84f, 0.03f, DarkGreen);
            p.Poly(Cream, 0.04f, -0.34f, -0.36f, 0.34f, -0.36f, 0.26f, -0.9f, -0.26f, -0.9f);
            p.Box(0f, -0.36f, 0.4f, 0.08f, Cream.Dark(0.1f), 0.04f);
        }

        static void GiftBox(VectorPainter p)
        {
            p.Ellipse(-0.24f, 0.62f, 0.24f, 0.15f, White, 25f).Minus(Sd.Ellipse(-0.24f, 0.62f, 0.1f, 0.05f, 25f));
            p.Ellipse(0.24f, 0.62f, 0.24f, 0.15f, White, -25f).Minus(Sd.Ellipse(0.24f, 0.62f, 0.1f, 0.05f, -25f));
            p.Box(0f, -0.24f, 0.7f, 0.58f, Purple, 0.06f);
            p.Box(0f, 0.38f, 0.8f, 0.15f, Purple.Light(0.15f), 0.05f);
            p.Box(0f, -0.2f, 0.11f, 0.68f, White, 0.02f);
            p.Circle(0f, 0.58f, 0.1f, White);
        }

        // =====================================================================================
        // Extended: food
        // =====================================================================================

        static void Burger(VectorPainter p)
        {
            p.Box(0f, -0.62f, 0.76f, 0.16f, Tan, 0.14f);
            p.Box(0f, -0.34f, 0.8f, 0.14f, DarkBrown, 0.12f);
            p.Poly(Yellow, 0.02f, -0.8f, -0.18f, 0.8f, -0.18f, 0.5f, -0.36f, 0.2f, -0.22f, -0.3f, -0.4f);
            p.Add(Sd.Box(0f, -0.08f, 0.84f, 0.09f, 0.08f), LeafGreen);
            p.Add(Sd.Circle(0f, -0.1f, 0.82f), Tan).Clip(Sd.HalfPlaneY(0f, false));
            for (int i = 0; i < 6; i++) p.Ellipse(-0.45f + i * 0.18f, 0.25f + (i % 2) * 0.18f, 0.04f, 0.025f, Cream).Detail();
        }

        static void Fries(VectorPainter p)
        {
            Rgba fry = Rgba.Hex("FFE07A");
            Sdf fries = null;
            for (int i = 0; i < 6; i++)
            {
                var f = Sd.Box(-0.45f + i * 0.18f, 0.32f + (i % 3) * 0.1f, 0.1f, 0.42f, 0.03f, (i - 2.5f) * 4f);
                fries = fries == null ? f : Sd.Union(fries, f);
            }
            p.Add(fries, fry);
            for (int i = 0; i < 5; i++) p.Line(-0.36f + i * 0.18f, 0.2f, -0.36f + i * 0.18f, 0.66f + (i % 2) * 0.1f, 0.03f, Yellow.Dark(0.3f));
            p.Poly(Yellow.Dark(0.08f), 0.05f, -0.64f, 0.12f, 0.64f, 0.12f, 0.46f, -0.88f, -0.46f, -0.88f);
            p.Star(0f, -0.38f, 0.22f, 0.1f, 5, White, 0.02f);
        }

        static void Cookie(VectorPainter p)
        {
            p.Circle(0f, 0f, 0.8f, Tan);
            float[] c = { -0.35f, 0.3f, 0.2f, 0.42f, 0.4f, -0.05f, -0.1f, -0.1f, -0.4f, -0.35f, 0.22f, -0.45f, 0.05f, 0.15f };
            for (int i = 0; i < c.Length; i += 2) p.Ellipse(c[i], c[i + 1], 0.09f, 0.07f, DarkBrown, i * 20f).NoLine();
        }

        static void IceCream(VectorPainter p)
        {
            p.Poly(Tan, 0.03f, -0.45f, 0.05f, 0.45f, 0.05f, 0f, -0.92f);
            p.Line(-0.3f, -0.05f, 0.12f, -0.6f, 0.03f, Brown);
            p.Line(0.3f, -0.05f, -0.12f, -0.6f, 0.03f, Brown);
            p.Add(Sd.Union(Sd.Circle(0f, 0.36f, 0.48f), Sd.Box(0f, 0.06f, 0.52f, 0.1f, 0.08f)), Purple)
                .Minus(Sd.Circle(-0.32f, -0.08f, 0.08f));
            p.Shine(-0.2f, 0.56f, 0.1f, 0.14f, -30f);
        }

        static void Egg(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Ellipse(0f, -0.1f, 0.6f, 0.68f), Sd.Ellipse(0f, 0.15f, 0.48f, 0.72f)), White);
            p.Shine(-0.22f, 0.3f, 0.1f, 0.2f, -20f);
        }

        static void Cheese(VectorPainter p)
        {
            p.Poly(Yellow, 0.05f, -0.82f, -0.5f, 0.82f, -0.5f, 0.82f, 0.25f, -0.82f, -0.12f)
                .Minus(Sd.Circle(-0.3f, -0.2f, 0.12f)).Minus(Sd.Circle(0.3f, 0f, 0.15f)).Minus(Sd.Circle(0.55f, -0.32f, 0.08f));
            p.Poly(Yellow.Light(0.3f), 0.03f, -0.82f, -0.12f, 0.82f, 0.25f, 0.6f, 0.4f, -0.86f, 0.0f);
        }

        static void MilkCarton(VectorPainter p)
        {
            p.Box(0f, -0.25f, 0.46f, 0.6f, Blue, 0.04f);
            p.Poly(Blue.Light(0.25f), 0.03f, -0.46f, 0.35f, 0.46f, 0.35f, 0.2f, 0.75f, -0.2f, 0.75f);
            p.Box(0f, 0.8f, 0.22f, 0.07f, Blue.Dark(0.1f), 0.02f);
            p.Box(0f, -0.25f, 0.32f, 0.28f, White, 0.06f);
            p.Ellipse(0f, -0.25f, 0.16f, 0.12f, Blue.Light(0.4f)).Detail();
        }

        static void Lemon(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Ellipse(0f, 0f, 0.7f, 0.5f, 15f), Sd.Union(Sd.Circle(-0.64f, -0.17f, 0.11f), Sd.Circle(0.64f, 0.17f, 0.11f))), Yellow);
            p.Ellipse(0.2f, 0.56f, 0.22f, 0.1f, LeafGreen, 30f);
            p.Shine(-0.25f, 0.18f, 0.2f, 0.08f, 15f);
        }

        static void Carrot(VectorPainter p)
        {
            p.Ellipse(-0.1f, 0.62f, 0.12f, 0.3f, LeafGreen, 25f);
            p.Ellipse(0.12f, 0.64f, 0.12f, 0.32f, LeafGreen, -20f);
            p.Taper(0f, 0.36f, 0f, -0.88f, 0.28f, 0.03f, Orange);
            p.Line(-0.2f, 0.1f, -0.05f, 0.08f, 0.03f, Orange.Dark(0.3f));
            p.Line(0.05f, -0.25f, 0.16f, -0.27f, 0.03f, Orange.Dark(0.3f));
        }

        static void Broccoli(VectorPainter p)
        {
            p.Poly(LightGreen, 0.06f, -0.2f, 0f, 0.2f, 0f, 0.14f, -0.86f, -0.14f, -0.86f);
            p.Add(Sd.Union(Sd.Union(Sd.Circle(-0.42f, 0.2f, 0.32f), Sd.Circle(0.42f, 0.2f, 0.32f)),
                Sd.Union(Sd.Circle(0f, 0.45f, 0.4f), Sd.Circle(0f, 0.12f, 0.3f))), Green);
            p.Dot(-0.3f, 0.35f, 0.05f, Green.Dark(0.25f));
            p.Dot(0.15f, 0.55f, 0.05f, Green.Dark(0.25f));
            p.Dot(0.4f, 0.22f, 0.05f, Green.Dark(0.25f));
        }

        static void Avocado(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Circle(0f, -0.25f, 0.6f), Sd.Circle(0f, 0.32f, 0.42f)), DarkGreen);
            p.Add(Sd.Union(Sd.Circle(0f, -0.25f, 0.5f), Sd.Circle(0f, 0.3f, 0.32f)), LightGreen).NoLine();
            p.Circle(0f, -0.25f, 0.24f, Brown);
            p.Shine(-0.07f, -0.17f, 0.06f, 0.08f);
        }

        static void Watermelon(VectorPainter p)
        {
            p.Ellipse(0f, 0f, 0.86f, 0.62f, Green);
            for (int i = -2; i <= 2; i++)
                p.Ellipse(i * 0.3f, 0f, 0.06f, 0.6f - Math.Abs(i) * 0.1f, DarkGreen).Detail().Clip(Sd.Ellipse(0f, 0f, 0.8f, 0.56f));
            p.Shine(-0.4f, 0.3f, 0.2f, 0.08f, 20f);
        }

        static void Bread(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Box(0f, -0.25f, 0.82f, 0.4f, 0.1f), Sd.Ellipse(0f, 0.12f, 0.8f, 0.45f)), Tan);
            p.Line(-0.45f, 0.38f, -0.25f, 0.1f, 0.05f, Brown);
            p.Line(-0.05f, 0.45f, 0.15f, 0.15f, 0.05f, Brown);
            p.Line(0.35f, 0.4f, 0.55f, 0.12f, 0.05f, Brown);
        }

        static void CerealBox(VectorPainter p)
        {
            p.Box(0f, 0f, 0.56f, 0.86f, Red, 0.05f);
            p.Box(0f, 0.6f, 0.46f, 0.14f, Yellow, 0.04f);
            p.Add(Sd.Circle(0f, -0.15f, 0.36f), White).Clip(Sd.HalfPlaneY(-0.15f, true));
            p.Dot(-0.12f, -0.12f, 0.08f, Yellow);
            p.Dot(0.1f, -0.1f, 0.08f, Yellow);
            p.Dot(0f, -0.02f, 0.07f, Yellow);
            p.Capsule(0.1f, -0.05f, 0.42f, 0.25f, 0.04f, Gray);
        }

        static void JamJar(VectorPainter p)
        {
            p.Box(0f, -0.15f, 0.56f, 0.66f, Purple, 0.2f);
            p.Box(0f, 0.62f, 0.5f, 0.15f, White, 0.05f);
            p.Box(0f, -0.12f, 0.4f, 0.24f, Cream, 0.05f);
            p.Circle(0f, -0.12f, 0.1f, Purple).Flat();
            p.Shine(-0.38f, -0.2f, 0.06f, 0.35f);
        }

        // =====================================================================================
        // Extended: office
        // =====================================================================================

        static void Ruler(VectorPainter p)
        {
            p.Push(25f);
            p.Box(0f, 0f, 0.92f, 0.18f, Yellow, 0.03f);
            for (int i = 0; i < 15; i++)
            {
                float x = -0.82f + i * 0.117f;
                p.Line(x, 0.18f, x, i % 2 == 0 ? 0.02f : 0.09f, 0.025f);
            }
            p.Pop();
        }

        static void Folder(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Box(0f, -0.08f, 0.82f, 0.62f, 0.06f), Sd.Box(-0.48f, 0.56f, 0.3f, 0.1f, 0.05f)), Blue.Dark(0.2f));
            p.Box(0.05f, 0.1f, 0.68f, 0.52f, White, 0.02f).Detail();
            p.Poly(Blue, 0.05f, -0.82f, -0.7f, 0.82f, -0.7f, 0.9f, 0.32f, -0.74f, 0.32f);
        }

        static void TapeDispenser(VectorPainter p)
        {
            p.Ring(-0.08f, 0.12f, 0.36f, 0.1f, Cream);
            p.Poly(Red, 0.1f, -0.86f, -0.6f, 0.86f, -0.6f, 0.78f, -0.15f, 0.25f, -0.15f, -0.15f, 0.2f, -0.6f, 0.2f, -0.86f, -0.1f);
            p.Poly(Steel, 0.01f, 0.62f, -0.15f, 0.86f, -0.15f, 0.86f, 0.0f, 0.74f, -0.08f, 0.66f, 0.0f);
            p.Circle(-0.08f, 0.12f, 0.12f, Red.Dark(0.2f));
        }

        static void HolePunch(VectorPainter p)
        {
            p.Box(0f, -0.58f, 0.86f, 0.12f, DarkGray, 0.08f);
            p.Box(-0.3f, -0.3f, 0.36f, 0.2f, Purple.Dark(0.2f), 0.06f);
            p.Poly(Purple, 0.12f, -0.82f, -0.12f, -0.72f, 0.25f, 0.78f, 0.02f, 0.86f, -0.18f, -0.4f, -0.35f);
            p.Circle(-0.6f, -0.44f, 0.06f, Black).Detail();
            p.Circle(0f, -0.44f, 0.06f, Black).Detail();
        }

        static void Keyboard(VectorPainter p)
        {
            p.Push(-8f);
            p.Box(0f, 0f, 0.92f, 0.4f, DarkGray, 0.08f);
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 9; c++)
                    p.Box(-0.72f + c * 0.18f + (r % 2) * 0.05f, 0.22f - r * 0.18f, 0.07f, 0.06f, Gray, 0.02f).NoLine();
            p.Box(0f, -0.3f, 0.4f, 0.06f, Gray, 0.02f).NoLine();
            p.Pop();
        }

        static void ComputerMouse(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Ellipse(0f, -0.25f, 0.42f, 0.58f), Sd.Ellipse(0f, 0.0f, 0.38f, 0.5f)), Blue);
            p.Line(0f, 0.42f, 0f, 0.05f, 0.03f);
            p.Line(-0.38f, 0.05f, 0.38f, 0.05f, 0.03f);
            p.Box(0f, 0.3f, 0.05f, 0.1f, DarkGray, 0.04f);
            p.Add(Sd.Ring(0.25f, 0.72f, 0.25f, 0.035f), DarkGray).Clip(Sd.HalfPlaneX(0.25f, true)).Detail();
            p.Capsule(0.25f, 0.97f, 0.6f, 0.97f, 0.035f, DarkGray).Detail();
            p.Shine(-0.2f, -0.1f, 0.07f, 0.2f, 10f);
        }

        static void Headphones(VectorPainter p)
        {
            p.Add(Sd.Ring(0f, 0.05f, 0.62f, 0.08f), Red).Clip(Sd.HalfPlaneY(-0.05f, false));
            p.Box(-0.64f, -0.3f, 0.2f, 0.32f, Red, 0.14f);
            p.Box(0.64f, -0.3f, 0.2f, 0.32f, Red, 0.14f);
            p.Box(-0.46f, -0.3f, 0.07f, 0.26f, DarkGray, 0.06f);
            p.Box(0.46f, -0.3f, 0.07f, 0.26f, DarkGray, 0.06f);
        }

        static void DeskLamp(VectorPainter p)
        {
            p.Box(-0.3f, -0.78f, 0.46f, 0.12f, Green, 0.1f);
            p.Capsule(-0.3f, -0.72f, 0.1f, -0.05f, 0.07f, Green);
            p.Capsule(0.1f, -0.05f, -0.2f, 0.52f, 0.07f, Green);
            p.Circle(0.1f, -0.05f, 0.1f, DarkGray);
            p.Ellipse(0.42f, 0.25f, 0.18f, 0.12f, Yellow.Light(0.5f), -40f).Detail();
            p.Poly(Green, 0.05f, -0.32f, 0.62f, -0.08f, 0.38f, 0.58f, 0.42f, 0.22f, 0.86f);
        }

        static void PaperSheet(VectorPainter p)
        {
            p.Push(6f);
            p.Box(0f, 0f, 0.6f, 0.82f, White, 0.02f).Minus(Sd.Poly(0f, 0.3f, 0.9f, 0.7f, 0.9f, 0.7f, 0.5f));
            p.Poly(Gray.Light(0.4f), 0.01f, 0.3f, 0.82f, 0.6f, 0.52f, 0.3f, 0.52f);
            for (int i = 0; i < 6; i++) p.Line(-0.42f, 0.4f - i * 0.2f, i == 0 ? 0.15f : 0.42f, 0.4f - i * 0.2f, 0.03f, Glass.Dark(0.25f));
            p.Pop();
        }

        static void Clipboard(VectorPainter p)
        {
            p.Box(0f, -0.06f, 0.64f, 0.86f, Brown, 0.08f);
            p.Box(0f, -0.12f, 0.52f, 0.68f, White, 0.02f);
            for (int i = 0; i < 5; i++) p.Line(-0.38f, 0.3f - i * 0.2f, 0.38f, 0.3f - i * 0.2f, 0.03f, Gray);
            p.Box(0f, 0.72f, 0.26f, 0.12f, Steel, 0.06f);
        }

        static void Marker(VectorPainter p)
        {
            p.Push(40f);
            p.Poly(Purple, 0.02f, 0.62f, 0.1f, 0.62f, -0.1f, 0.84f, -0.04f, 0.84f, 0.04f);
            p.Box(0.05f, 0f, 0.58f, 0.15f, White, 0.08f);
            p.Box(-0.42f, 0f, 0.3f, 0.17f, Purple, 0.1f);
            p.Box(0.2f, 0f, 0.24f, 0.155f, Purple.Light(0.25f), 0.02f).NoLine();
            p.Pop();
        }

        static void Highlighter(VectorPainter p)
        {
            p.Push(-40f);
            p.Poly(Yellow.Dark(0.15f), 0.02f, 0.58f, 0.14f, 0.58f, -0.14f, 0.86f, -0.02f, 0.86f, 0.1f);
            p.Box(0.05f, 0f, 0.56f, 0.2f, Yellow, 0.1f);
            p.Box(-0.42f, 0f, 0.3f, 0.22f, Yellow.Dark(0.15f), 0.1f);
            p.Shine(0.1f, 0.08f, 0.35f, 0.03f);
            p.Pop();
        }

        // =====================================================================================
        // Extended: home
        // =====================================================================================

        static void Pillow(VectorPainter p)
        {
            p.Poly(Purple, 0.2f, -0.68f, -0.5f, 0f, -0.38f, 0.68f, -0.5f, 0.56f, 0f, 0.68f, 0.5f, 0f, 0.38f, -0.68f, 0.5f, -0.56f, 0f);
            p.Line(-0.4f, 0.1f, 0.4f, -0.1f, 0.04f, Purple.Dark(0.3f));
            p.Shine(-0.35f, 0.25f, 0.2f, 0.08f, 10f);
        }

        static void Book(VectorPainter p)
        {
            p.Box(0.08f, -0.06f, 0.62f, 0.8f, Cream, 0.04f);
            p.Box(0f, 0f, 0.62f, 0.82f, Red, 0.06f);
            p.Box(-0.52f, 0f, 0.1f, 0.82f, Red.Dark(0.2f), 0.04f).NoLine();
            p.Box(0.08f, 0.35f, 0.32f, 0.12f, Yellow.Light(0.3f), 0.03f);
            p.Line(-0.15f, -0.2f, 0.35f, -0.2f, 0.04f, Red.Dark(0.35f));
        }

        static void RemoteControl(VectorPainter p)
        {
            p.Push(-15f);
            p.Box(0f, 0f, 0.3f, 0.86f, Black, 0.18f);
            p.Circle(0f, 0.6f, 0.08f, Red).NoLine();
            p.Circle(0f, 0.25f, 0.16f, DarkGray).NoLine();
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 2; c++)
                    p.Box(-0.1f + c * 0.2f, -0.1f - r * 0.2f, 0.06f, 0.05f, Gray, 0.02f).NoLine();
            p.Pop();
        }

        static void Wallet(VectorPainter p)
        {
            p.Box(0f, -0.1f, 0.82f, 0.56f, Brown, 0.1f);
            p.Poly(Brown.Light(0.15f), 0.08f, -0.82f, 0.46f, 0.82f, 0.46f, 0.82f, 0.08f, 0f, -0.08f, -0.82f, 0.08f);
            p.Circle(0f, -0.02f, 0.08f, Yellow);
            p.Box(0f, -0.1f, 0.74f, 0.48f, Brown, 0f).Detail().Minus(Sd.Box(0f, -0.1f, 0.72f, 0.46f));
        }

        static void Telephone(VectorPainter p)
        {
            p.Poly(Red, 0.12f, -0.72f, -0.82f, 0.72f, -0.82f, 0.5f, 0.1f, -0.5f, 0.1f);
            p.Circle(0f, -0.4f, 0.3f, White);
            p.Circle(0f, -0.4f, 0.1f, Red.Dark(0.2f));
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4.5 + 0.5;
                p.Dot((float)Math.Cos(a) * 0.2f, -0.4f + (float)Math.Sin(a) * 0.2f, 0.04f, Gray);
            }
            p.Add(Sd.Union(Sd.Capsule(-0.6f, 0.38f, 0.6f, 0.38f, 0.15f),
                Sd.Union(Sd.Circle(-0.68f, 0.24f, 0.2f), Sd.Circle(0.68f, 0.24f, 0.2f))), Red);
        }

        static void Hairbrush(VectorPainter p)
        {
            p.Push(-35f);
            p.Capsule(-0.85f, 0f, 0f, 0f, 0.13f, Purple);
            p.Ellipse(0.42f, 0f, 0.46f, 0.32f, Purple);
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 4; c++)
                    p.Dot(0.18f + c * 0.15f, -0.17f + r * 0.17f, 0.035f, DarkGray);
            p.Pop();
        }

        static void Shampoo(VectorPainter p)
        {
            p.Box(0f, 0.7f, 0.18f, 0.16f, White, 0.05f);
            p.Box(0f, -0.15f, 0.46f, 0.72f, Green, 0.22f);
            p.Box(0f, -0.15f, 0.32f, 0.3f, White, 0.06f);
            p.Dot(-0.08f, -0.08f, 0.06f, Glass);
            p.Dot(0.1f, -0.2f, 0.08f, Glass);
            p.Shine(-0.3f, 0.2f, 0.05f, 0.25f);
        }

        static void Soap(VectorPainter p)
        {
            p.Box(0f, -0.28f, 0.72f, 0.36f, Blue, 0.24f);
            p.Shine(-0.3f, -0.12f, 0.25f, 0.06f, 5f);
            p.Ring(0.1f, 0.38f, 0.2f, 0.035f, Glass).NoLine();
            p.Ring(-0.3f, 0.5f, 0.14f, 0.03f, Glass).NoLine();
            p.Ring(0.42f, 0.66f, 0.12f, 0.03f, Glass).NoLine();
        }

        static void Towel(VectorPainter p)
        {
            p.Box(0f, -0.3f, 0.82f, 0.32f, Yellow, 0.12f);
            p.Box(0f, 0.18f, 0.74f, 0.24f, Yellow.Light(0.12f), 0.12f);
            p.Line(-0.6f, -0.3f, 0.6f, -0.3f, 0.06f, Yellow.Dark(0.25f));
            p.Line(-0.5f, 0.18f, 0.5f, 0.18f, 0.06f, Yellow.Dark(0.25f));
        }

        static void Hanger(VectorPainter p)
        {
            p.OutlineWidth = 0.03f;
            p.Add(Sd.Ring(0.08f, 0.62f, 0.14f, 0.045f), Brown).Clip(Sd.HalfPlaneY(0.6f, false));
            p.Capsule(-0.06f, 0.6f, 0f, 0.4f, 0.045f, Brown);
            p.Capsule(0f, 0.4f, -0.88f, -0.32f, 0.07f, Brown);
            p.Capsule(0f, 0.4f, 0.88f, -0.32f, 0.07f, Brown);
            p.Capsule(-0.86f, -0.34f, 0.86f, -0.34f, 0.05f, Brown);
        }

        static void LaundryBasket(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Circle(-0.3f, 0.3f, 0.22f), Sd.Circle(0.25f, 0.34f, 0.24f)), White);
            var body = p.Poly(Blue, 0.06f, -0.82f, 0.28f, 0.82f, 0.28f, 0.6f, -0.86f, -0.6f, -0.86f);
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 5; c++)
                    body.Minus(Sd.Box(-0.44f + c * 0.22f, -0.05f - r * 0.24f, 0.06f, 0.07f, 0.02f));
            p.Box(0f, 0.3f, 0.86f, 0.08f, Blue.Dark(0.15f), 0.04f);
        }

        static void TissueBox(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Circle(-0.12f, 0.42f, 0.22f), Sd.Union(Sd.Circle(0.12f, 0.5f, 0.24f), Sd.Circle(0f, 0.72f, 0.16f))), White);
            p.Box(0f, -0.25f, 0.78f, 0.52f, Green, 0.06f);
            p.Ellipse(0f, 0.2f, 0.36f, 0.06f, Green.Dark(0.3f)).NoLine();
            p.Dot(-0.4f, -0.3f, 0.1f, Green.Light(0.35f));
            p.Dot(0.35f, -0.45f, 0.08f, Green.Light(0.35f));
            p.Dot(0.1f, -0.15f, 0.06f, Green.Light(0.35f));
        }

        // =====================================================================================
        // Extended: toys
        // =====================================================================================

        static void TeddyBear(VectorPainter p)
        {
            p.Circle(-0.36f, 0.68f, 0.16f, Brown);
            p.Circle(0.36f, 0.68f, 0.16f, Brown);
            p.Ellipse(0f, -0.42f, 0.48f, 0.44f, Brown);
            p.Circle(-0.5f, -0.6f, 0.17f, Brown);
            p.Circle(0.5f, -0.6f, 0.17f, Brown);
            p.Circle(0f, 0.32f, 0.44f, Brown);
            p.Ellipse(0f, 0.2f, 0.18f, 0.13f, Cream);
            p.Dot(0f, 0.25f, 0.05f, Black);
            p.Dot(-0.16f, 0.42f, 0.05f, Black);
            p.Dot(0.16f, 0.42f, 0.05f, Black);
            p.Ellipse(0f, -0.45f, 0.24f, 0.24f, Cream).NoLine();
        }

        static void ToyCar(VectorPainter p)
        {
            p.Poly(Red.Dark(0.1f), 0.1f, -0.4f, 0.0f, 0.4f, 0.0f, 0.25f, 0.42f, -0.28f, 0.42f);
            p.Poly(Glass, 0.04f, -0.3f, 0.06f, -0.03f, 0.06f, -0.03f, 0.34f, -0.22f, 0.34f).NoLine();
            p.Poly(Glass, 0.04f, 0.03f, 0.06f, 0.3f, 0.06f, 0.18f, 0.34f, 0.03f, 0.34f).NoLine();
            p.Box(0f, -0.2f, 0.86f, 0.24f, Red, 0.14f);
            p.Circle(-0.5f, -0.44f, 0.2f, Black);
            p.Circle(0.5f, -0.44f, 0.2f, Black);
            p.Circle(-0.5f, -0.44f, 0.08f, Gray).NoLine();
            p.Circle(0.5f, -0.44f, 0.08f, Gray).NoLine();
            p.Dot(0.78f, -0.12f, 0.05f, Yellow.Light(0.4f));
        }

        static void BuildingBlock(VectorPainter p)
        {
            p.Box(-0.3f, 0.3f, 0.18f, 0.12f, Yellow, 0.04f);
            p.Box(0.3f, 0.3f, 0.18f, 0.12f, Yellow, 0.04f);
            p.Box(0f, -0.2f, 0.72f, 0.46f, Yellow, 0.06f);
            p.Shine(-0.4f, 0.0f, 0.18f, 0.05f);
        }

        static void ToyTrain(VectorPainter p)
        {
            p.Box(-0.5f, 0.32f, 0.1f, 0.18f, DarkGray, 0.03f);
            p.Box(0.38f, 0.2f, 0.34f, 0.42f, Blue.Dark(0.1f), 0.06f);
            p.Box(0.38f, 0.32f, 0.18f, 0.14f, Glass, 0.04f).NoLine();
            p.Box(-0.15f, -0.1f, 0.72f, 0.3f, Blue, 0.08f);
            p.Poly(Red, 0.02f, -0.87f, -0.42f, -0.87f, -0.15f, -1.0f, -0.42f);
            p.Circle(-0.5f, -0.45f, 0.17f, Red);
            p.Circle(0f, -0.45f, 0.17f, Red);
            p.Circle(0.48f, -0.45f, 0.17f, Red);
        }

        static void RubberDuck(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Ellipse(-0.05f, -0.38f, 0.75f, 0.42f), Sd.Ellipse(-0.72f, -0.18f, 0.18f, 0.16f, 30f)), Yellow);
            p.Circle(0.28f, 0.3f, 0.34f, Yellow);
            p.Poly(Orange, 0.06f, 0.55f, 0.36f, 0.9f, 0.25f, 0.55f, 0.15f);
            p.Dot(0.36f, 0.4f, 0.06f, Black);
            p.Ellipse(-0.15f, -0.32f, 0.32f, 0.18f, Yellow.Dark(0.12f), -10f).Detail();
        }

        static void YoYo(VectorPainter p)
        {
            p.Line(0f, 0.1f, 0.1f, 0.95f, 0.035f, Gray);
            p.Circle(0f, -0.08f, 0.72f, Red);
            p.Circle(0f, -0.08f, 0.45f, Red.Dark(0.18f)).NoLine();
            p.Circle(0f, -0.08f, 0.12f, White);
            p.Shine(-0.35f, 0.25f, 0.1f, 0.15f, -40f);
        }

        static void ToyRobot(VectorPainter p)
        {
            p.Capsule(0f, 0.65f, 0f, 0.85f, 0.03f, DarkGray);
            p.Circle(0f, 0.9f, 0.07f, Red);
            p.Capsule(-0.6f, -0.05f, -0.75f, -0.4f, 0.09f, Blue.Dark(0.2f));
            p.Capsule(0.6f, -0.05f, 0.75f, -0.4f, 0.09f, Blue.Dark(0.2f));
            p.Box(-0.22f, -0.72f, 0.12f, 0.16f, DarkGray, 0.04f);
            p.Box(0.22f, -0.72f, 0.12f, 0.16f, DarkGray, 0.04f);
            p.Box(0f, -0.2f, 0.5f, 0.4f, Blue, 0.08f);
            p.Box(0f, 0.42f, 0.38f, 0.24f, Blue, 0.08f);
            p.Circle(-0.15f, 0.44f, 0.08f, White);
            p.Circle(0.15f, 0.44f, 0.08f, White);
            p.Dot(-0.15f, 0.44f, 0.035f, Black);
            p.Dot(0.15f, 0.44f, 0.035f, Black);
            p.Box(0f, -0.2f, 0.25f, 0.15f, Gray, 0.04f);
            p.Dot(-0.1f, -0.2f, 0.04f, Red);
            p.Dot(0.08f, -0.2f, 0.04f, Yellow);
        }

        // =====================================================================================
        // Extended: electronics
        // =====================================================================================

        static void Smartphone(VectorPainter p)
        {
            p.Push(-10f);
            p.Box(0f, 0f, 0.42f, 0.84f, Black, 0.12f);
            p.Box(0f, 0f, 0.35f, 0.72f, Screen, 0.06f).NoLine();
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    p.Box(-0.2f + c * 0.2f, 0.45f - r * 0.24f, 0.06f, 0.06f, c == r ? Yellow : Glass, 0.02f).NoLine();
            p.Pop();
        }

        static void Tablet(VectorPainter p)
        {
            p.Box(0f, 0f, 0.86f, 0.62f, Black, 0.1f);
            p.Box(-0.04f, 0f, 0.72f, 0.52f, Screen, 0.04f).NoLine();
            p.Circle(0.8f, 0f, 0.035f, DarkGray).Detail();
            p.Add(Sd.Circle(-0.2f, -0.05f, 0.25f), Glass.Dark(0.2f)).Detail();
            p.Poly(LeafGreen, 0f, -0.76f, -0.52f, -0.3f, -0.1f, 0.1f, -0.4f, 0.4f, -0.2f, 0.68f, -0.52f).Detail();
        }

        static void CameraBody(VectorPainter p)
        {
            p.Box(-0.4f, 0.48f, 0.2f, 0.1f, DarkGray, 0.04f);
            p.Box(0f, -0.08f, 0.84f, 0.52f, DarkGray, 0.12f);
            p.Box(0.58f, 0.32f, 0.12f, 0.07f, Glass, 0.02f).NoLine();
            p.Circle(0f, -0.08f, 0.42f, Gray);
            p.Circle(0f, -0.08f, 0.3f, Black);
            p.Circle(0f, -0.08f, 0.15f, Screen).NoLine();
            p.Shine(-0.08f, 0f, 0.06f, 0.06f, 0f, 0.7f);
            p.Circle(0.6f, 0.52f, 0.07f, Red);
        }

        static void GameController(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Box(0f, 0.05f, 0.62f, 0.3f, 0.2f),
                Sd.Union(Sd.Circle(-0.56f, -0.2f, 0.3f), Sd.Circle(0.56f, -0.2f, 0.3f))), Purple);
            p.Box(-0.48f, 0.0f, 0.15f, 0.05f, DarkGray, 0.02f).NoLine();
            p.Box(-0.48f, 0.0f, 0.05f, 0.15f, DarkGray, 0.02f).NoLine();
            p.Dot(0.45f, 0.12f, 0.07f, Yellow);
            p.Dot(0.6f, -0.02f, 0.07f, Red);
            p.Dot(0.3f, -0.02f, 0.07f, Blue);
            p.Dot(0.45f, -0.16f, 0.07f, Green);
            p.Dot(-0.15f, -0.18f, 0.1f, DarkGray);
            p.Dot(0.15f, -0.18f, 0.1f, DarkGray);
        }

        static void Smartwatch(VectorPainter p)
        {
            p.Box(0f, 0f, 0.22f, 0.92f, DarkGray, 0.15f);
            p.Box(0f, 0f, 0.4f, 0.44f, Black, 0.14f);
            p.Box(0f, 0f, 0.31f, 0.35f, Screen, 0.08f).NoLine();
            p.Ring(0f, 0f, 0.2f, 0.035f, LeafGreen).Detail();
            p.Box(0.42f, 0.1f, 0.04f, 0.08f, Gray, 0.02f);
        }

        static void Speaker(VectorPainter p)
        {
            p.Box(0f, 0f, 0.55f, 0.86f, DarkGray, 0.1f);
            p.Circle(0f, 0.44f, 0.2f, Black);
            p.Circle(0f, 0.44f, 0.08f, Gray).NoLine();
            p.Circle(0f, -0.3f, 0.36f, Black);
            p.Circle(0f, -0.3f, 0.14f, Gray).NoLine();
        }

        // =====================================================================================
        // Extended: nature
        // =====================================================================================

        static void Flower(VectorPainter p)
        {
            p.Capsule(0f, 0.2f, 0.05f, -0.9f, 0.05f, DarkGreen);
            p.Ellipse(0.25f, -0.5f, 0.24f, 0.1f, LeafGreen, 30f);
            for (int i = 0; i < 6; i++)
            {
                double a = i * Math.PI / 3;
                p.Circle((float)Math.Cos(a) * 0.3f, 0.35f + (float)Math.Sin(a) * 0.3f, 0.24f, Purple);
            }
            p.Circle(0f, 0.35f, 0.18f, Yellow);
        }

        static void Leaf(VectorPainter p)
        {
            p.Push(30f);
            p.Add(Sd.Union(Sd.Circle(0f, -0.48f, 0.72f), Sd.Circle(0f, 0.48f, 0.72f)), Green)
                .Clip(Sd.Circle(-0.48f, 0f, 0.82f)).Clip(Sd.Circle(0.48f, 0f, 0.82f));
            p.Capsule(0f, -0.62f, 0f, -0.95f, 0.04f, DarkGreen);
            p.Line(0f, -0.62f, 0f, 0.65f, 0.035f, DarkGreen);
            for (int i = 0; i < 3; i++)
            {
                float y = -0.3f + i * 0.3f;
                p.Line(0f, y, -0.25f, y + 0.18f, 0.03f, DarkGreen);
                p.Line(0f, y, 0.25f, y + 0.18f, 0.03f, DarkGreen);
            }
            p.Pop();
        }

        static void Mushroom(VectorPainter p)
        {
            p.Box(0f, -0.45f, 0.24f, 0.42f, Cream, 0.15f);
            p.Add(Sd.Ellipse(0f, -0.02f, 0.86f, 0.8f), Red).Clip(Sd.HalfPlaneY(-0.05f, false));
            p.Dot(-0.4f, 0.25f, 0.12f, White);
            p.Dot(0.1f, 0.5f, 0.1f, White);
            p.Dot(0.45f, 0.2f, 0.1f, White);
            p.Dot(-0.05f, 0.12f, 0.07f, White);
        }

        static void Rock(VectorPainter p)
        {
            p.Poly(Gray, 0.12f, -0.82f, -0.5f, 0.84f, -0.52f, 0.7f, 0.14f, 0.2f, 0.5f, -0.3f, 0.42f, -0.72f, 0.05f);
            p.Line(-0.2f, 0.15f, 0.05f, -0.15f, 0.04f, DarkGray);
            p.Line(0.3f, 0.1f, 0.45f, -0.2f, 0.04f, DarkGray);
            p.Shine(-0.3f, 0.2f, 0.2f, 0.07f, 20f, 0.35f);
        }

        static void PineCone(VectorPainter p)
        {
            p.Ellipse(0f, -0.05f, 0.5f, 0.82f, Brown);
            for (int r = 0; r < 6; r++)
            {
                float y = 0.55f - r * 0.24f;
                float w = 0.45f * (float)Math.Sin((r + 1) / 7.0 * Math.PI);
                for (int c = -1; c <= 1; c++)
                    p.Add(Sd.Ring(c * w * 0.6f, y, 0.11f, 0.03f), DarkBrown).Clip(Sd.HalfPlaneY(y, true)).Detail();
            }
            p.Capsule(0f, 0.72f, 0.06f, 0.95f, 0.04f, DarkBrown);
        }

        // =====================================================================================
        // Later: animals
        // =====================================================================================

        static void Fish(VectorPainter p)
        {
            p.Poly(Blue.Dark(0.15f), 0.06f, 0.4f, 0f, 0.92f, 0.42f, 0.92f, -0.42f);
            p.Poly(Blue.Dark(0.15f), 0.05f, -0.15f, 0.3f, 0.15f, 0.62f, 0.2f, 0.3f);
            p.Ellipse(-0.12f, 0f, 0.62f, 0.38f, Blue);
            p.Circle(-0.45f, 0.08f, 0.09f, White);
            p.Dot(-0.47f, 0.08f, 0.045f, Black);
            p.Line(-0.2f, 0.25f, -0.2f, -0.25f, 0.03f, Blue.Dark(0.3f));
        }

        static void Butterfly(VectorPainter p)
        {
            p.Ellipse(-0.42f, 0.3f, 0.36f, 0.42f, Purple, 20f);
            p.Ellipse(0.42f, 0.3f, 0.36f, 0.42f, Purple, -20f);
            p.Ellipse(-0.34f, -0.36f, 0.26f, 0.3f, Purple.Light(0.2f), -20f);
            p.Ellipse(0.34f, -0.36f, 0.26f, 0.3f, Purple.Light(0.2f), 20f);
            p.Dot(-0.45f, 0.35f, 0.1f, Yellow);
            p.Dot(0.45f, 0.35f, 0.1f, Yellow);
            p.Capsule(0f, 0.45f, 0f, -0.6f, 0.07f, DarkGray);
            p.Line(0f, 0.5f, -0.22f, 0.9f, 0.03f);
            p.Line(0f, 0.5f, 0.22f, 0.9f, 0.03f);
        }

        static void Frog(VectorPainter p)
        {
            p.Ellipse(-0.6f, -0.62f, 0.3f, 0.14f, Green.Dark(0.15f));
            p.Ellipse(0.6f, -0.62f, 0.3f, 0.14f, Green.Dark(0.15f));
            p.Ellipse(0f, -0.2f, 0.74f, 0.52f, Green);
            p.Circle(-0.36f, 0.3f, 0.22f, Green);
            p.Circle(0.36f, 0.3f, 0.22f, Green);
            p.Circle(-0.36f, 0.32f, 0.13f, White);
            p.Circle(0.36f, 0.32f, 0.13f, White);
            p.Dot(-0.36f, 0.32f, 0.06f, Black);
            p.Dot(0.36f, 0.32f, 0.06f, Black);
            p.Add(Sd.Ring(0f, 0.05f, 0.4f, 0.03f), Black).Clip(Sd.HalfPlaneY(-0.15f, true)).Detail();
            p.Dot(-0.4f, -0.25f, 0.06f, Pink);
            p.Dot(0.4f, -0.25f, 0.06f, Pink);
        }

        static void Bird(VectorPainter p)
        {
            p.Line(-0.05f, -0.55f, -0.12f, -0.85f, 0.04f, DarkGray);
            p.Line(0.15f, -0.55f, 0.12f, -0.85f, 0.04f, DarkGray);
            p.Poly(Red.Dark(0.2f), 0.04f, -0.5f, -0.1f, -0.95f, -0.35f, -0.92f, 0.05f);
            p.Ellipse(0f, -0.15f, 0.56f, 0.44f, Red);
            p.Circle(0.4f, 0.32f, 0.3f, Red);
            p.Poly(Yellow, 0.03f, 0.64f, 0.38f, 0.92f, 0.28f, 0.64f, 0.18f);
            p.Dot(0.46f, 0.38f, 0.05f, Black);
            p.Ellipse(-0.1f, -0.1f, 0.32f, 0.2f, Red.Dark(0.2f), -20f);
        }

        static void Turtle(VectorPainter p)
        {
            p.Circle(0.72f, -0.15f, 0.2f, LightGreen);
            p.Ellipse(-0.45f, -0.5f, 0.15f, 0.12f, LightGreen);
            p.Ellipse(0.4f, -0.5f, 0.15f, 0.12f, LightGreen);
            p.Add(Sd.Ellipse(0f, -0.35f, 0.66f, 0.72f), Green).Clip(Sd.HalfPlaneY(-0.4f, false));
            p.Box(0f, -0.42f, 0.68f, 0.05f, Green.Dark(0.25f), 0.04f);
            p.Star(0f, 0.0f, 0.2f, 0.17f, 3, Green.Dark(0.2f), 0.04f).Detail();
            p.Dot(0.78f, -0.1f, 0.04f, Black);
        }

        static void Cat(VectorPainter p)
        {
            p.Capsule(0.45f, -0.6f, 0.8f, -0.1f, 0.08f, Gray);
            p.Ellipse(0f, -0.4f, 0.5f, 0.5f, Gray);
            p.Poly(Gray, 0.03f, -0.45f, 0.35f, -0.4f, 0.85f, -0.1f, 0.55f);
            p.Poly(Gray, 0.03f, 0.45f, 0.35f, 0.4f, 0.85f, 0.1f, 0.55f);
            p.Circle(0f, 0.3f, 0.42f, Gray);
            p.Dot(-0.16f, 0.36f, 0.06f, Black);
            p.Dot(0.16f, 0.36f, 0.06f, Black);
            p.Poly(Pink, 0.01f, -0.06f, 0.24f, 0.06f, 0.24f, 0f, 0.17f).Detail();
            p.Line(-0.15f, 0.18f, -0.55f, 0.25f, 0.025f);
            p.Line(-0.15f, 0.14f, -0.55f, 0.08f, 0.025f);
            p.Line(0.15f, 0.18f, 0.55f, 0.25f, 0.025f);
            p.Line(0.15f, 0.14f, 0.55f, 0.08f, 0.025f);
        }

        static void Dog(VectorPainter p)
        {
            p.Capsule(-0.55f, -0.2f, -0.82f, 0.05f, 0.07f, Brown);
            p.Ellipse(-0.1f, -0.35f, 0.56f, 0.38f, Tan);
            p.Box(-0.42f, -0.7f, 0.08f, 0.15f, Tan, 0.05f);
            p.Box(0.2f, -0.7f, 0.08f, 0.15f, Tan, 0.05f);
            p.Circle(0.4f, 0.25f, 0.36f, Tan);
            p.Ellipse(0.72f, 0.12f, 0.2f, 0.14f, Cream);
            p.Dot(0.88f, 0.16f, 0.06f, Black);
            p.Ellipse(0.16f, 0.22f, 0.13f, 0.3f, Brown, -15f);
            p.Dot(0.52f, 0.35f, 0.05f, Black);
        }

        // =====================================================================================
        // Later: fantasy / absurd
        // =====================================================================================

        static void Monster(VectorPainter p)
        {
            p.Poly(Cream, 0.03f, -0.45f, 0.5f, -0.3f, 0.9f, -0.2f, 0.52f);
            p.Poly(Cream, 0.03f, 0.45f, 0.5f, 0.3f, 0.9f, 0.2f, 0.52f);
            p.Add(Sd.Union(Sd.Circle(0f, 0.05f, 0.6f), Sd.Box(0f, -0.45f, 0.62f, 0.38f, 0.2f)), Purple);
            p.Circle(0f, 0.2f, 0.26f, White);
            p.Circle(0.04f, 0.18f, 0.12f, Green.Dark(0.1f));
            p.Dot(0.06f, 0.18f, 0.05f, Black);
            p.Box(0f, -0.4f, 0.34f, 0.12f, DarkBrown, 0.1f);
            p.Poly(White, 0f, -0.25f, -0.3f, -0.12f, -0.3f, -0.18f, -0.42f).Detail();
            p.Poly(White, 0f, 0.12f, -0.3f, 0.25f, -0.3f, 0.18f, -0.42f).Detail();
        }

        static void Alien(VectorPainter p)
        {
            p.Capsule(-0.2f, 0.75f, -0.3f, 0.95f, 0.03f, Green.Dark(0.2f));
            p.Capsule(0.2f, 0.75f, 0.3f, 0.95f, 0.03f, Green.Dark(0.2f));
            p.Box(0f, -0.65f, 0.3f, 0.28f, Green.Dark(0.1f), 0.18f);
            p.Add(Sd.Union(Sd.Circle(0f, 0.3f, 0.55f), Sd.Ellipse(0f, -0.05f, 0.32f, 0.45f)), Green);
            p.Ellipse(-0.22f, 0.22f, 0.15f, 0.24f, Black, -25f);
            p.Ellipse(0.22f, 0.22f, 0.15f, 0.24f, Black, 25f);
            p.Shine(-0.25f, 0.3f, 0.04f, 0.06f, 0f, 0.8f);
            p.Shine(0.19f, 0.3f, 0.04f, 0.06f, 0f, 0.8f);
            p.Line(-0.08f, -0.25f, 0.08f, -0.25f, 0.03f);
        }

        static void Ufo(VectorPainter p)
        {
            p.Add(Sd.Circle(0f, 0.08f, 0.42f), Glass).Clip(Sd.HalfPlaneY(0.05f, false));
            p.Ellipse(0f, -0.05f, 0.9f, 0.24f, Steel);
            p.Ellipse(0f, -0.2f, 0.45f, 0.1f, DarkGray).NoLine();
            for (int i = -2; i <= 2; i++) p.Dot(i * 0.3f, -0.02f, 0.05f, Yellow);
            p.Dot(0.05f, 0.3f, 0.12f, Green);
        }

        static void TreasureChest(VectorPainter p)
        {
            p.Box(0f, -0.35f, 0.82f, 0.46f, Brown, 0.06f);
            p.Add(Sd.Ellipse(0f, 0.12f, 0.82f, 0.48f), Brown.Light(0.1f)).Clip(Sd.HalfPlaneY(0.1f, false));
            p.Box(-0.5f, -0.1f, 0.08f, 0.66f, Yellow, 0.02f);
            p.Box(0.5f, -0.1f, 0.08f, 0.66f, Yellow, 0.02f);
            p.Box(0f, 0.1f, 0.84f, 0.05f, Yellow.Dark(0.2f), 0.02f);
            p.Box(0f, 0.0f, 0.12f, 0.15f, Yellow, 0.04f);
            p.Dot(0f, -0.02f, 0.04f, Black);
        }

        static void MagicWand(VectorPainter p)
        {
            p.Capsule(-0.7f, -0.78f, 0.12f, 0.08f, 0.07f, Black);
            p.Capsule(-0.7f, -0.78f, -0.52f, -0.6f, 0.075f, White);
            p.Star(0.3f, 0.28f, 0.52f, 0.22f, 5, Yellow, 0.06f, -12f);
            p.Star(-0.4f, 0.6f, 0.12f, 0.04f, 4, Yellow.Light(0.3f)).NoLine();
            p.Star(0.75f, -0.35f, 0.1f, 0.035f, 4, Yellow.Light(0.3f)).NoLine();
            p.Shine(0.2f, 0.4f, 0.08f, 0.12f, 30f);
        }

        static void DragonEgg(VectorPainter p)
        {
            p.Add(Sd.Union(Sd.Ellipse(0f, -0.15f, 0.62f, 0.68f), Sd.Ellipse(0f, 0.12f, 0.5f, 0.75f)), Red);
            for (int r = 0; r < 4; r++)
                for (int c = -2; c <= 2; c++)
                {
                    float x = c * 0.22f + (r % 2) * 0.11f, y = 0.4f - r * 0.3f;
                    p.Add(Sd.Ring(x, y, 0.1f, 0.025f), Red.Dark(0.3f)).Clip(Sd.HalfPlaneY(y, true)).Detail()
                        .Clip(Sd.Ellipse(0f, -0.1f, 0.55f, 0.62f));
                }
            p.Shine(-0.25f, 0.35f, 0.08f, 0.18f, -20f);
        }

        static void WizardHat(VectorPainter p)
        {
            p.Ellipse(0f, -0.65f, 0.9f, 0.2f, Blue.Dark(0.1f));
            p.Poly(Blue, 0.04f, -0.55f, -0.62f, 0.55f, -0.62f, 0.15f, 0.55f, 0.55f, 0.85f, 0.0f, 0.62f);
            p.Box(0f, -0.48f, 0.5f, 0.08f, Yellow, 0.02f, 0f);
            p.Star(-0.12f, -0.1f, 0.12f, 0.05f, 5, Yellow).NoLine();
            p.Star(0.15f, 0.2f, 0.08f, 0.035f, 5, Yellow).NoLine();
            p.Star(0.22f, -0.28f, 0.07f, 0.03f, 5, Yellow).NoLine();
        }

        static void DinosaurBone(VectorPainter p)
        {
            p.Push(-25f);
            p.Add(Sd.Union(Sd.Capsule(-0.55f, 0f, 0.55f, 0f, 0.16f),
                Sd.Union(Sd.Union(Sd.Circle(-0.68f, 0.16f, 0.18f), Sd.Circle(-0.68f, -0.16f, 0.18f)),
                    Sd.Union(Sd.Circle(0.68f, 0.16f, 0.18f), Sd.Circle(0.68f, -0.16f, 0.18f)))), Cream);
            p.Line(-0.2f, 0.05f, 0.25f, 0.05f, 0.03f, Tan);
            p.Pop();
        }

        static void Rocket(VectorPainter p)
        {
            p.Poly(Yellow, 0.05f, -0.16f, -0.62f, 0.16f, -0.62f, 0f, -0.96f);
            p.Poly(Orange, 0.03f, -0.08f, -0.62f, 0.08f, -0.62f, 0f, -0.82f).NoLine();
            p.Poly(Red.Dark(0.2f), 0.04f, -0.28f, -0.2f, -0.55f, -0.62f, -0.2f, -0.55f);
            p.Poly(Red.Dark(0.2f), 0.04f, 0.28f, -0.2f, 0.55f, -0.62f, 0.2f, -0.55f);
            p.Add(Sd.Union(Sd.Ellipse(0f, 0.05f, 0.3f, 0.66f), Sd.Box(0f, -0.4f, 0.25f, 0.2f, 0.06f)), Red);
            p.Circle(0f, 0.15f, 0.14f, Glass);
            p.Box(0f, -0.55f, 0.27f, 0.05f, Gray, 0.02f);
        }
    }
}
