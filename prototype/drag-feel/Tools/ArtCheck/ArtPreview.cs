// Bakes every library object the way ObjectArt does (outside Unity) and writes raw RGBA for preview_sheet.py.
//   ArtPreview.exe <outDir> <Flat|Painted|Molded> [res=160]
// Writes <id>.rgba (res x res, row 0 = bottom), <id>.shadow.rgba (ToyShading.ShadowRes, Molded only),
// index.txt (id|name|tier|category|family) and timing.txt.
// Build (from Assets/SortEverything/Prototype/Scripts/Content):
//   mcs -out:artpreview.exe ObjectDefs.cs VectorPainter.cs VectorPainter.Molded.cs ToyShading.cs ObjectMaterials.cs
//       ObjectLibrary.cs ObjectDrawings.cs ../../../../../Tools/ArtCheck/ArtPreview.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SortEverything.Prototype;

static class ArtPreview
{
    static void Main(string[] args)
    {
        string dir = args[0];
        ToyShading.Mode = (ToyRenderMode)Enum.Parse(typeof(ToyRenderMode), args[1]);
        int res = args.Length > 2 ? int.Parse(args[2]) : 160;
        Directory.CreateDirectory(dir);

        // Warm up the JIT so timings measure baking, not compilation.
        foreach (var d in ObjectLibrary.All) { var w = ObjectDrawings.Paint(d.id); if (w != null) { ObjectMaterials.Configure(w, d); w.Rasterize(24); } }

        var times = new List<double>();
        double total = 0, worst = 0;
        string worstId = "";
        long bytes = 0;
        using (var idx = new StreamWriter(Path.Combine(dir, "index.txt")))
            foreach (var d in ObjectLibrary.All)
            {
                var p = ObjectDrawings.Paint(d.id);
                if (p == null) continue;
                ObjectMaterials.Configure(p, d);
                var sw = Stopwatch.StartNew();
                byte[] rgba = p.Rasterize(res);
                byte[] shadow = null;
                byte[] silhouette = p.RasterizeSilhouette(res); // ObjectArt computes this anyway for the hull
                if (ToyShading.Mode == ToyRenderMode.Molded) shadow = ToyShading.SoftShadow(silhouette, res);
                sw.Stop();
                double ms = sw.Elapsed.TotalMilliseconds;
                times.Add(ms); total += ms;
                if (ms > worst) { worst = ms; worstId = d.id; }
                File.WriteAllBytes(Path.Combine(dir, d.id + ".rgba"), rgba);
                bytes += rgba.Length;
                if (shadow != null) { File.WriteAllBytes(Path.Combine(dir, d.id + ".shadow.rgba"), shadow); bytes += shadow.Length; }
                idx.WriteLine(d.id + "|" + d.displayName + "|" + d.tier + "|" + d.category + "|" + ObjectMaterials.For(d));
            }
        times.Sort();
        string report = string.Format(
            "mode {0}: {1} objects, total {2:F0} ms, median {3:F1} ms, p90 {4:F1} ms, worst {5:F1} ms ({6}); texture memory {7:F2} MB (RGBA32, no mips)",
            ToyShading.Mode, times.Count, total, times[times.Count / 2], times[times.Count * 9 / 10], worst, worstId, bytes / (1024.0 * 1024.0));
        File.WriteAllText(Path.Combine(dir, "timing.txt"), report + "\n");
        Console.WriteLine(report);
    }
}
