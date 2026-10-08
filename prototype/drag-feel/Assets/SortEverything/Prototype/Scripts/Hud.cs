using UnityEngine;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Minimal in-game HUD (round number, combo, "SORTED!" stamp, NEXT) plus the observer's tuning panel.
    /// Testers never see which E1 variant they are in; the panel (gear button, top-right) is for the observer.
    /// Drawn with IMGUI so the prototype needs no UI packages or prefabs.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        static Font font;
        public static Font BuiltinFont
        {
            get
            {
                if (font == null)
                {
#if UNITY_2022_2_OR_NEWER
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                }
                return font;
            }
        }

        public bool PanelOpen { get; private set; }

        float u; // pixels per dp
        GUIStyle small, label, title, stamp, combo, button, bigButton, panelBg, value, hudLabel, stats;
        // Visual-style animation clocks (pop-ins); presentation only.
        int lastRound = -1;
        float roundPopAt = -10f, nextPopAt = -10f, toastPopAt = -10f;
        bool nextWasShown;
        Texture2D white, glowV, glowH;
        Vector2 scroll;
        float viewHeight;
        string toast;
        float toastUntil;
        bool scrolling;
        Material glMaterial;

        void Awake()
        {
            white = Texture2D.whiteTexture;
            glowV = new Texture2D(1, 32, TextureFormat.RGBA32, false);
            glowH = new Texture2D(32, 1, TextureFormat.RGBA32, false);
            for (int i = 0; i < 32; i++)
            {
                float a = Mathf.Pow(i / 31f, 2f);
                glowV.SetPixel(0, i, new Color(1f, 1f, 1f, a));       // texture row 31 = top
                glowH.SetPixel(31 - i, 0, new Color(1f, 1f, 1f, a));  // column 0 = left
            }
            glowV.wrapMode = glowH.wrapMode = TextureWrapMode.Clamp;
            glowV.Apply();
            glowH.Apply();
        }

        Rect SafeGui
        {
            get
            {
                Rect s = Screen.safeArea;
                return new Rect(s.x, Screen.height - s.yMax, s.width, s.height);
            }
        }

        Rect GearRect
        {
            get
            {
                Rect safe = SafeGui;
                float size = 44f * Units.DpToPx(1f);
                float pad = 8f * Units.DpToPx(1f);
                return new Rect(safe.xMax - size - pad, safe.y + pad, size, size);
            }
        }

        /// <summary>Screen position (origin bottom-left) over a HUD control?</summary>
        public bool IsOverUi(Vector2 screen)
        {
            if (PanelOpen) return true;
            var gui = new Vector2(screen.x, Screen.height - screen.y);
            Rect gear = GearRect;
            float grow = 8f * Units.DpToPx(1f);
            gear.xMin -= grow; gear.yMin -= grow; gear.xMax += grow; gear.yMax += grow;
            return gear.Contains(gui);
        }

        void Update()
        {
            // IMGUI scroll views don't drag-scroll on touch screens; do it by hand for the panel.
            if (!PanelOpen || Input.touchCount != 1) { scrolling = false; return; }
            Touch t = Input.GetTouch(0);
            float labelColumn = Screen.width * 0.42f;
            if (t.phase == TouchPhase.Began) scrolling = t.position.x < labelColumn;
            else if (scrolling && t.phase == TouchPhase.Moved)
                scroll.y = Mathf.Clamp(scroll.y + t.deltaPosition.y, 0f, Mathf.Max(0f, viewHeight - Screen.height * 0.8f));
        }

        void EnsureStyles()
        {
            float nu = Units.DpToPx(1f);
            if (small != null && Mathf.Approximately(nu, u)) return;
            u = nu;
            var f = BuiltinFont;
            var display = ToyStyle.Display;
            var body = ToyStyle.Body;
            small = new GUIStyle(GUI.skin.label) { font = body, fontSize = Px(12), wordWrap = true };
            small.normal.textColor = new Color(0.18f, 0.14f, 0.2f, 0.9f);
            label = new GUIStyle(GUI.skin.label) { font = body, fontSize = Px(13), alignment = TextAnchor.MiddleLeft };
            label.normal.textColor = Color.white;
            value = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };
            value.normal.textColor = ToyStyle.Reward;
            title = new GUIStyle(label) { font = display, fontSize = Px(18), alignment = TextAnchor.MiddleLeft };
            stamp = new GUIStyle(GUI.skin.label) { font = display, fontSize = Px(56), alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
            stamp.normal.textColor = ToyStyle.Reward;
            combo = new GUIStyle(stamp) { fontSize = Px(30) };
            combo.normal.textColor = ToyStyle.Reward;
            button = new GUIStyle(GUI.skin.label) { font = display, fontSize = Px(14), alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
            bigButton = new GUIStyle(button) { fontSize = Px(28) };
            hudLabel = new GUIStyle(button) { fontSize = Px(15) };
            stats = new GUIStyle(GUI.skin.label) { font = f, fontSize = Px(12), wordWrap = true };
            stats.normal.textColor = new Color(0.9f, 0.88f, 1f);
            panelBg = new GUIStyle();
            panelBg.normal.background = white;

            ToyGui.SkinSliders(GUI.skin);
        }

        int Px(float dp) { return Mathf.RoundToInt(dp * u); }

        void OnGUI()
        {
            if (Proto.Director == null) return;
            ToyGui.Begin(Units.DpToPx(1f));
            EnsureStyles();
            var d = Proto.Director;
            Rect safe = SafeGui;
            if (d.Round != lastRound) { lastRound = d.Round; roundPopAt = Time.unscaledTime; }
            if (d.ShowNext && !nextWasShown) nextPopAt = Time.unscaledTime;
            nextWasShown = d.ShowNext;

            // Combo edge glow at x5 (GDD ch. 01 §1.5).
            if (Proto.Config.juice && d.Combo >= 5 && Time.unscaledTime - d.ComboTime < 1.2f)
                DrawEdgeGlow(new Color(1f, 0.6f, 0.15f, 0.35f + 0.15f * Mathf.Sin(Time.unscaledTime * 10f)));

            {
                string roundText = "ROUND " + d.Round;
                float pw = hudLabel.CalcSize(new GUIContent(roundText)).x + 30 * u;
                var pill = new Rect(safe.x + 10 * u, safe.y + 10 * u, pw, 36 * u);
                float pop = Pop(roundPopAt, 0.35f, 1.18f);
                Matrix4x4 pm = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), pill.center);
                ToyGui.Pill(pill, ToyTone.Paper);
                ToyGui.Text(new Rect(pill.x, pill.y - ToyGui.ButtonDepth * 0.5f, pill.width, pill.height), roundText, hudLabel,
                    ToyStyle.Purple, 1.6f, 0f);
                GUI.matrix = pm;
            }

            if (Proto.Config.debugOverlay && Proto.Drag != null && Proto.Drag.LastReleaseType != null)
                GUI.Label(new Rect(safe.x + 12 * u, safe.y + 50 * u, 320 * u, 24 * u),
                    "last release: " + Proto.Drag.LastReleaseType + " @ " + Proto.Drag.LastReleaseSpeedDp.ToString("F0") + " dp/s", small);

            // Combo pop-up above the bin.
            float since = Time.unscaledTime - d.ComboTime;
            if (Proto.Config.juice && d.Combo >= 2 && since < 0.6f)
            {
                Vector2 p = Units.WorldToGui(d.ComboWorldPos);
                var c = combo.normal.textColor;
                var r = new Rect(p.x - 60 * u, p.y - 50 * u - since * 60 * u, 120 * u, 40 * u);
                float pop = Mathf.Lerp(1.7f, 1f, Juice.EaseOutBack(Mathf.Clamp01(since / 0.16f)));
                Matrix4x4 cm = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), r.center);
                ToyGui.Text(r, "x" + d.Combo, combo, new Color(c.r, c.g, c.b, 1f - since / 0.6f), 2.6f, 2.6f);
                GUI.matrix = cm;
            }

            if (d.RoundComplete && d.StampTime > d.CompleteTime) DrawStamp(d);
            if (d.ShowNext) DrawNext(d);

            if (Proto.Config.debugOverlay && !PanelOpen)
            {
                for (int i = 0; i < d.Objects.Count; i++)
                {
                    var o = d.Objects[i];
                    if (!o.gameObject.activeInHierarchy || o.state == ObjState.Sorted) continue;
                    Vector2 g = Units.WorldToGui(o.Position + Vector2.down * o.HalfExtent);
                    ToyGui.Text(new Rect(g.x - 60 * u, g.y, 120 * u, 18 * u), o.displayName,
                        new GUIStyle(small) { alignment = TextAnchor.UpperCenter, fontSize = Px(10) }, Color.white, 1.2f, 0f);
                }
            }

            if (toast != null && Time.unscaledTime < toastUntil)
            {
                var ts = new GUIStyle(hudLabel) { fontSize = Px(16) };
                float tw = ts.CalcSize(new GUIContent(toast)).x + 36 * u;
                var tr = new Rect((Screen.width - tw) / 2f, safe.yMax - 86 * u, tw, 42 * u);
                float pop = Pop(toastPopAt, 0.3f, 0.7f);
                Matrix4x4 tm = GUI.matrix;
                GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), tr.center);
                ToyGui.Pill(tr, ToyTone.Purple);
                ToyGui.Text(new Rect(tr.x, tr.y - ToyGui.ButtonDepth * 0.5f, tr.width, tr.height), toast, ts, Color.white, 1.8f, 1.2f);
                GUI.matrix = tm;
            }

            if (!PanelOpen)
            {
                if (ToyGui.Button(GearRect, "•••", ToyTone.Purple, button)) { PanelOpen = true; scroll = Vector2.zero; }
            }
            else
            {
                DrawPanel();
            }
        }

        void DrawStamp(RoundDirector d)
        {
            float t = Time.unscaledTime - d.StampTime;
            float scale = Proto.Config.juice ? Mathf.Lerp(2f, 1f, Juice.EaseOutBack(Mathf.Clamp01(t / 0.18f))) : 1f;
            Vector2 centre = Units.WorldToGui(new Vector2(0f, d.TableTop + 2.4f));
            var rect = new Rect(centre.x - 200 * u, centre.y - 40 * u, 400 * u, 80 * u);
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(-8f, centre);
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), centre);
            ToyGui.Text(rect, "SORTED!", stamp, stamp.normal.textColor, 3.6f, 5f);
            GUI.matrix = m;
        }

        void DrawNext(RoundDirector d)
        {
            float y = Units.WorldToGui(new Vector2(0f, (d.TableTop + d.BinsTop) / 2f)).y;
            float w = Screen.width * 0.62f, h = 70f * u;
            var r = new Rect((Screen.width - w) / 2f, y - h / 2f, w, h);
            float pop = Pop(nextPopAt, 0.35f, 0.6f);
            Matrix4x4 m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), r.center);
            bool next = ToyGui.Button(r, "NEXT  >", ToyTone.Primary, bigButton);
            GUI.matrix = m;
            if (next) d.Next();
        }

        void DrawEdgeGlow(Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            float t = 28f * u;
            // glowV is strongest at its top row, glowH at its left column; flip texcoords for the opposite edges.
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, Screen.width, t), glowV, new Rect(0, 0, 1, 1));
            GUI.DrawTextureWithTexCoords(new Rect(0, Screen.height - t, Screen.width, t), glowV, new Rect(0, 1, 1, -1));
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, t, Screen.height), glowH, new Rect(0, 0, 1, 1));
            GUI.DrawTextureWithTexCoords(new Rect(Screen.width - t, 0, t, Screen.height), glowH, new Rect(1, 0, -1, 1));
            GUI.color = old;
        }

        /// <summary>UI pop-in: scale from `from` to 1 with a little overshoot over `seconds` after `start`.</summary>
        float Pop(float start, float seconds, float from)
        {
            if (!Proto.Config.juice) return 1f;
            float t = Mathf.Clamp01((Time.unscaledTime - start) / seconds);
            return Mathf.LerpUnclamped(from, 1f, Juice.EaseOutBack(t));
        }

        // ---- tuning panel --------------------------------------------------------------------

        void DrawPanel()
        {
            Rect safe = SafeGui;
            var old = GUI.color;
            GUI.color = new Color(0.12f, 0.08f, 0.2f, 0.55f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, panelBg);
            GUI.color = old;

            float pad = 14f * u;
            float row = 38f * u;
            float cardInset = 6f * u;
            ToyGui.Card(new Rect(safe.x + cardInset, safe.y + cardInset, safe.width - 2 * cardInset, safe.height - 2 * cardInset));
            var area = new Rect(safe.x + pad + 6 * u, safe.y + pad + 4 * u, safe.width - 2 * pad - 12 * u, safe.height - 2 * pad - 18 * u);
            var view = new Rect(0, 0, area.width, Mathf.Max(viewHeight, area.height));
            scroll = GUI.BeginScrollView(area, scroll, view, false, false, GUIStyle.none, GUIStyle.none);

            float y = 0f;
            float w = area.width;
            ToyGui.Text(new Rect(0, y, w, row), "DRAG-FEEL TUNING  (observer only)", title, Color.white, 2f, 1.5f);
            y += row;

            // E1 variants.
            GUI.Label(new Rect(0, y, w, row * 0.7f), "E1 variant  (current: " + Proto.Variant + (Proto.CustomTuning ? ", custom" : "") + ")", label);
            y += row * 0.75f;
            string[] names = { "A spec", "B no lift", "C no lag", "D neither" };
            float bw = w / 4f;
            for (int i = 0; i < 4; i++)
            {
                bool selected = (int)Proto.Variant == i && !Proto.CustomTuning;
                if (ToyGui.Button(new Rect(i * bw + 2, y, bw - 4, row - 4), names[i], selected ? ToyTone.Secondary : ToyTone.Inactive, button))
                    SelectVariant((FeelVariant)i);
            }
            y += row + 6 * u;

            // Content: what the rounds are made of. Switching starts a new round straight away.
            GUI.Label(new Rect(0, y, w, row * 0.7f), "Content  (round: " + ContentSettings.ModeLabel(Proto.Director.RoundMode) + ")", label);
            y += row * 0.75f;
            float cw = w / 4f;
            for (int i = 0; i < 3; i++)
            {
                var m = (ContentMode)i;
                if (ToyGui.Button(new Rect(i * cw + 2, y, cw - 4, row - 4), ContentSettings.ModeLabel(m),
                        ContentSettings.Mode == m ? ToyTone.Purple : ToyTone.Inactive, button))
                    SelectContent(m, ContentSettings.Pool);
            }
            if (ToyGui.Button(new Rect(3 * cw + 2, y, cw - 4, row - 4), ContentSettings.PoolLabel(ContentSettings.Pool), ToyTone.Secondary, button))
                SelectContent(ContentSettings.Mode, (ObjectPool)(((int)ContentSettings.Pool + 1) % 3));
            y += row + 6 * u;

            var c = Proto.Config;
            bool changed = false;
            changed |= Slider(ref y, w, row, "Lift offset", ref c.liftOffsetDp, 0f, 80f, "F0", " dp");
            changed |= Slider(ref y, w, row, "Follow (mass 1)", ref c.followLightMs, 5f, 150f, "F0", " ms");
            changed |= Slider(ref y, w, row, "Follow (mass 5)", ref c.followHeavyMs, 5f, 300f, "F0", " ms");
            changed |= Slider(ref y, w, row, "Sag (mass 5)", ref c.sagMass5Dp, 0f, 30f, "F0", " dp");
            changed |= Slider(ref y, w, row, "Sag (mass 4)", ref c.sagMass4Dp, 0f, 30f, "F0", " dp");
            changed |= Slider(ref y, w, row, "Pickup scale", ref c.pickupScale, 1f, 1.4f, "F2", "x");
            changed |= Slider(ref y, w, row, "Hit padding", ref c.hitPaddingDp, 0f, 30f, "F0", " dp");
            changed |= Slider(ref y, w, row, "Drop assist", ref c.dropAssistDp, 0f, 60f, "F0", " dp");
            changed |= Slider(ref y, w, row, "Flick threshold", ref c.flickThresholdDpS, 500f, 3000f, "F0", " dp/s");
            changed |= Slider(ref y, w, row, "Throw scale", ref c.throwScale, 0.4f, 1.3f, "F2", "x");
            changed |= Slider(ref y, w, row, "Aim assist", ref c.aimAssistDp, 0f, 120f, "F0", " dp");
            changed |= Slider(ref y, w, row, "Max tilt", ref c.maxTiltDeg, 0f, 30f, "F0", "°");

            float tw = w / 3f;
            changed |= Toggle(new Rect(0, y, tw - 4, row - 4), "Weight lag", ref c.weightLag);
            Toggle(new Rect(tw, y, tw - 4, row - 4), "Juice", ref c.juice);
            Toggle(new Rect(tw * 2, y, tw - 4, row - 4), "Sound", ref c.sound);
            y += row;
            Toggle(new Rect(0, y, tw - 4, row - 4), "Haptics", ref c.haptics);
            Toggle(new Rect(tw, y, tw - 4, row - 4), "Debug", ref c.debugOverlay);
            y += row + 6 * u;

            if (changed && !Proto.CustomTuning)
            {
                Proto.CustomTuning = true;
                Proto.Telemetry.OnVariantChanged(Proto.Variant + "*");
            }

            // Session stats.
            string stats = Proto.Telemetry.SummaryText();
            float statsH = this.stats.CalcHeight(new GUIContent(stats), w) + 8 * u;
            var statStyle = this.stats;
            GUI.Label(new Rect(0, y, w, statsH), stats, statStyle);
            y += statsH;
            string path = "Logs: " + Proto.Telemetry.FolderPath;
            float pathH = this.stats.CalcHeight(new GUIContent(path), w) + 6 * u;
            GUI.Label(new Rect(0, y, w, pathH), path, statStyle);
            y += pathH + 4 * u;

            float aw = w / 2f;
            if (ToyGui.Button(new Rect(0, y, aw - 4, row - 4), "New round", ToyTone.Primary, button)) { Close(); Proto.Director.StartRound(); }
            if (ToyGui.Button(new Rect(aw, y, aw - 4, row - 4), "Reset stats", ToyTone.Warning, button)) { Proto.Telemetry.ResetStats(); Toast("Stats reset"); }
            y += row;
            if (ToyGui.Button(new Rect(0, y, aw - 4, row - 4), "Copy summary", ToyTone.Secondary, button))
            {
                GUIUtility.systemCopyBuffer = Proto.Telemetry.SummaryJson();
                Toast("Summary copied");
            }
            if (ToyGui.Button(new Rect(aw, y, aw - 4, row - 4), "Close", ToyTone.Purple, button)) Close();
            y += row;

            viewHeight = y + pad;
            GUI.EndScrollView();
        }

        bool Slider(ref float y, float w, float row, string name, ref float v, float min, float max, string fmt, string unit)
        {
            float labelW = w * 0.42f;
            GUI.Label(new Rect(0, y, labelW * 0.62f, row), name, label);
            GUI.Label(new Rect(labelW * 0.55f, y, labelW * 0.45f, row), v.ToString(fmt) + unit, value);
            float nv = GUI.HorizontalSlider(new Rect(labelW + 8 * u, y + (row - 16 * u) / 2f, w - labelW - 22 * u, 16 * u), v, min, max);
            y += row;
            if (Mathf.Approximately(nv, v)) return false;
            v = nv;
            return true;
        }

        bool Toggle(Rect r, string name, ref bool v)
        {
            if (!ToyGui.Button(r, name + (v ? ": ON" : ": off"), v ? ToyTone.Primary : ToyTone.Inactive, button)) return false;
            v = !v;
            return true;
        }

        void SelectContent(ContentMode mode, ObjectPool pool)
        {
            ContentSettings.Mode = mode;
            ContentSettings.Pool = pool;
            ContentSettings.Save();
            Proto.Telemetry.OnContentChanged(ContentSettings.ModeLabel(mode) + "/" + pool);
            Proto.Director.StartRound();
            Toast(ContentSettings.ModeLabel(mode) + " · " + ContentSettings.PoolLabel(pool));
        }

        void SelectVariant(FeelVariant v)
        {
            var old = Proto.Config;
            var c = FeelConfig.ForVariant(v);
            c.juice = old.juice;
            c.sound = old.sound;
            c.haptics = old.haptics;
            c.debugOverlay = old.debugOverlay;
            Proto.Config = c;
            Proto.Variant = v;
            Proto.CustomTuning = false;
            FeelConfig.Save(v, c, false);
            Proto.Telemetry.OnVariantChanged(v.ToString());
            Toast("Variant " + v);
        }

        void Close()
        {
            PanelOpen = false;
            FeelConfig.Save(Proto.Variant, Proto.Config, Proto.CustomTuning);
        }

        void Toast(string text)
        {
            toast = text;
            toastUntil = Time.unscaledTime + 1.5f;
            toastPopAt = Time.unscaledTime;
        }

        // ---- debug overlay (world space) -----------------------------------------------------

        void OnRenderObject()
        {
            if (Proto.Config == null || !Proto.Config.debugOverlay || Proto.Director == null) return;
            if (Camera.current != Proto.Cam) return;
            if (glMaterial == null)
            {
                glMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
                glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                glMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                glMaterial.SetInt("_ZWrite", 0);
            }
            glMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            var d = Proto.Director;
            float assist = Units.DpToWorld(Proto.Config.dropAssistDp);
            for (int i = 0; i < d.Bins.Count; i++)
            {
                var b = d.Bins[i];
                GL.Color(new Color(1f, 0.85f, 0.1f, 0.8f));
                Rect(b.InnerLeft - assist, b.Bottom, b.InnerRight + assist, d.TableTop);
                GL.Color(new Color(0.2f, 1f, 0.3f, 0.9f));
                Line(new Vector2(b.InnerLeft, b.Top), new Vector2(b.InnerRight, b.Top));
            }
            float pad = Units.DpToWorld(Proto.Config.hitPaddingDp);
            GL.Color(new Color(0.2f, 0.9f, 1f, 0.6f));
            for (int i = 0; i < d.Objects.Count; i++)
            {
                var o = d.Objects[i];
                if (o.Grabbable) Circle(o.Position, o.HalfExtent + pad);
            }
            GL.Color(new Color(1f, 0.3f, 0.8f, 0.7f));
            Line(new Vector2(-50f, d.ContainerZoneTop), new Vector2(50f, d.ContainerZoneTop));

            GL.End();
            GL.PopMatrix();
        }

        static void Line(Vector2 a, Vector2 b)
        {
            GL.Vertex3(a.x, a.y, 0f);
            GL.Vertex3(b.x, b.y, 0f);
        }

        static void Rect(float x0, float y0, float x1, float y1)
        {
            Line(new Vector2(x0, y0), new Vector2(x1, y0));
            Line(new Vector2(x1, y0), new Vector2(x1, y1));
            Line(new Vector2(x1, y1), new Vector2(x0, y1));
            Line(new Vector2(x0, y1), new Vector2(x0, y0));
        }

        static void Circle(Vector2 c, float r)
        {
            const int n = 20;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                Line(c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r);
            }
        }
    }
}
