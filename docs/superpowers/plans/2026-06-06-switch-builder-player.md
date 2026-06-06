# Switch Builder And Player Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the `helengine-switch` managed builder and generated-core-aware native Switch player so cooked content can be staged into RomFS and packaged into a runnable `.nro`.

**Architecture:** Follow the working platform split used in `helengine-ds` and `helengine-psp`: a managed builder owns platform metadata, manifest generation, staged package layout, and Docker-backed native packaging; the native player mounts RomFS, reads generated runtime manifests, loads packaged assets, and runs the shared core through Switch-specific host services. Use placeholder Switch render backends initially so the player contract is complete without pretending a mature Switch renderer already exists.

**Tech Stack:** C# `net9.0`, xUnit, Helengine base-platform/editor/files projects, Docker, devkitPro `devkitA64`, `libnx`, GNU Make, C++20.

---

### Task 1: Builder Scaffold And Contract Tests

**Files:**
- Create: `builder/helengine.switch.builder.csproj`
- Create: `builder/helengine.switch.builder.sln`
- Create: `builder/Program.cs`
- Create: `builder.tests/helengine.switch.builder.tests.csproj`
- Create: `builder.tests/SwitchPlatformAssetBuilderTests.cs`
- Create: `builder.tests/SwitchRomFsAssetStagerTests.cs`
- Create: `builder.tests/SwitchRuntimeNativeManifestWriterTests.cs`
- Create: `builder.tests/SwitchNativeBuildExecutorTests.cs`

- [ ] **Step 1: Add failing metadata and staging tests**
- [ ] **Step 2: Run the targeted test project and confirm the new tests fail for missing types**

### Task 2: Managed Switch Builder Implementation

**Files:**
- Create: `builder/SwitchPlatformAssetBuilder.cs`
- Create: `builder/SwitchPlatformDefinitionFactory.cs`
- Create: `builder/SwitchMaterialSchemaIds.cs`
- Create: `builder/SwitchRomFsAssetStager.cs`
- Create: `builder/SwitchBuildWorkspace.cs`
- Create: `builder/ISwitchNativeBuildExecutor.cs`
- Create: `builder/SwitchNativeBuildExecutor.cs`
- Create: `builder/SwitchRuntimeNativeManifestWriter.cs`

- [ ] **Step 1: Implement the minimal builder descriptor, platform definition, and material cook path**
- [ ] **Step 2: Implement RomFS staging and runtime manifest generation against the real Helengine base-platform/editor contracts**
- [ ] **Step 3: Implement Docker native-build argument construction and exported artifact flow**
- [ ] **Step 4: Re-run focused builder tests until green**

### Task 3: Native Switch Player Integration

**Files:**
- Modify: `Makefile`
- Modify: `README.md`
- Modify: `src/platform/switch/SwitchBootHost.hpp`
- Modify: `src/platform/switch/SwitchBootHost.cpp`
- Create: `src/platform/switch/SwitchInputBackend.hpp`
- Create: `src/platform/switch/SwitchInputBackend.cpp`
- Create: `src/platform/switch/SwitchPackagedAssetLoader.hpp`
- Create: `src/platform/switch/SwitchPackagedAssetLoader.cpp`
- Create: `src/platform/switch/SwitchRuntimeTexture2D.hpp`
- Create: `src/platform/switch/SwitchRuntimeTexture2D.cpp`
- Create: `src/platform/switch/SwitchRuntimeModel.hpp`
- Create: `src/platform/switch/SwitchRuntimeModel.cpp`
- Create: `src/platform/switch/SwitchRuntimeMaterial.hpp`
- Create: `src/platform/switch/SwitchRuntimeMaterial.cpp`
- Create: `src/platform/switch/SwitchRenderManager2D.hpp`
- Create: `src/platform/switch/SwitchRenderManager2D.cpp`
- Create: `src/platform/switch/SwitchRenderManager3D.hpp`
- Create: `src/platform/switch/SwitchRenderManager3D.cpp`

- [ ] **Step 1: Teach the Makefile to compile generated-core translation units and embed a caller-provided RomFS root**
- [ ] **Step 2: Implement the generated-core-aware boot flow with RomFS mounting, scene catalog/input manifest ingestion, and bootstrap fallback**
- [ ] **Step 3: Implement placeholder Switch render and asset-loading backends that satisfy the core runtime contract cleanly**
- [ ] **Step 4: Keep the no-generated-core bootstrap path working**

### Task 4: Verification

**Files:**
- Modify as needed from prior tasks

- [ ] **Step 1: Run `dotnet test builder.tests/helengine.switch.builder.tests.csproj -p:HelengineRoot=../helengine`**
- [ ] **Step 2: Run the native bootstrap build without generated core through Docker and confirm the `.nro` still builds**
- [ ] **Step 3: Document any generated-core-path verification gaps that cannot be exercised from the current repo state**
