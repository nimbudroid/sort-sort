// Minimal stand-in for the NUnit API used by the EditMode tests, so they can run under plain mono here.
using System;
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
    }
}
