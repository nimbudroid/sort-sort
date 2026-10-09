// Minimal NUnit stand-in used only by Tools/LogicTests/run.sh; Unity uses the real NUnit.
using System; using System.Linq; using System.Reflection;
static class Runner { static int Main(string[] a){
  var asm = Assembly.LoadFrom(a[0]); int pass=0, fail=0;
  foreach (var t in asm.GetTypes().OrderBy(t=>t.Name))
    foreach (var m in t.GetMethods().Where(m=>m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute),false).Length>0).OrderBy(m=>m.Name)) {
      try { m.Invoke(Activator.CreateInstance(t), null); pass++; Console.WriteLine("  PASS "+t.Name+"."+m.Name); }
      catch (TargetInvocationException e) { fail++; Console.WriteLine("  FAIL "+t.Name+"."+m.Name+": "+e.InnerException.Message); }
    }
  Console.WriteLine("passed "+pass+", failed "+fail); return fail==0?0:1;
}}
