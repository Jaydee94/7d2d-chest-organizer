# CategorySorter — automatic chest sorting for 7 Days to Die (V2.6)

A server-side mod that automatically moves items from a central **sorting chest** into nearby
**category chests** when the sorting chest is closed. Each category chest is assigned to an item
category through its **name (label)**. Server-side only — other players do not need to install
anything.

---

## Quick start

1. Place a storage chest and name it exactly **`[sort]`** → this is the sorting chest.
2. Name other chests after a **category**, e.g. `Ammo/Weapons`, `Tools/Traps`, `Food/Cooking`.
3. Optionally name a chest **`[misc]`** → a catch-all for everything else.
4. Put items into the `[sort]` chest and **close** it → the server distributes them automatically
   and confirms the result in chat.

---

## Labeling chests

A chest's "label" is simply its **name**, which you set in-game through the chest menu (the field
used to rename the chest). No special blocks are required — any regular storage or secure chest can
carry a label.

Label rules:

- **Case-insensitive** — `ammo/weapons` = `Ammo/Weapons`.
- **Square brackets optional** — `Tools/Traps` = `[Tools/Traps]`.
- **Keep the slash** — categories such as `Ammo/Weapons` are written exactly like that.

There are three kinds of labels:

| Label | Function |
|-------|----------|
| `[sort]` | **Sorting chest** – its contents are distributed when closed. |
| *category name* | **Category chest** – receives items of that category (see table below). |
| `[misc]` | **Catch-all chest** – receives anything that does not match an existing category chest. |

---

## Available category labels

The categories match the game's item groups. An item is placed into a category chest exactly when
that chest's label matches one of the item's categories. Possible labels:

| Label | Contents (typical items) |
|-------|---------------------------|
| `Basic` | Basic early-game items |
| `Ammo/Weapons` | Weapons and ammunition |
| `Tools/Traps` | Tools, devices and traps |
| `Building` | Building materials and placeable blocks |
| `Resources` | Raw materials and crafting resources |
| `Forging/Molds` | Forging materials and molds |
| `Decor` | Decoration and furnishing items |
| `Medicine` | Medicine, bandages and remedies |
| `Chemicals` | Chemicals and chemical base materials |
| `Food/Cooking` | Food, drinks and cooking ingredients |
| `Clothing` | Clothing and armor |
| `Miscellaneous` | Miscellaneous items without a dedicated category |
| `Special Items` | Special items |

> Note: You don't need a chest for every category. Create only the category chests you need — the
> rest goes into `[misc]` or stays in the `[sort]` chest.

---

## How sorting works

- **Matching category:** Each item is placed into the category chest whose label matches one of the
  item's categories.
- **Multiple matching chests:** If several chests share the same category, the **nearest** one is
  filled first; when it is full, the next one is used.
- **Items with multiple categories:** If an item belongs to more than one category, the **nearest
  matching** chest wins.
- **Catch-all:** If no matching category chest exists, the item goes into a `[misc]` chest (if
  present).
- **No target:** If neither a matching category chest nor a `[misc]` chest exists, the item
  **stays in the sorting chest**.
- **Range:** Only chests within the configured radius around the sorting chest are considered
  (default 20 blocks).
- **Protection:** Chests currently opened by a player and not-yet-looted world containers are
  skipped.

After closing, the player receives a short chat confirmation (all / partially / nothing sorted).

---

## Configuration

Settings in `Config/CategorySorter.xml`:

| Setting | Default | Meaning |
|---------|---------|---------|
| `SortTag` | `[sort]` | Name of the sorting chest |
| `MiscTag` | `[misc]` | Name of the catch-all chest |
| `MaxDistance` | `20.0` | Search radius in blocks (`-1` = no distance limit within the area) |
| `VerboseLogging` | `false` | Additional server log output |

---

## Installation

1. Copy the `CategorySorter/` folder (from the release ZIP) into the **server's** `Mods/` directory.
2. Restart the server.

The mod runs **server-side only**: connected players do not need to install anything. On a
dedicated server, EasyAntiCheat (EAC) can stay enabled; on a self-hosted listen server (P2P), EAC
must be disabled for the host.

Compatible with **7 Days to Die V2.6**.

---

## For developers

Local build and deploy: `./build.sh` (see the script for details). Releases are produced by the
GitHub Actions workflows in `.github/workflows/` (manual release run; versioning and release notes
are derived from commits via semantic-release).
