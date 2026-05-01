# Helengine Switch Host

This repository contains the native Nintendo Switch host scaffold for Helengine.

## Current milestone

- Docker-only build using the official devkitPro Switch image
- Standard Switch homebrew packaging ending in `.nro`
- First boot check with a solid yellow screen in a Switch emulator

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

## Generated core seam

The native build reserves `HELENGINE_CORE_CPP_ROOT` for later `cs2.cpp` integration, but the first milestone does not compile generated core output yet.

## Boot check

Load `build/helengine_switch.nro` in Ryujinx or yuzu. The expected result for this milestone is a stable solid yellow frame with no immediate crash or return to the launcher.
