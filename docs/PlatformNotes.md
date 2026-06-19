# Switch Platform Notes

## Current Milestone

- Docker-only build using the official devkitPro Switch image
- Standard Switch homebrew packaging ending in `.nro`
- Managed Switch builder under `builder/`
- Generated-core-aware native build path that stages cooked content into RomFS
- Bootstrap boot check with a solid yellow screen when no generated core is supplied

## Builder Tests

```bash
dotnet test builder.tests/helengine.switch.builder.tests.csproj -p:HelengineRoot=C:\dev\helworks\helengine
```

## Generated Core Seam

The native build accepts:

- `HELENGINE_CORE_CPP_ROOT`: generated Helengine C++ output root that contains `helcpp_config.hpp`, one generated translation unit, and the runtime manifest sources under `runtime/`
- `HELENGINE_SWITCH_ROMFS_ROOT`: staged RomFS root that should be embedded into the produced `.nro`

When `HELENGINE_CORE_CPP_ROOT` is omitted, the Switch host stays in bootstrap mode and renders the solid yellow frame. When it is supplied together with a staged RomFS tree, the native player mounts `romfs:/`, loads the generated runtime manifests, and runs the packaged startup scene through the Switch host backends.

## Boot Check

Load `build/helengine_switch.nro` in Ryujinx or yuzu. The expected result for this milestone is a stable solid yellow frame with no immediate crash or return to the launcher.
