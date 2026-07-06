# Switch Bootstrap Builder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Switch builder emit an engine-only bootstrap `.nro` when no generated core or staged content exists, while keeping content builds strict.

**Architecture:** Extend the managed builder's request validation so it distinguishes empty manifests from authored-content manifests, then thread that mode into the native Docker build workspace and external platform discovery metadata. Keep the native `Makefile` behavior unchanged and make the managed layer decide when generated-core inputs are required.

**Tech Stack:** C#, xUnit, Docker, libnx Switch build wrapper, Helengine platform discovery metadata

---

### Task 1: Bootstrap builder behavior

**Files:**
- Modify: `builder.tests/SwitchPlatformAssetBuilderTests.cs`
- Modify: `builder/SwitchPlatformAssetBuilder.cs`

- [ ] **Step 1: Write the failing bootstrap build test**
- [ ] **Step 2: Run `dotnet test` and verify the test fails**
- [ ] **Step 3: Allow empty-manifest builds to skip generated core and staged package-source**
- [ ] **Step 4: Run `dotnet test` and verify the builder tests pass**

### Task 2: Native workspace argument wiring

**Files:**
- Modify: `builder.tests/SwitchNativeBuildExecutorTests.cs`
- Modify: `builder/SwitchBuildWorkspace.cs`
- Modify: `builder/SwitchNativeBuildExecutor.cs`

- [ ] **Step 1: Write the failing no-generated-core Docker argument test**
- [ ] **Step 2: Run `dotnet test` and verify the test fails**
- [ ] **Step 3: Make generated-core mounts optional in the workspace and Docker argument builder**
- [ ] **Step 4: Run `dotnet test` and verify the executor tests pass**

### Task 3: Platform discovery metadata

**Files:**
- Create: `builder/SwitchPlatformPluginManifest.cs`
- Create: `builder.tests/SwitchPlatformPluginManifestTests.cs`
- Create: `platform-plugin.json`
- Modify: `../helengine/user_settings/platforms.json`

- [ ] **Step 1: Write the failing plugin-manifest tests**
- [ ] **Step 2: Run `dotnet test` and verify the tests fail**
- [ ] **Step 3: Add the metadata-only Switch plugin manifest and register Switch in Helengine development platforms**
- [ ] **Step 4: Run `dotnet test` and verify the manifest tests pass**

### Task 4: Docs and verification

**Files:**
- Modify: `README.md`
- Create: `docs/superpowers/specs/2026-06-06-switch-bootstrap-builder-design.md`

- [ ] **Step 1: Document the engine-only bootstrap behavior**
- [ ] **Step 2: Run focused verification for builder tests and native bootstrap make**
- [ ] **Step 3: Summarize exact remaining constraints, if any**
