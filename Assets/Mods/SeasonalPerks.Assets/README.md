# Seasonal Perks SDK workspace

This folder supplies the Unity builders and previews for the companion
`../SeasonalPerks` project. Use Unity **2022.3.43f1** and the local SPT references
configured by CJ-SDK.

## Source synchronization

- `Editor/Generated` and `PreviewRuntime` contain synchronized copies of the
  companion project's `UI` sources. Run `tools/sync_ui_preview.py` from
  SeasonalPerks after editing those sources. Commit the generated C# files and
  their existing `.meta` files together so Unity asset GUIDs remain stable.
- The preview runners and story builders originate in SeasonalPerks
  `tools/unity`. Copy updates into `Editor`, converting file-scoped namespaces
  to block namespaces for Unity's C# 9 compiler.
- Other SDK-only builders remain in `Editor`.

## Local asset prerequisites

The hub and story recovery folders, generated story example assets, compiled
shaders and bundles remain local and are ignored by Git. Existing tracked
assets are preserved. A checkout alone does not supply all inputs needed to
rebuild the UI or trader rooms.

Follow the companion project's `README.md` for the recovered UI build, and
`docs/story-authoring.md` for donor recovery, room import, shader handling,
story preview checks and bundle finalization. Those procedures require the
locally supplied game assets and recovery manifests. Rebuild the synthetic
cinematic with `SeasonalPerks.Tools.SeasonalStoryExampleBuilder.Build`.

Build the recovered UI through **SDK / Seasonal Perks / Build recovered UI**.
Package and install runtime updates from the companion SeasonalPerks project.
