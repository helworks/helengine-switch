# Helengine Switch Host

This repository contains the Nintendo Switch platform host and builder integration for Helengine.

## Build

```powershell
dotnet run --project ..\helengine\tools\build-waiter\helengine.buildwaiter.csproj -- `
  --output ..\helprojs\city\switch-build `
  --require helengine_switch.nro `
  -- powershell -NoProfile -ExecutionPolicy Bypass -File ..\helengine\scripts\build-platform.ps1 `
  -Project ..\helprojs\city\project.heproj `
  -Platform switch `
  -Output ..\helprojs\city\switch-build
```

The Build Waiter returns successfully only after `helengine_switch.nro` is fresh and non-empty.

## Run In Emulator

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\launch_in_emulator.ps1 `
  -ArtifactPath ..\helprojs\city\switch-build\helengine_switch.nro
```

## More Docs

- [Docker Build Notes](docs/Docker.md)
- [Platform Notes](docs/PlatformNotes.md)
