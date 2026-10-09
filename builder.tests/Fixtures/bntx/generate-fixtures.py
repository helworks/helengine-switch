"""Creates original NX BNTX fixtures using the upstream injector's Tegra swizzler.

Usage: python generate-fixtures.py PATH/TO/BNTX-Injector/swizzle.py
No game data or upstream implementation is copied into this repository.
"""
import importlib.util
from pathlib import Path
import struct
import sys

spec = importlib.util.spec_from_file_location("reference_swizzle", sys.argv[1])
reference = importlib.util.module_from_spec(spec)
spec.loader.exec_module(reference)
root = Path(__file__).parent


def pattern(width, height, layer=0, mip=0):
    return bytes(value for y in range(height) for x in range(width)
                 for value in ((x * 17 + layer * 71) % 256, (y * 23 + mip * 79) % 256,
                               (x * 13 + y * 11 + layer * 37) % 256, (x * 29 + y * 7) % 256))


def make(name, tiled, width, height, surface, bpp, bw=1, bh=1, tight=None, expected=None, layers=1, mips=1, swizzle=0x02030405):
    gob = 1 if tiled and (height + bh - 1) // bh > 8 else 0
    levels = []
    views = []
    for layer in range(layers):
        payloads = []
        for mip in range(mips):
            w, h = max(1, width >> mip), max(1, height >> mip)
            pixels = pattern(w, h, layer, mip) if tight is None else tight
            view = pixels if expected is None else expected
            exponent = gob
            if mip:
                while exponent and (h + bh - 1) // bh <= 8 << (exponent - 1):
                    exponent -= 1
            payloads.append(bytes(reference.swizzle(w, h, bw, bh, True, bpp, 0 if tiled else 1, exponent, pixels)))
            views.append(view)
            (root / f"{name}.layer{layer}.mip{mip}.rgba").write_bytes(view)
        levels.append(payloads)
    image = b"".join(chunk for layer in levels for chunk in layer)
    data = bytearray(0x400 + len(image))
    struct.pack_into("<8sIH2BI2H2I", data, 0, b"BNTX\0\0\0\0", 0x00040000, 0xfeff, 9, 8, 0, 0, 0x100, 0, len(data))
    struct.pack_into("<4sI5qI4x", data, 32, b"NX  ", 1, 88, 0x3f0, 0, 0, 0, 0)
    struct.pack_into("<Q", data, 88, 0x100)
    struct.pack_into("<4s2I4x", data, 0x100, b"BRTI", 0x2f0, 0x100)
    struct.pack_into("<2B4H2x2I3i3I20x3IB3x8q", data, 0x110,
                     0, 2, 0 if tiled else 1, 0, mips, 1, surface, 0, width, height, 1,
                     layers, gob, 0, len(image), 512 if tiled else 32, swizzle, 1,
                     0x200, 32, 0x220, 0, 0, 0, 0, 0)
    encoded_name = name.encode("utf8")
    struct.pack_into("<H", data, 0x200, len(encoded_name))
    data[0x202:0x202 + len(encoded_name)] = encoded_name
    offset = 0x400
    for mip, payload in enumerate(levels[0]):
        struct.pack_into("<Q", data, 0x220 + mip * 8, offset)
        offset += len(payload)
    struct.pack_into("<4s2I4x", data, 0x3f0, b"BRTD", 0, len(image) + 16)
    data[0x400:] = image
    (root / f"{name}.bntx").write_bytes(data)


make("rgba-linear", False, 13, 9, 0x0b01, 4)
make("rgba-gob", True, 13, 9, 0x0b01, 4)
make("rgba-array-mips", True, 13, 9, 0x0b01, 4, layers=2, mips=2)
make("bc1-red", True, 4, 4, 0x1a01, 8, 4, 4,
     tight=bytes.fromhex("00f8e00700000000"), expected=bytes([255, 0, 0, 255]) * 16)
# ASTC LDR void-extent block: constant opaque red, independently defined by the ASTC block specification.
make("astc-red", True, 4, 4, 0x2d01, 16, 4, 4,
     tight=bytes.fromhex("fcfdffffffffffffffff00000000ffff"), expected=bytes([255, 0, 0, 255]) * 16)
print("Created five BNTX containers and their independent expected RGBA views.")
