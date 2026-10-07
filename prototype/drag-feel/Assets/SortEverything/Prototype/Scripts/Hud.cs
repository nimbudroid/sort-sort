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
        GUIStyle small, label, title, stamp, stampShadow, combo, button, bigButton, panelBg, value;
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
            small = new GUIStyle(GUI.skin.label) { font = f, fontSize = Px(12), wordWrap = true };
            small.normal.textColor = new Color(0.2f, 0.18f, 0.25f, 0.8f);
            label = new GUIStyle(GUI.skin.label) { font = f, fontSize = Px(15), alignment = TextAnchor.MiddleLeft };
            label.normal.textColor = Color.white;
            value = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };
            title = new GUIStyle(label) { fontSize = Px(18), fontStyle = FontStyle.Bold };
            stamp = new GUIStyle(GUI.skin.label) { font = f, fontSize = Px(54), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            stamp.normal.textColor = new Color32(0xE8, 0x3B, 0x2E, 0xFF);
            stampShadow = new GUIStyle(stamp);
            stampShadow.normal.textColor = new Color(0.2f, 0.05f, 0.05f, 0.35f);
            combo = new GUIStyle(stamp) { fontSize = Px(28) };
            combo.normal.textColor = new Color32(0xFF, 0x8A, 0x1F, 0xFF);
            button = new GUIStyle(GUI.skin.button) { font = f, fontSize = Px(14), fontStyle = FontStyle.Bold };
            bigButton = new GUIStyle(GUI.skin.button) { font = f, fontSize = Px(26), fontStyle = FontStyle.Bold };
            panelBg = new GUIStyle();
            panelBg.normal.background = white;

            GUI.skin.horizontalSlider.fixedHeight = 26f * u;
            GUI.skin.horizontalSliderThumb.fixedHeight = 26f * u;
            GUI.skin.horizontalSliderThumb.fixedWidth = 26f * u;
        }

        int Px(float dp) { return Mathf.RoundToInt(dp * u); }

        void OnGUI()
        {
            if (Proto.Director == null) return;
            EnsureStyles();
            var d = Proto.Director;
            Rect safe = SafeGui;

            // Combo edge glow at x5 (GDD ch. 01 §1.5).
            if (Proto.Config.juice && d.Combo >= 5 && Time.unscaledTime - d.ComboTime < 1.2f)
                DrawEdgeGlow(new Color(1f, 0.6f, 0.15f, 0.35f + 0.15f * Mathf.Sin(Time.unscaledTime * 10f)));

            GUI.Label(new Rect(safe.x + 12 * u, safe.y + 12 * u, 200 * u, 24 * u), "ROUND " + d.Round,
                new GUIStyle(small) { fontSize = Px(14), fontStyle = FontStyle.Bold });

            if (Proto.Config.debugOverlay && Proto.Drag != null && Proto.Drag.LastReleaseType != null)
                GUI.Label(new Rect(safe.x + 12 * u, safe.y + 34 * u, 320 * u, 24 * u),
                    "last release: " + Proto.Drag.LastReleaseType + " @ " + Proto.Drag.LastReleaseSpeedDp.ToString("F0") + " dp/s", small);

            // Combo pop-up above the bin.
            float since = Time.unscaledTime - d.ComboTime;
            if (Proto.Config.juice && d.Combo >= 2 && since < 0.6f)
            {
                Vector2 p = Units.WorldToGui(d.ComboWorldPos);
                var c = combo.normal.textColor;
                var style = new GUIStyle(combo);
                style.normal.textColor = new Color(c.r, c.g, c.b, 1f - since / 0.6f);
                GUI.Label(new Rect(p.x - 60 * u, p.y - 50 * u - since * 60 * u, 120 * u, 40 * u), "x" + d.Combo, style);
            }

            if (d.RoundComplete && d.StampTime > d.CompleteTime) DrawStamp(d);
            if (d.ShowNext) DrawNext(d);

            if (toast != null && Time.unscaledTime < toastUntil)
                GUI.Label(new Rect(0, safe.yMax - 80 * u, Screen.width, 30 * u), toast, new GUIStyle(title) { alignment = TextAnchor.MiddleCenter });

            if (!PanelOpen)
            {
                if (GUI.Button(GearRect, "•••", button)) { PanelOpen = true; scroll = Vector2.zero; }
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
            GUI.Label(new Rect(rect.x + 3 * u, rect.y + 4 * u, rect.width, rect.height), "SORTED!", stampShadow);
            GUI.Label(rect, "SORTED!", stamp);
            GUI.matrix = m;
        }

        void DrawNext(RoundDirector d)
        {
            float y = Units.WorldToGui(new Vector2(0f, (d.TableTop + d.BinsTop) / 2f)).y;
            float w = Screen.width * 0.62f, h = 64f * u;
            if (GUI.Button(new Rect((Screen.width - w) / 2f, y - h / 2f, w, h), "NEXT  >", bigButton)) d.Next();
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

        // ---- tuning panel --------------------------------------------------------------------

        void DrawPanel()
        {
            Rect safe = SafeGui;
            var old = GUI.color;
            GUI.color = new Color(0.08f, 0.07f, 0.12f, 0.93f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, panelBg);
            GUI.color = old;

            float pad = 14f * u;
            float row = 38f * u;
            var area = new Rect(safe.x + pad, safe.y + pad, safe.width - 2 * pad, safe.height - 2 * pad);
            var view = new Rect(0, 0, area.width, Mathf.Max(viewHeight, area.height));
            scroll = GUI.BeginScrollView(area, scroll, view, false, false, GUIStyle.none, GUIStyle.none);

            float y = 0f;
            float w = area.width;
            GUI.Label(new Rect(0, y, w, row), "DRAG-FEEL TUNING  (observer only)", title);
            y += row;

            // E1 variants.
            GUI.Label(new Rect(0, y, w, row * 0.7f), "E1 variant  (current: " + Proto.Variant + (Proto.CustomTuning ? ", custom" : "") + ")", label);
            y += row * 0.75f;
            string[] names = { "A spec", "B no lift", "C no lag", "D neither" };
            float bw = w / 4f;
            for (int i = 0; i < 4; i++)
            {
                bool selected = (int)Proto.Variant == i && !Proto.CustomTuning;
                var s = new GUIStyle(button);
                if (selected) s.normal.textColor = new Color(1f, 0.75f, 0.2f);
                if (GUI.Button(new Rect(i * bw + 2, y, bw - 4, row - 4), names[i], s)) SelectVariant((FeelVariant)i);
            }
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
            float statsH = small.CalcHeight(new GUIContent(stats), w) + 8 * u;
            var statStyle = new GUIStyle(small);
            statStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
            GUI.Label(new Rect(0, y, w, statsH), stats, statStyle);
            y += statsH;
            string path = "Logs: " + Proto.Telemetry.FolderPath;
            float pathH = small.CalcHeight(new GUIContent(path), w) + 6 * u;
            GUI.Label(new Rect(0, y, w, pathH), path, statStyle);
            y += pathH + 4 * u;

            float aw = w / 2f;
            if (GUI.Button(new Rect(0, y, aw - 4, row - 4), "New round", button)) { Close(); Proto.Director.StartRound(); }
            if (GUI.Button(new Rect(aw, y, aw - 4, row - 4), "Reset stats", button)) { Proto.Telemetry.ResetStats(); Toast("Stats reset"); }
            y += row;
            if (GUI.Button(new Rect(0, y, aw - 4, row - 4), "Copy summary", button))
            {
                GUIUtility.systemCopyBuffer = Proto.Telemetry.SummaryJson();
                Toast("Summary copied");
            }
            if (GUI.Button(new Rect(aw, y, aw - 4, row - 4), "Close", button)) Close();
            y += row;

            viewHeight = y + pad;
            GUI.EndScrollView();
        }

        bool Slider(ref float y, float w, float row, string name, ref float v, float min, float max, string fmt, string unit)
        {
            float labelW = w * 0.42f;
            GUI.Label(new Rect(0, y, labelW * 0.62f, row), name, label);
            GUI.Label(new Rect(labelW * 0.55f, y, labelW * 0.45f, row), v.ToString(fmt) + unit, value);
            float nv = GUI.HorizontalSlider(new Rect(labelW + 8 * u, y + (row - 26 * u) / 2f, w - labelW - 12 * u, 26 * u), v, min, max);
            y += row;
            if (Mathf.Approximately(nv, v)) return false;
            v = nv;
            return true;
        }

        bool Toggle(Rect r, string name, ref bool v)
        {
            var s = new GUIStyle(button);
            if (v) s.normal.textColor = new Color(0.5f, 1f, 0.6f);
            if (!GUI.Button(r, name + (v ? ": ON" : ": off"), s)) return false;
            v = !v;
            return true;
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
