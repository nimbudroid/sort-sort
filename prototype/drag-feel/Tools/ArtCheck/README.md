# ArtCheck

Plain-C# checks for the library object art. Run outside Unity with mono (`mcs` / `mono`). Run every command from
`Assets/SortEverything/Prototype/Scripts/Content`.

```
SRC="ObjectDefs.cs Categories.cs VectorPainter.cs VectorPainter.Molded.cs VectorPainter.Sculpted.cs ToyShading.cs ObjectMaterials.cs ObjectLibrary.cs ObjectLibrary.Categories.cs ObjectDrawings.cs"
T=../../../../../Tools/ArtCheck
```

## Silhouette test

Colliders are built from `VectorPainter.RasterizeSilhouette`, so rendering-only changes must leave it byte-identical.
`silhouette_baseline_dae71c2.txt` holds the SHA-256 of every object's mask at commit dae71c2. The test exits 1 if any
of those changes; objects added since are listed but not failed. `silhouette_baseline_full_house.txt` extends it
with the kitchen and pantry objects added for the full-house campaign; verify both.

```
mcs -out:/tmp/sil.exe $SRC $T/SilhouetteHashes.cs && mono /tmp/sil.exe verify $T/silhouette_baseline_dae71c2.txt
```

## Preview sheet

Bake the library in two modes (`Flat`, `Painted`, `Molded` or `Sculpted`), then build a before/after sheet. Use
about 150 px per object for on-phone size, or a larger `--size` to enlarge. Each run also prints bake timing and
texture memory.

```
mcs -langversion:7.2 -out:/tmp/ap.exe $SRC $T/ArtPreview.cs
mono /tmp/ap.exe /tmp/molded Molded && mono /tmp/ap.exe /tmp/sculpted Sculpted
python3 $T/preview_sheet.py /tmp/molded /tmp/sculpted /tmp/sheet.png --size 150 --labels
python3 $T/preview_sheet.py /tmp/molded /tmp/sculpted /tmp/hero.png --size 260 --cols 4 \
  --ids apple,strawberry,orange,watermelon_slice,cupcake,donut,pizza_slice,popsicle,calculator,notebook,stapler,ketchup,pencil,shoe
```

## Squint test

Each folder gets a row at phone size, then the same row downscaled, blurred and scaled back up. Form that survives
the blurred row survives on a phone.

```
python3 $T/squint_sheet.py /tmp/molded,/tmp/sculpted /tmp/squint.png apple,calculator,notebook,stapler,pencil,shoe 120
```
