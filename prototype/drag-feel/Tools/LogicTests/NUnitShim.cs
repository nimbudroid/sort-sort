// Minimal stand-in for the NUnit API used by the EditMode tests, so they can run under plain mono here.
using System;
using System.Collections;
namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)] public class TestAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class TestFixtureAttribute : Attribute { }
    public class AssertionException : Exception { public AssertionException(string m) : base(m) { } }
    public static class Assert
    {
        static void Fail(string what, string msg) { throw new AssertionException(what + (msg != null ? " | " + msg : "")); }
        public static void Fail(string msg) { Fail("Fail", msg); }
        public static void IsTrue(bool c, string msg = null) { if (!c) Fail("expected true", msg); }
        public static void IsFalse(bool c, string msg = null) { if (c) Fail("expected false", msg); }
        public static void IsNotNull(object o, string msg = null) { if (o == null) Fail("expected not null", msg); }
        public static void IsNull(object o, string msg = null) { if (o != null) Fail("expected null", msg); }
        public static void AreSame(object e, object a, string msg = null) { if (!ReferenceEquals(e, a)) Fail("expected same instance", msg); }
        public static void AreEqual(object e, object a, string msg = null) { if (!Equals(e, a)) Fail("expected <" + e + "> but was <" + a + ">", msg); }
        public static void AreEqual(double e, double a, double delta, string msg = null) { if (Math.Abs(e - a) > delta) Fail("expected " + e + " +/- " + delta + " but was " + a, msg); }
        public static void Greater(int a, int b, string msg = null) { if (!(a > b)) Fail("expected " + a + " > " + b, msg); }
        public static T Throws<T>(Action code) where T : Exception
        {
            try { code(); }
            catch (T e) { return e; }
            catch (Exception e) { Fail("expected " + typeof(T).Name + " but got " + e.GetType().Name, null); }
            Fail("expected " + typeof(T).Name + " but nothing was thrown", null);
            return null;
        }
    }
    public static class CollectionAssert
    {
        static int Count(IEnumerable c) { int n = 0; foreach (var _ in c) n++; return n; }
        public static void IsEmpty(IEnumerable c, string msg = null) { if (Count(c) != 0) throw new AssertionException("expected empty but had " + Count(c) + (msg != null ? " | " + msg : "")); }
        public static void IsNotEmpty(IEnumerable c, string msg = null) { if (Count(c) == 0) throw new AssertionException("expected not empty" + (msg != null ? " | " + msg : "")); }
        public static void AreEqual(IEnumerable e, IEnumerable a, string msg = null)
        {
            var x = e.GetEnumerator(); var y = a.GetEnumerator();
            while (true)
            {
                bool mx = x.MoveNext(), my = y.MoveNext();
                if (mx != my || (mx && !Equals(x.Current, y.Current))) throw new AssertionException("collections differ" + (msg != null ? " | " + msg : ""));
                if (!mx) return;
            }
        }
    }
}
