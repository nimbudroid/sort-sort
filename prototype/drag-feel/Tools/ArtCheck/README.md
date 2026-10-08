# ArtCheck

Plain-C# checks for the library object art. Run outside Unity with mono (`mcs` / `mono`). Run every command from
`Assets/SortEverything/Prototype/Scripts/Content`.

```
SRC="ObjectDefs.cs VectorPainter.cs VectorPainter.Molded.cs ToyShading.cs ObjectMaterials.cs ObjectLibrary.cs ObjectDrawings.cs"
T=../../../../../Tools/ArtCheck
```

## Silhouette test

Colliders are built from `VectorPainter.RasterizeSilhouette`, so rendering-only changes must leave it byte-identical.
`silhouette_baseline_dae71c2.txt` holds the SHA-256 of every object's mask at commit dae71c2. The test exits 1 on any
change.

```
mcs -out:/tmp/sil.exe $SRC $T/SilhouetteHashes.cs && mono /tmp/sil.exe verify $T/silhouette_baseline_dae71c2.txt
```

## Preview sheet

Bake the library in two modes, then build a before/after sheet. Use about 150 px per object for on-phone size, or a
larger `--size` to enlarge. Each run also prints bake timing and texture memory.

```
mcs -langversion:7.2 -out:/tmp/ap.exe $SRC $T/ArtPreview.cs
mono /tmp/ap.exe /tmp/painted Painted && mono /tmp/ap.exe /tmp/molded Molded
python3 $T/preview_sheet.py /tmp/painted /tmp/molded /tmp/sheet.png --size 150 --labels
python3 $T/preview_sheet.py /tmp/painted /tmp/molded /tmp/hero.png --size 260 --cols 4 \
  --ids apple,strawberry,orange,watermelon_slice,cupcake,donut,pizza_slice,popsicle,calculator,notebook,stapler,ketchup,pencil,shoe
```
