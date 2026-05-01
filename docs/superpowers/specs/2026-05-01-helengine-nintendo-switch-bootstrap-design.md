# Nintendo Switch Bootstrap Design

## Goal

Create the first working `helengine-switch` bootstrap that builds in Docker with the current devkitPro Switch toolchain, targets Nintendo Switch only, and boots in a Switch emulator to an immediate solid yellow screen with no input dependency.

## Constraints

- The repository should stay structurally close to the other recent platform scaffolds.
- The build must be Switch only, not a generic handheld target.
- The first milestone should keep the same generated-core seam used in the other platform repos.
- The first runtime target is intentionally minimal: initialize, display yellow, and keep running.
- The low-level rendering boundary should remain explicitly Switch native so future `deko3d` and 3D work is not blocked by an early high-level abstraction.

## Chosen Approach

Use a shared-shape scaffold with Switch defaults and a `deko3d`-ready rendering boundary.

This keeps the repo layout and build conventions aligned with the established `helengine-*` platform repos while using the standard Switch homebrew foundation now. The first milestone should be built on `libnx` for lifecycle and platform startup, while keeping the host boundary clean enough that future `deko3d` integration can happen above the same bootstrap layer rather than requiring a rewrite.

## Repository Shape

The initial scaffold should include:

- `Dockerfile`
- `Makefile`
- `README.md`
- `src/main.cpp`
- `src/platform/switch/SwitchBootHost.hpp`
- `src/platform/switch/SwitchBootHost.cpp`

The file naming should remain platform explicit so later shared engine code can live above the platform host layer without blurring the boot boundary.

## Build Design

The Docker image should follow the devkitPro Switch toolchain flow.

The build files should:

- use the current devkitPro Switch homebrew packages
- emit the normal Switch homebrew artifact that emulators can run
- preserve `HELENGINE_CORE_CPP_ROOT ?=`
- define `HELENGINE_SWITCH_HAS_GENERATED_CORE=0/1`
- compile the minimal source set for the first platform host

The generated-core seam is reserved for future `cs2.cpp` integration, but no generated core code is required for this milestone.

## Runtime Design

`main.cpp` should hand off immediately to a Switch specific boot host.

`SwitchBootHost` should:

- initialize the minimum Switch application and display path needed for the first visible frame
- configure a simple native framebuffer or presentation path using the active Switch homebrew APIs
- clear the visible output to a solid yellow color
- remain alive in a simple loop so the emulator keeps showing the frame

No controller setup, input polling, text console UI, audio, filesystem gameplay code, or full `deko3d` renderer setup is required for this milestone.

## Packaging

The first runnable artifact should be the standard Switch homebrew `NRO` format. The exact helper macros, linker settings, and output naming should be derived from the current active devkitPro Switch toolchain used in Docker.

## Platform Boundary

This repository is Switch first and Switch only for now.

That means:

- no attempt to target other platforms with the same low-level host
- no 2D-only helper abstraction at the rendering boundary
- no premature unification of Switch graphics setup with older Nintendo repos

Future low-level graphics work is expected to use Switch-native APIs, likely including `deko3d`. The first scaffold should keep that path open rather than abstract it away prematurely.

## Verification

Success means:

1. `docker build -t helengine-switch .` succeeds
2. `docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make` succeeds
3. the build emits a runnable `NRO`
4. the emulator boots that artifact and shows a stable solid yellow screen immediately

## Non-Goals

- cross-platform compatibility at the low-level host boundary
- controller input
- text console output as the primary milestone
- shared engine runtime code beyond the generated-core seam
- full `deko3d` renderer setup for this first milestone
