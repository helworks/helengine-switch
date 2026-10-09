# Original BNTX fixtures

These synthetic files contain original coordinate-color patterns and specification-defined constant compressed blocks. They contain no game assets. `.rgba` sidecars hold expected logical pixels independently of the engine importer.

`generate-fixtures.py` builds NX/BRTI headers with Python's `struct` and calls the external [BNTX Injector swizzler](https://github.com/aboood40091/BNTX-Injector/blob/b27df538c38fa7f2240a9cd9ecd2b1a58dd484ea/swizzle.py), pinned to commit `b27df538c38fa7f2240a9cd9ecd2b1a58dd484ea`. The upstream implementation stays external; it is not vendored into this repository. Run:

```text
python generate-fixtures.py PATH/TO/BNTX-Injector/swizzle.py
```

`rgba-linear` and `rgba-gob` are 13×9 RGBA8 with nonuniform coverage. `rgba-array-mips` contains two array layers, each with 13×9 and 6×4 levels. `bc1-red` uses literal RGB565 endpoints with selector zero. `astc-red` uses an LDR void-extent block with opaque red UNORM16 channels. Header offsets and texture descriptor fields follow the same pinned injector's `structs.py` layout.
