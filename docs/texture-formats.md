# Switch texture formats

The Switch image and font-atlas cook capabilities publish 262 color-format IDs: the four existing generic IDs (`Rgba32`, `Rgba4444`, `Indexed4`, `Indexed8`) and 258 native choices. Native IDs use `Switch.Linear.<format>` or `Switch.BlockLinear.<format>` and cover all 129 non-sentinel `DkImageFormat` values in the pinned deko3d catalog. Default cook settings remain generic RGBA32/A8.

| Family | Native storage |
| --- | --- |
| R, RG, RGB, RGBA, RGBX | Available 8/16/32-bit normalized, integer and floating-point component formats |
| Packed color | RGB/BGR565, RGB/BGR5, RGB/BGR5A1, A1BGR5, RGBA4, RGB10A2, RG11B10 float, E5BGR9 shared-exponent float |
| Channel order / transfer function | Native BGRA/BGRX alternatives and the catalog's sRGB variants |
| BC | RGB/RGBA BC1, BC2, BC3, unsigned/signed BC4/5, unsigned/signed BC6H, BC7 |
| ASTC | 4×4, 5×4, 5×5, 6×5, 6×6, 8×5, 8×6, 8×8, 10×5, 10×6, 10×8, 10×10, 12×10, 12×12; linear and sRGB |
| ETC2 / EAC | RGB, RGB punch-through alpha, RGBA, unsigned/signed R and RG eleven-bit channels |
| Depth / stencil | S8, Z16, Z24X8, Z24S8, ZF32, ZF32_X24S8 |

The authoritative names, numeric codes and block geometry live in `SwitchTextureFormatCatalog`. Each format accepts opaque coverage and, where present, its intrinsic alpha precision: binary, A2, A4 or A8. This describes stored bytes. A renderer must also observe deko3d's per-format usage restrictions when creating GPU images; the linear representation can serve as staging storage for a block-linear GPU image.

## Cooking and shared consumers

The Switch builder executes texture and font-atlas work items before RomFS staging. It reads the standard texture settings JSON, imports the source, resizes within the resolution cap, encodes the chosen native format, and writes a normal `.hetex` asset with `TextureAssetColorFormat.SwitchNative`. Stable runtime identities use the source asset ID; font atlases append `#atlas`. An already staged `.hefont` supplies the authoritative font atlas before raw TTF/OTF fallback. The final outputs and declared cooked artifacts are staged into RomFS using the existing bounded path and deduplication checks.

Cooking starts from the engine's RGBA8 source view. Integer and signed-normalized components receive normalized positive values; opaque formats discard coverage. Packed and depth/stencil words use native little-endian storage. BC uses BCnEncoder.Net 2.2.1, ASTC uses AstcSharp 2.0.0, and full ETC2 RGB mode decoding uses AssetRipper.TextureDecoder 2.6.3. EAC channels use exact eleven-bit signed/unsigned arithmetic and all sixteen Khronos modifier tables. Signed negative and HDR values are clamped only in the RGBA8 preview; an imported native buffer retains its original typed values.

Compression quality has explicit limits: BC uses the library's balanced preset; AstcSharp's encoder is experimental; ETC2 RGB cooking uses legal ETC1-compatible blocks; punch-through cooking fits a flat block color while retaining binary coverage; EAC cooking searches table zero with local endpoints and all multipliers. Import decoders support the broader legal block modes. These are functional encoders, with room for later quality and speed improvements.

Native `Colors` stores a 64-byte SWT1 descriptor followed by the complete padded native allocation. The existing `PaletteColors` auxiliary buffer stores an owned RGBA8 view decoded from the actual encoded output. It is a CPU cache rather than a GPU palette. This keeps compression packages in managed editor/build code while core, previews, texture readers and font readers consume native assets without translating those packages into C++. Both buffers carry integrity checks. Native assets must be recooked after modifying either buffer; the preview cannot silently become stale.

Linear rows use 32-byte pitch alignment. Block-linear storage follows Tegra X1's 64×8-byte GOBs and 1–32 GOB height groups. Addressing operates per byte, including twelve-byte elements crossing sixteen-byte sectors. Cooked assets contain one 2D level. Limits are 1–16384 pixels per axis, 128 MiB of native allocation and 64 MiB of RGBA8 cache per asset. The VRAM estimate counts the padded native allocation; serialized size and CPU memory also include the descriptor and preview cache.

## BNTX import

The editor registers `.bntx`. `SwitchBntxTextureImporter.ImportTexture` returns an independently owned RGBA image, while `ImportNativeTexture` retains the native format, allocation, GOB height and component selectors. Both APIs accept texture, array-layer and mip indices and default to the first subresource. Selected mips reduce GOB height as required by their block-row count.

Current container mappings cover R8, RG8, RGBA8, BGRA8, RGBA4, RGB/BGR565, RGB/BGR5A1, A1BGR5, RGB10A2, RG11B10, BC1–BC7 and all fourteen ASTC footprints, with supported numeric/sRGB variants. These mappings are a subset of the full cook catalog. Unknown BNTX surface words fail explicitly. The importer supports little-endian NX, single-sample 2D images and selected array layers, bounded to 256 MiB containers. It validates full 64-bit pointers, geometry, layer strides, mip ordering, storage overlap and component selectors. It does not unpack BFRES/SARC containers, extract embedded BNTX, import BFFNT metrics, or retain an entire mip/array chain in one engine texture. Ordinary build work items decode and recook the selected source; native import is the API for retaining its original native bytes.

## SWT1 version 1

All fields are unsigned little-endian 32-bit words.

| Byte offset | Meaning |
| --- | --- |
| 0 / 4 | `SWT1` magic (`0x31545753`) / version 1 |
| 8 | deko3d format code, 1–129 |
| 12 / 16 | Logical width / height |
| 20 | Layout: 0 linear, 1 block-linear |
| 24 / 28 | Native byte pitch / GOB-height exponent |
| 32 / 36 | Native allocation bytes / RGBA8 cache bytes |
| 40 | RGBA component selectors in low-to-high byte order: Zero=0, One=1, R=2, G=3, B=4, A=5 |
| 44 / 48 | FNV-1a of native allocation / CPU cache |
| 52 / 56 / 60 | Reserved, required zero |

## Runtime and validation

`SwitchRuntimeTexture2D` validates and owns independent logical pixels and native storage. The existing Switch host renderer still has placeholder draw methods and no GPU image upload/binding. This work supplies format cooking, import, serialization, previews, RomFS integration and runtime ownership; visible native rendering needs the host's deko3d renderer implementation.

Validation artifacts are under `builds/helengine-switch/texture-formats-20261008`: 419 focused builder/codec tests, six font reader cases covering both metadata byte orders and packed alpha, and the editor importer registration test. Original BNTX fixtures cover linear/GOB RGBA, two layers with two mips, literal BC1 and literal ASTC. Independently decoded cooked BC1/2/3/7 and all ASTC footprints agree within one byte of interpolation rounding. A reference check verifies all 129 catalog geometries and all 258 cooked allocations against upstream Tegra byte addressing.

The real core was generated for Switch with eight-byte pointers, host filesystem enabled, and automatic menu/script reflection disabled for this validation. Four generated format/codec units and both changed host texture/renderer units compile with devkitA64/libnx and warnings treated as errors. This is source/SDK compilation, not a complete NRO or hardware rendering test. Generation with automatic script reflection enabled encounters the existing ownership classification failure in `AutomaticScriptComponentRuntimeDeserializer.CreateComponent`; that unrelated path is outside the validated configuration.

References: [deko3d catalog and traits, commit 350f2b0](https://github.com/devkitPro/deko3d/tree/350f2b00a3e76ecd4f00191f8c5d6544ffbcb9db), [BNTX Injector structures and swizzler, commit b27df53](https://github.com/aboood40091/BNTX-Injector/tree/b27df538c38fa7f2240a9cd9ecd2b1a58dd484ea), [Khronos data format specification](https://registry.khronos.org/DataFormat/specs/1.4/dataformat.1.4.pdf).
