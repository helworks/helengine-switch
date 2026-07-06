# Switch Bootstrap Builder Design

## Goal

Allow `switch` to build through Helengine's shared platform pipeline even when the build graph produces no staged content and no generated core, emitting the engine-only bootstrap `.nro`.

## Design

- Keep the existing authored-content path unchanged.
- Treat missing generated core and missing `package-source` as valid only when the manifest is effectively empty.
- Continue failing hard for content builds that are missing generated core or staged package content.
- Expose Switch through the same metadata-only plugin manifest contract used by the other external platforms.
- Register Switch in Helengine's local development `platforms.json` so `build-platform.ps1 -Platform switch` can resolve the builder on this machine.

## Verification

- Add builder tests for the bootstrap build path.
- Add native-build argument tests to prove generated-core mounts are omitted for bootstrap builds.
- Add plugin-manifest tests to keep the checked-in metadata file aligned with the generated payload.
