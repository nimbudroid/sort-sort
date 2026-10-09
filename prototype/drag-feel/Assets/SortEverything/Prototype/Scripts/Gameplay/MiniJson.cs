using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SortEverything.Prototype
{
    /// <summary>
    /// Minimal JSON reader/writer (objects -> Dictionary&lt;string, object&gt;, arrays -> List&lt;object&gt;, numbers -> double).
    /// Plain C# so the catalogue and save data load identically in Unity and in EditMode tests.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            var p = new Parser(json);
            p.SkipWs();
            var v = p.Value();
            p.SkipWs();
            if (!p.End) throw p.Error("trailing characters");
            return v;
        }

        public static string Write(object value, bool pretty = false)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value, pretty, 0);
            return sb.ToString();
        }

        sealed class Parser
        {
            readonly string s;
            int i;
            public Parser(string s) { this.s = s ?? ""; }
            public bool End { get { return i >= s.Length; } }
            public FormatException Error(string m) { return new FormatException("JSON: " + m + " at " + i); }

            public void SkipWs()
            {
                while (i < s.Length)
                {
                    char c = s[i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') { i++; continue; }
                    if (c == '/' && i + 1 < s.Length && s[i + 1] == '/') { while (i < s.Length && s[i] != '\n') i++; continue; } // allow // comments in authored data
                    break;
                }
            }

            public object Value()
            {
                if (End) throw Error("unexpected end");
                char c = s[i];
                if (c == '{') return Obj();
                if (c == '[') return Arr();
                if (c == '"') return Str();
                if (c == 't' && s.Substring(i).StartsWith("true")) { i += 4; return true; }
                if (c == 'f' && s.Substring(i).StartsWith("false")) { i += 5; return false; }
                if (c == 'n' && s.Substring(i).StartsWith("null")) { i += 4; return null; }
                return Num();
            }

            Dictionary<string, object> Obj()
            {
                var d = new Dictionary<string, object>();
                i++;
                SkipWs();
                if (i < s.Length && s[i] == '}') { i++; return d; }
                while (true)
                {
                    SkipWs();
                    if (End || s[i] != '"') throw Error("expected key");
                    string k = Str();
                    SkipWs();
                    if (End || s[i] != ':') throw Error("expected ':'");
                    i++;
                    SkipWs();
                    d[k] = Value();
                    SkipWs();
                    if (End) throw Error("unterminated object");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return d; }
                    throw Error("expected ',' or '}'");
                }
            }

            List<object> Arr()
            {
                var l = new List<object>();
                i++;
                SkipWs();
                if (i < s.Length && s[i] == ']') { i++; return l; }
                while (true)
                {
                    SkipWs();
                    l.Add(Value());
                    SkipWs();
                    if (End) throw Error("unterminated array");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return l; }
                    throw Error("expected ',' or ']'");
                }
            }

            string Str()
            {
                var sb = new StringBuilder();
                i++;
                while (true)
                {
                    if (End) throw Error("unterminated string");
                    char c = s[i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (End) throw Error("bad escape");
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length) throw Error("bad unicode escape");
                            sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                            i += 4;
                            break;
                        default: throw Error("bad escape");
                    }
                }
            }

            object Num()
            {
                int start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                if (start == i) throw Error("unexpected character '" + s[i] + "'");
                return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

        static void WriteValue(StringBuilder sb, object v, bool pretty, int depth)
        {
            if (v == null) { sb.Append("null"); return; }
            if (v is string) { WriteString(sb, (string)v); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is int || v is long) { sb.Append(Convert.ToInt64(v).ToString(CultureInfo.InvariantCulture)); return; }
            if (v is float || v is double) { sb.Append(Convert.ToDouble(v).ToString("R", CultureInfo.InvariantCulture)); return; }
            var dict = v as IDictionary<string, object>;
            if (dict != null)
            {
                sb.Append('{');
                bool first = true;
                var keys = new List<string>(dict.Keys);
                keys.Sort(StringComparer.Ordinal);
                foreach (var k in keys)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    if (pretty) { sb.Append('\n'); sb.Append(' ', (depth + 1) * 2); }
                    WriteString(sb, k);
                    sb.Append(pretty ? ": " : ":");
                    WriteValue(sb, dict[k], pretty, depth + 1);
                }
                if (pretty && !first) { sb.Append('\n'); sb.Append(' ', depth * 2); }
                sb.Append('}');
                return;
            }
            var list = v as System.Collections.IEnumerable;
            if (list != null)
            {
                sb.Append('[');
                bool first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteValue(sb, item, pretty, depth + 1);
                }
                sb.Append(']');
                return;
            }
            throw new ArgumentException("MiniJson cannot write " + v.GetType());
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ---- typed accessors for loaders ------------------------------------------------------

        public static string Str(IDictionary<string, object> d, string key, string fallback = null)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) && v is string ? (string)v : fallback;
        }

        public static int Int(IDictionary<string, object> d, string key, int fallback = 0)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) && v is double ? (int)Math.Round((double)v) : fallback;
        }

        public static float Float(IDictionary<string, object> d, string key, float fallback = 0f)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) && v is double ? (float)(double)v : fallback;
        }

        public static bool Bool(IDictionary<string, object> d, string key, bool fallback = false)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) && v is bool ? (bool)v : fallback;
        }

        public static List<object> List(IDictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v as List<object> : null;
        }

        public static IDictionary<string, object> Obj(IDictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v as IDictionary<string, object> : null;
        }

        public static string[] Strings(IDictionary<string, object> d, string key)
        {
            var l = List(d, key);
            if (l == null) return new string[0];
            var a = new string[l.Count];
            for (int i = 0; i < l.Count; i++) a[i] = l[i] as string;
            return a;
        }
    }
}
