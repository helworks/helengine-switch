ifeq ($(strip $(DEVKITPRO)),)
$(error "Please set DEVKITPRO in your environment. export DEVKITPRO=/opt/devkitpro")
endif

HELENGINE_CORE_CPP_ROOT ?=
HELENGINE_SWITCH_ROMFS_ROOT ?=
TOPDIR ?= $(CURDIR)

include $(DEVKITPRO)/libnx/switch_rules

TARGET := helengine_switch
BUILD := build
SOURCES := src src/platform/switch
DATA :=
INCLUDES := src
ROMFS :=
GENERATED_CORE_SOURCE_DIRS :=
GENERATED_CORE_CPPFILES :=
GENERATED_CORE_TRANSLATION_UNIT :=

APP_TITLE := Helengine Switch
APP_AUTHOR := Helena Assis
APP_VERSION := 0.1.0

ARCH := -march=armv8-a -mtune=cortex-a57 -mtp=soft -fPIE
CFLAGS := -g -Wall -O2 -ffunction-sections $(ARCH)
CFLAGS += $(INCLUDE) -D__SWITCH__
CXXFLAGS := $(CFLAGS) -std=gnu++20
ASFLAGS := -g $(ARCH)
LDFLAGS := -specs=$(DEVKITPRO)/libnx/switch.specs -g $(ARCH) -Wl,-Map,$(notdir $*.map)
LIBS := -lnx

ifneq ($(strip $(HELENGINE_SWITCH_ROMFS_ROOT)),)
ROMFS := $(abspath $(HELENGINE_SWITCH_ROMFS_ROOT))
$(if $(wildcard $(ROMFS)),,$(error HELENGINE_SWITCH_ROMFS_ROOT '$(HELENGINE_SWITCH_ROMFS_ROOT)' does not exist))
NROFLAGS += --romfsdir=$(ROMFS)
endif

ifeq ($(strip $(HELENGINE_CORE_CPP_ROOT)),)
CFLAGS += -DHELENGINE_SWITCH_HAS_GENERATED_CORE=0
CXXFLAGS += -DHELENGINE_SWITCH_HAS_GENERATED_CORE=0
else
$(if $(wildcard $(HELENGINE_CORE_CPP_ROOT)/helcpp_config.hpp),,$(error HELENGINE_CORE_CPP_ROOT '$(HELENGINE_CORE_CPP_ROOT)' does not contain helcpp_config.hpp))
ifneq ($(wildcard $(HELENGINE_CORE_CPP_ROOT)/helengine_core_amalgamated.cpp),)
GENERATED_CORE_TRANSLATION_UNIT := helengine_core_amalgamated.cpp
else ifneq ($(wildcard $(HELENGINE_CORE_CPP_ROOT)/helengine_core_unity.cpp),)
GENERATED_CORE_TRANSLATION_UNIT := helengine_core_unity.cpp
else
$(error HELENGINE_CORE_CPP_ROOT '$(HELENGINE_CORE_CPP_ROOT)' does not contain helengine_core_amalgamated.cpp or helengine_core_unity.cpp)
endif
$(if $(wildcard $(HELENGINE_CORE_CPP_ROOT)/runtime/runtime_startup_manifest.cpp),,$(error HELENGINE_CORE_CPP_ROOT '$(HELENGINE_CORE_CPP_ROOT)' does not contain runtime/runtime_startup_manifest.cpp))
$(if $(wildcard $(HELENGINE_CORE_CPP_ROOT)/runtime/runtime_scene_catalog_manifest.cpp),,$(error HELENGINE_CORE_CPP_ROOT '$(HELENGINE_CORE_CPP_ROOT)' does not contain runtime/runtime_scene_catalog_manifest.cpp))
$(if $(wildcard $(HELENGINE_CORE_CPP_ROOT)/runtime/runtime_standard_platform_input_manifest.cpp),,$(error HELENGINE_CORE_CPP_ROOT '$(HELENGINE_CORE_CPP_ROOT)' does not contain runtime/runtime_standard_platform_input_manifest.cpp))
CFLAGS += -DHELENGINE_SWITCH_HAS_GENERATED_CORE=1
CXXFLAGS += -DHELENGINE_SWITCH_HAS_GENERATED_CORE=1 -I$(HELENGINE_CORE_CPP_ROOT)
GENERATED_CORE_SOURCE_DIRS := $(HELENGINE_CORE_CPP_ROOT) $(HELENGINE_CORE_CPP_ROOT)/runtime
GENERATED_CORE_CPPFILES := $(GENERATED_CORE_TRANSLATION_UNIT) runtime_startup_manifest.cpp runtime_scene_catalog_manifest.cpp runtime_standard_platform_input_manifest.cpp
ifneq ($(wildcard $(HELENGINE_CORE_CPP_ROOT)/runtime/runtime_code_module_manifest.cpp),)
GENERATED_CORE_CPPFILES += runtime_code_module_manifest.cpp
endif
ifneq ($(wildcard $(HELENGINE_CORE_CPP_ROOT)/runtime/runtime_physics3d_scene_feature_manifest.cpp),)
GENERATED_CORE_CPPFILES += runtime_physics3d_scene_feature_manifest.cpp
endif
endif

LIBDIRS := $(PORTLIBS) $(LIBNX)

ifneq ($(BUILD),$(notdir $(CURDIR)))
export OUTPUT := $(CURDIR)/$(BUILD)/$(TARGET)
export TOPDIR := $(CURDIR)
export VPATH := $(foreach dir,$(SOURCES),$(CURDIR)/$(dir)) $(GENERATED_CORE_SOURCE_DIRS)
export DEPSDIR := $(CURDIR)/$(BUILD)

CFILES := $(foreach dir,$(SOURCES),$(notdir $(wildcard $(dir)/*.c)))
CPPFILES := $(foreach dir,$(SOURCES),$(notdir $(wildcard $(dir)/*.cpp)))
CPPFILES += $(GENERATED_CORE_CPPFILES)
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
