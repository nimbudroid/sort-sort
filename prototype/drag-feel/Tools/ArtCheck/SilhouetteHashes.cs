// Silhouette regression check for library object art (plain C#, runs outside Unity with mono or dotnet).
// Colliders are built from VectorPainter.RasterizeSilhouette (see ObjectArt.Build), so its output must stay
// byte-identical whenever only the object *rendering* changes.
//   record:  SilhouetteHashes.exe record <baseline.txt>
//   verify:  SilhouetteHashes.exe verify <baseline.txt>   (exit code 1 on any mismatch)
// Build (from Assets/SortEverything/Prototype/Scripts/Content):
//   mcs -out:silhouette.exe ObjectDefs.cs Categories.cs VectorPainter.cs VectorPainter.Molded.cs
//       VectorPainter.Sculpted.cs ToyShading.cs ObjectMaterials.cs ObjectLibrary.cs ObjectLibrary.Categories.cs
//       ObjectDrawings.cs ../../../../../Tools/ArtCheck/SilhouetteHashes.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using SortEverything.Prototype;

static class SilhouetteHashes
{
    const int Res = 160; // ObjectArt.Res

    static int Main(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("usage: record|verify <baseline.txt>"); return 2; }
        var current = new SortedDictionary<string, string>();
        using (var sha = SHA256.Create())
            foreach (var def in ObjectLibrary.All)
            {
                var painter = ObjectDrawings.Paint(def.id);
                if (painter == null) continue;
                current[def.id] = BitConverter.ToString(sha.ComputeHash(painter.RasterizeSilhouette(Res))).Replace("-", "").ToLowerInvariant();
            }

        if (args[0] == "record")
        {
            using (var w = new StreamWriter(args[1]))
                foreach (var kv in current) w.WriteLine(kv.Key + " " + kv.Value);
            Console.WriteLine("recorded " + current.Count + " silhouette hashes");
            return 0;
        }

        var baseline = new Dictionary<string, string>();
        foreach (var line in File.ReadAllLines(args[1]))
        {
            var parts = line.Split(' ');
            if (parts.Length == 2) baseline[parts[0]] = parts[1];
        }
        int ok = 0, bad = 0;
        foreach (var kv in baseline)
        {
            string now;
            if (!current.TryGetValue(kv.Key, out now)) { Console.WriteLine("MISSING " + kv.Key); bad++; }
            else if (now != kv.Value) { Console.WriteLine("CHANGED " + kv.Key); bad++; }
            else ok++;
        }
        // Objects added since the baseline have no hash to compare; they are listed, not failed.
        int added = 0;
        foreach (var id in current.Keys) if (!baseline.ContainsKey(id)) { Console.WriteLine("added (not in baseline) " + id); added++; }
        Console.WriteLine(bad == 0
            ? "PASS: " + ok + "/" + baseline.Count + " baseline silhouettes byte-identical" + (added > 0 ? ", " + added + " objects added" : "")
            : "FAIL: " + bad + " mismatches, " + ok + " identical");
        return bad == 0 ? 0 : 1;
    }
}
