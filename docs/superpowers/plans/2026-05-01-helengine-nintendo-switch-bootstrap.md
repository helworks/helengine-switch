# Nintendo Switch Bootstrap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first `helengine-switch` Dockerized Switch homebrew scaffold that targets Nintendo Switch only and boots in a Switch emulator to a stable solid yellow screen with no input dependency.

**Architecture:** Use the established `helengine-*` scaffold shape, but follow the current devkitPro Switch toolchain flow built around `devkitA64` and `libnx`. Keep the platform boundary explicit by having `main.cpp` delegate immediately to `SwitchBootHost`, preserve the generated-core seam, and use `libnx`'s native framebuffer path for the first visible frame while keeping the boundary clean for future `deko3d` integration.

**Tech Stack:** Docker, devkitPro `devkitA64`, `libnx`, GNU Make, C++17, Switch framebuffer/NWindow APIs, Ryujinx or yuzu

---

## File Structure

- Create: `Dockerfile`
  Purpose: define the Switch build container on top of the official `devkitpro/devkita64:latest` image.
- Create: `Makefile`
  Purpose: build Switch homebrew using the active `libnx` rules flow, Switch only compile macros, and the generated-core seam.
- Create: `README.md`
  Purpose: document the Docker build flow, output artifact name, and the expected emulator yellow-screen verification result.
- Create: `src/main.cpp`
  Purpose: enter the Switch bootstrap and hand off immediately to `SwitchBootHost`.
- Create: `src/platform/switch/SwitchBootHost.hpp`
  Purpose: declare the Switch specific boot host interface and helper methods for framebuffer creation and presentation.
- Create: `src/platform/switch/SwitchBootHost.cpp`
  Purpose: initialize the Switch app lifecycle and framebuffer path, clear the framebuffer to yellow, and keep the frame alive.

### Task 1: Confirm The Current Switch Toolchain Surface

**Files:**
- Research only

- [ ] **Step 1: Record the official Switch toolchain facts from primary sources**

Run:

```bash
rtk printf '%s\n' \
  'Official Switch facts to preserve in the scaffold:' \
  '1. devkitPro provides the Switch Docker image as devkitpro/devkita64' \
  '2. Switch homebrew support is installed via the switch-dev package group' \
  '3. libnx is the standard userland foundation library for Switch homebrew' \
  '4. deko3d is the low-level 3D API we want to stay ready for later'
```

Expected: the Switch scaffold is grounded in the current official toolchain surface before repo files are created.

- [ ] **Step 2: Inspect the active container layout instead of guessing rule paths**

Run:

```bash
DOCKER_CONFIG=/tmp/docker-no-creds docker run --rm devkitpro/devkita64:latest sh -lc 'find /opt/devkitpro -maxdepth 5 \( -name switch_rules -o -name "*nacptool*" -o -name "*elf2nro*" -o -name "libnx*" \) | sort'
```

Expected: you know the exact rules/helper paths exposed by the image and whether the base image already contains all needed Switch packages.

- [ ] **Step 3: Record the concrete build facts before scaffolding**

Capture these facts for implementation:

```text
- exact rule/include path used for libnx Makefile builds
- whether devkitpro/devkita64:latest already contains switch-dev/libnx tools
- exact output helper tools available for NRO generation
- minimum Switch APIs needed for an appletMainLoop + framebuffer bootstrap
```

Expected: later tasks can write repo files without stale assumptions from older templates.

### Task 2: Create The Switch Build Skeleton

**Files:**
- Create: `Dockerfile`
- Create: `Makefile`

- [ ] **Step 1: Inspect the official Switch example shape before adapting it**

Run:

```bash
rtk printf '%s\n' \
  'Reference the official Switch example shape:' \
  '- devkitpro/devkita64 image' \
  '- switch-dev package group / libnx foundation' \
  '- Makefile builds based on libnx switch rules' \
  '- final runnable artifact is an .nro'
```

Expected: the scaffold preserves the official Switch entry points while adapting the repo layout to `src/` and `src/platform/switch/`.

- [ ] **Step 2: Write the Switch Dockerfile**

Create `Dockerfile` with:

```dockerfile
FROM devkitpro/devkita64:latest

ENV DEVKITPRO=/opt/devkitpro
ENV DEVKITA64=/opt/devkitpro/devkitA64
ENV PATH=/opt/devkitpro/devkitA64/bin:/opt/devkitpro/tools/bin:${PATH}

WORKDIR /workspace
```

- [ ] **Step 3: Write the Switch Makefile header and toolchain wiring**

Create `Makefile` with:

```makefile
ifeq ($(strip $(DEVKITPRO)),)
$(error "Please set DEVKITPRO in your environment. export DEVKITPRO=/opt/devkitpro")
endif

HELENGINE_CORE_CPP_ROOT ?=
TOPDIR ?= $(CURDIR)

include $(DEVKITPRO)/libnx/switch_rules

TARGET := helengine_switch
BUILD := build
SOURCES := src src/platform/switch
DATA :=
INCLUDES := src
ROMFS :=
```

- [ ] **Step 4: Add the Switch only compile/link configuration**

Append to `Makefile`:

```makefile
ARCH := -march=armv8-a -mtune=cortex-a57 -mtp=soft -fPIE
CFLAGS := -g -Wall -O2 -ffunction-sections $(ARCH)
CFLAGS += $(INCLUDE) -D__SWITCH__
CXXFLAGS := $(CFLAGS) -std=gnu++17
ASFLAGS := -g $(ARCH)
LDFLAGS := -specs=$(DEVKITPRO)/libnx/switch.specs -g $(ARCH) -Wl,-Map,$(notdir $*.map)
LIBS := -lnx

ifeq ($(strip $(HELENGINE_CORE_CPP_ROOT)),)
CFLAGS += -DHELENGINE_SWITCH_HAS_GENERATED_CORE=0
CXXFLAGS += -DHELENGINE_SWITCH_HAS_GENERATED_CORE=0
else
CFLAGS += -DHELENGINE_SWITCH_HAS_GENERATED_CORE=1
CXXFLAGS += -DHELENGINE_SWITCH_HAS_GENERATED_CORE=1 -I$(HELENGINE_CORE_CPP_ROOT)
endif

LIBDIRS := $(PORTLIBS) $(LIBNX)
```

- [ ] **Step 5: Add the recursive build rules adapted to the repo layout**

Append to `Makefile`:

```makefile
ifneq ($(BUILD),$(notdir $(CURDIR)))
export OUTPUT := $(CURDIR)/$(BUILD)/$(TARGET)
export TOPDIR := $(CURDIR)
export VPATH := $(foreach dir,$(SOURCES),$(CURDIR)/$(dir))
export DEPSDIR := $(CURDIR)/$(BUILD)

CFILES := $(foreach dir,$(SOURCES),$(notdir $(wildcard $(dir)/*.c)))
CPPFILES := $(foreach dir,$(SOURCES),$(notdir $(wildcard $(dir)/*.cpp)))
SFILES := $(foreach dir,$(SOURCES),$(notdir $(wildcard $(dir)/*.s)))
BINFILES :=

ifeq ($(strip $(CPPFILES)),)
export LD := $(CC)
else
export LD := $(CXX)
endif

export OFILES_BIN :=
export OFILES_SRC := $(CPPFILES:.cpp=.o) $(CFILES:.c=.o) $(SFILES:.s=.o)
export OFILES := $(OFILES_BIN) $(OFILES_SRC)
export INCLUDE := $(foreach dir,$(INCLUDES),-I$(CURDIR)/$(dir)) \
	$(foreach dir,$(LIBDIRS),-I$(dir)/include) \
	-I$(CURDIR)/$(BUILD)
export LIBPATHS := $(foreach dir,$(LIBDIRS),-L$(dir)/lib)

.PHONY: $(BUILD) clean all

all: $(BUILD)

$(BUILD):
	@[ -d $@ ] || mkdir -p $@
	@$(MAKE) --no-print-directory -C $(BUILD) -f $(CURDIR)/Makefile

clean:
	@rm -fr $(BUILD)

else
.PHONY: all

DEPENDS := $(OFILES:.o=.d)

all: $(OUTPUT).nro

$(OUTPUT).nro: $(OUTPUT).elf
$(OUTPUT).elf: $(OFILES)

-include $(DEPENDS)
endif
```

- [ ] **Step 6: Dry-run the build surface before adding runtime code**

Run:

```bash
rtk make -n
```

Expected: make prints the recursive libnx-based build shape and references `build/helengine_switch.elf` and `build/helengine_switch.nro` rather than any Wii U or handheld-agnostic artifacts.

- [ ] **Step 7: Commit the build scaffold**

Run:

```bash
rtk git add Dockerfile Makefile
rtk git commit -m "Add Switch Docker build scaffold"
```

Expected: commit succeeds with only the Switch build files staged.

### Task 3: Add The Switch Boot Host

**Files:**
- Create: `src/main.cpp`
- Create: `src/platform/switch/SwitchBootHost.hpp`
- Create: `src/platform/switch/SwitchBootHost.cpp`

- [ ] **Step 1: Write the minimal entry point**

Create `src/main.cpp` with:

```cpp
#include "platform/switch/SwitchBootHost.hpp"

int main() {
    return helengine::switch_platform::SwitchBootHost::Run();
}
```

- [ ] **Step 2: Declare the Switch boot host interface**

Create `src/platform/switch/SwitchBootHost.hpp` with:

```cpp
#pragma once

#include <switch.h>

namespace helengine::switch_platform {
    class SwitchBootHost {
    public:
        static int Run();

    private:
        static bool InitializeFramebuffer(Framebuffer& framebuffer);
        static void RenderYellowFrame(Framebuffer& framebuffer);
        static u32 BuildYellowColor();
    };
}
```

- [ ] **Step 3: Implement the Switch applet and framebuffer bootstrap**

Create `src/platform/switch/SwitchBootHost.cpp` with:

```cpp
#include "platform/switch/SwitchBootHost.hpp"

namespace helengine::switch_platform {
    namespace {
        constexpr u32 FramebufferWidth = 1280;
        constexpr u32 FramebufferHeight = 720;
    }

    int SwitchBootHost::Run() {
        NWindow* window = nwindowGetDefault();
        Framebuffer framebuffer;
        if (!InitializeFramebuffer(framebuffer)) {
            return 1;
        }

        while (appletMainLoop()) {
            RenderYellowFrame(framebuffer);
        }

        framebufferClose(&framebuffer);
        return 0;
    }

    bool SwitchBootHost::InitializeFramebuffer(Framebuffer& framebuffer) {
        NWindow* window = nwindowGetDefault();
        if (R_FAILED(framebufferCreate(&framebuffer, window, FramebufferWidth, FramebufferHeight, PIXEL_FORMAT_RGBA_8888, 2))) {
            return false;
        }

        if (R_FAILED(framebufferMakeLinear(&framebuffer))) {
            framebufferClose(&framebuffer);
            return false;
        }

        return true;
    }

    void SwitchBootHost::RenderYellowFrame(Framebuffer& framebuffer) {
        u32 stride = 0;
        u32* pixels = static_cast<u32*>(framebufferBegin(&framebuffer, &stride));
        const u32 yellow = BuildYellowColor();
        const u32 stridePixels = stride / sizeof(u32);

        for (u32 y = 0; y < FramebufferHeight; ++y) {
            for (u32 x = 0; x < FramebufferWidth; ++x) {
                pixels[y * stridePixels + x] = yellow;
            }
        }

        framebufferEnd(&framebuffer);
    }

    u32 SwitchBootHost::BuildYellowColor() {
        return RGBA8_MAXALPHA(255, 255, 0);
    }
}
```

- [ ] **Step 4: Check include paths and symbol names before container verification**

Run:

```bash
rtk sed -n '1,220p' src/main.cpp
rtk sed -n '1,260p' src/platform/switch/SwitchBootHost.hpp
rtk sed -n '1,320p' src/platform/switch/SwitchBootHost.cpp
```

Expected: `main.cpp` includes `platform/switch/SwitchBootHost.hpp`, the namespace is consistently `helengine::switch_platform`, and the framebuffer helpers match the declarations.

- [ ] **Step 5: Build in Docker to verify the boot host compiles and links**

Run:

```bash
DOCKER_CONFIG=/tmp/docker-no-creds docker build -t helengine-switch .
DOCKER_CONFIG=/tmp/docker-no-creds docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make
```

Expected: the container emits `build/helengine_switch.elf` and `build/helengine_switch.nro`.

- [ ] **Step 6: Commit the Switch bootstrap code**

Run:

```bash
rtk git add src/main.cpp src/platform/switch/SwitchBootHost.hpp src/platform/switch/SwitchBootHost.cpp
rtk git commit -m "Add Switch yellow-screen bootstrap"
```

Expected: commit succeeds with only the new source files staged.

### Task 4: Document The Verified Flow

**Files:**
- Create: `README.md`

- [ ] **Step 1: Write the README with the exact Docker build flow**

Create `README.md` with:

````md
# helengine-switch

Nintendo Switch bootstrap scaffold for Helengine.

## Build

```bash
docker build -t helengine-switch .
docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make
```

If Docker credential-helper issues prevent anonymous pulls, use:

```bash
DOCKER_CONFIG=/tmp/docker-no-creds docker build -t helengine-switch .
DOCKER_CONFIG=/tmp/docker-no-creds docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make
```

## Output

The build emits:

- `build/helengine_switch.elf`
- `build/helengine_switch.nro`

## Generated core seam

`HELENGINE_CORE_CPP_ROOT` is reserved for future generated core integration. This milestone only builds the native Switch host scaffold.

## Boot check

Load `build/helengine_switch.nro` in the emulator. The expected result is an immediate stable solid yellow screen with no input dependency.
````

- [ ] **Step 2: Sanity-check the README against the actual build outputs**

Run:

```bash
rtk sed -n '1,220p' README.md
```

Expected: the README names the same commands and artifact extensions produced by the verified build.

- [ ] **Step 3: Commit the documentation**

Run:

```bash
rtk git add README.md
rtk git commit -m "Document Switch bootstrap flow"
```

Expected: commit succeeds with only the README staged.

### Task 5: Final Verification And Cleanup

**Files:**
- Verify repository state only

- [ ] **Step 1: Re-run the full container build from a clean workspace**

Run:

```bash
rtk make clean
DOCKER_CONFIG=/tmp/docker-no-creds docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make clean
DOCKER_CONFIG=/tmp/docker-no-creds docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make
```

Expected: the clean build completes and re-emits the Switch artifacts without manual intervention.

- [ ] **Step 2: Verify the runtime milestone in the emulator**

Run this manual check:

```text
Load build/helengine_switch.nro in Ryujinx or yuzu.
Expected: immediate stable solid yellow screen on boot, no controller required, no immediate crash back to the launcher.
```

- [ ] **Step 3: Check repository cleanliness before handoff**

Run:

```bash
rtk git status --short
```

Expected: only intended source/docs/build-script changes remain. Generated artifacts should still be ignored or intentionally unstaged.

- [ ] **Step 4: Prepare the final integration checkpoint**

Run:

```bash
rtk git log --oneline --decorate -n 5
```

Expected: the recent history shows the scaffold commits in a clean order for review or squashing later if desired.
