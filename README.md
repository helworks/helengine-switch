# Helengine Switch Host

This repository contains the native Nintendo Switch host and managed platform builder for Helengine.

## Current milestone

- Docker-only build using the official devkitPro Switch image
- Standard Switch homebrew packaging ending in `.nro`
- Managed Switch builder under `builder/`
- Generated-core-aware native build path that stages cooked content into RomFS
- Bootstrap boot check with a solid yellow screen when no generated core is supplied

## Build

```bash
docker build -t helengine-switch .
docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make
```

If Docker Desktop's credential helper blocks anonymous pulls on this machine, use:

```bash
DOCKER_CONFIG=/tmp/docker-no-creds docker build -t helengine-switch .
DOCKER_CONFIG=/tmp/docker-no-creds docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make
```

The build emits `build/helengine_switch.elf` and `build/helengine_switch.nro`.

## Builder tests

```bash
dotnet test builder.tests/helengine.switch.builder.tests.csproj -p:HelengineRoot=C:\dev\helworks\helengine
```

## Project build flow

Build packaged Switch output through the shared Helengine platform build script:

```powershell
powershell -NoProfile `
  -ExecutionPolicy Bypass -File C:\dev\helworks\helengine\artifacts\build-platform.ps1 `
    -Project C:\dev\helprojs\city\project.heproj `
    -Platform switch `
    -Output C:\dev\helprojs\city\switch-build
```

This is the expected path for generated-core output, cooked content staging, and final Switch package export.

To build and launch the newest produced `.nro` in the installed Switch emulator:

```powershell
powershell -NoProfile `
  -ExecutionPolicy Bypass -File C:\dev\helworks\helengine-switch\artifacts\build-and-launch-switch.ps1 `
    -Project C:\dev\helprojs\city\project.heproj `
    -Output C:\dev\helprojs\city\switch-build
```

The launcher currently defaults to:

- `C:\dev\helworks\helengine\artifacts\build-platform.ps1`
- `C:\dev\helworks\emus\Eden-Windows-v0.2.0-rc2-amd64-msvc-standard\eden-cli.exe`

## Generated core seam

The native build accepts:

- `HELENGINE_CORE_CPP_ROOT`: generated Helengine C++ output root that contains `helcpp_config.hpp`, one generated translation unit, and the runtime manifest sources under `runtime/`
- `HELENGINE_SWITCH_ROMFS_ROOT`: staged RomFS root that should be embedded into the produced `.nro`

When `HELENGINE_CORE_CPP_ROOT` is omitted, the Switch host stays in bootstrap mode and renders the solid yellow frame. When it is supplied together with a staged RomFS tree, the native player mounts `romfs:/`, loads the generated runtime manifests, and runs the packaged startup scene through the Switch host backends.

## Boot check

Load `build/helengine_switch.nro` in Ryujinx or yuzu. The expected result for this milestone is a stable solid yellow frame with no immediate crash or return to the launcher.
