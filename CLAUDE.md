# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`CategorySorter` is a **server-side Harmony/DLL mod for 7 Days to Die V2.6**. When a player closes a
storage chest named `[sort]`, the mod distributes its items into nearby chests whose name (label)
matches one of the item's game categories (`ItemClass.Groups`), with an optional `[misc]` catch-all.
It introduces no new blocks/assets — it only acts on existing chests by name — so clients need not
install anything and EAC can stay enabled on a dedicated server.

## Build & release commands

```bash
# Local build (macOS) — csproj auto-detects the local Steam install's Managed DLLs
dotnet build CategorySorter.csproj -c Release

# Local build + package into dist/CategorySorter/ (and optionally deploy)
./build.sh
DEPLOY_DIR="/path/to/7 Days To Die/Mods" ./build.sh

# Build against a different install (e.g. dedicated server / CI)
dotnet build CategorySorter.csproj -c Release -p:GameRoot="/path/to/server"
MANAGED_DIR="/path/to/Managed" ./build.sh

# Release (CI only; manual): builds + runs semantic-release
gh workflow run release.yml -f dry_run=true   # safe preview
gh workflow run release.yml                    # real release
```

There is **no test suite**. Verification is: a clean compile against the real game DLLs (the
compiler validates every game API used) plus a runtime smoke test on a server (`[CategorySorter] initialized`
in the log, then sorting behaves as documented in the README).

## Architecture

The runtime flow is a single Harmony hook → scan → distribute pipeline, all guarded to run only on
the server (`ConnectionManager.Instance.IsServer`):

- **`src/ModApi.cs`** — `IModApi.InitMod` entry point; loads config and calls `harmony.PatchAll()`.
- **`src/Patches/GameManager_Patch.cs`** — Harmony **prefix on `GameManager.TEUnlockServer`** (fires
  server-side when any chest is closed). Resolves the closed `TileEntity` via
  `GameManager.Instance.lockedTileEntities` and hands it to the sorter.
- **`src/Sorter.cs`** — core logic (`DoSortingOut`). Reads the source chest via
  `TileEntityComposite` features `TEFeatureSignable` (name) and `TEFeatureStorage` (`items`).
  Only proceeds if the name matches the sort tag. Buckets nearby chests into category targets vs
  `[misc]`, then for each item finds the nearest matching category chest via `CategoryMatcher.Matches`.
  Moves via `ITileEntityLootable.TryStackItem`/`AddItem` (the `AddItem` fallback covers non-stackable
  items), then `SetModified()` to sync to clients.
- **`src/CategoryMatcher.cs`** — decides whether a chest label matches an item's categories
  (`ItemClass.Groups`). **V2.6 reality (verified against the local `items.xml` + decompiled
  `ItemClass`):** categories come from the item property **`Group`** (singular) — e.g.
  `Group="Ammo/Weapons,Ranged Weapons"`; `ItemClass` comma-splits it into `Groups` and **defaults to
  `Decor/Miscellaneous`** when absent (only ~311 of ~1390 items define a `Group`). The real category
  set is: `Resources`, `Food/Cooking`, `Ammo/Weapons` (umbrella), `Ranged Weapons`, `Melee Weapons`,
  `Ammo`, `Tools/Traps`, `Special Items`, `Books`, `Science`, `Medical`, `Chemicals`, `Armor`,
  `Clothing`, `Robotics`, `Basics`, `Building`, `Decor/Miscellaneous`. Matching: a label matches a
  group token **or any of its slash-segments** (so `[Food]` matches `Food/Cooking`), with an optional
  alias map (Config) for custom names; internal filter tokens (`CF*`, `TC*`, `*Only`, `advBuilding`)
  are ignored.
- **`src/TargetScanner.cs`** — finds candidate chests via a 3×3 chunk scan (`World.GetChunkSync`,
  `chunk.GetTileEntities()`), filtered by radius, container type, and skipping the sort chest itself,
  player-opened chests, and un-looted world containers. Returns them ordered by distance.
- **`src/TagUtil.cs`** — label comparison: strips optional surrounding `[...]` and compares
  case-insensitively. Used for both special tags and category names.
- **`src/Config.cs`** — loads `Config/CategorySorter.xml` at runtime from
  `Mods/CategorySorter/Config/CategorySorter.xml` (path derived from the assembly name, so the
  deployed mod folder must be named `CategorySorter`). Also parses the optional `<Aliases>` block
  (`<Alias label="Guns" group="Ranged Weapons"/>`, comma-separated `group` allowed) into
  `Config.Aliases` for `CategoryMatcher`.

### Build model (csproj)

Targets **net48** and references the game's `Managed` DLLs, never bundling them
(`Microsoft.NETFramework.ReferenceAssemblies` makes net48 buildable on Linux/macOS). Reference
resolution: pass `-p:GameRoot=...` (CI/dedicated server) → `ManagedDir` is derived; otherwise it
falls back to the local macOS Steam path. Harmony resolves from the install or a `Lib.Harmony` NuGet
fallback. Game DLLs are not redistributable and must never be committed.

### CI & releases

`.github/workflows/` — `ci.yml` (PR compile check) and `release.yml` (manual). Both obtain the game
DLLs by installing the **7DTD Dedicated Server (Steam app 294420) anonymously via SteamCMD**
(`scripts/install-7dtd-server.sh`, cached) — no Steam account or secret needed. Releases use
**semantic-release** (`.releaserc.json`): version and notes come from **Conventional Commits**,
`scripts/stage-release.sh` stamps the version into `ModInfo.xml` and zips `CategorySorter-vX.Y.Z.zip`,
`scripts/release-footer.sh` appends an installation section to the notes.

## Conventions that matter

- **Commit messages must follow Conventional Commits** (`feat:`, `fix:`, `docs:`, `chore:`, …) —
  this drives versioning. A `feat:`/`fix:` is required to trigger any release.
- **All player-facing and documentation text is English** (in-game chat messages, README,
  release-notes footer). Code comments are German; that is fine.
- **Chest labels are English** and case-insensitive with optional brackets (e.g. `[Ammo/Weapons]`).
- When changing game-API usage, confirm symbols against the real `Assembly-CSharp.dll` (the build is
  the check) rather than assuming — game internals change between versions.
