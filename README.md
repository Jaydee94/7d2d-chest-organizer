# CategorySorter — 7 Days to Die (V2.6)

Serverseitige Mod, die Items aus einer **Sortierkiste** beim Schließen automatisch in
nahegelegene **Kategorie-Kisten** einsortiert. Die Kategorie einer Ziel-Kiste ergibt sich aus
ihrem Namen, der einer der spielinternen Item-Gruppen (`ItemClass.Groups`) entspricht.

## Funktionsweise

1. Eine normale Aufbewahrungskiste exakt **`[sort]`** benennen → Sortierkiste.
2. Weitere Kisten nach einer Item-Kategorie benennen (Tags sind **englisch**, Klammern optional,
   Groß/Klein egal), z. B.:
   - `Ammo/Weapons`
   - `Tools/Traps`
   - `Food/Cooking`
   - `Building`, `Resources`, `Medicine`, `Chemicals`, `Clothing`, `Decor`, `Forging/Molds`,
     `Basic`, `Miscellaneous`, `Special Items`
3. Optional eine Kiste **`[misc]`** benennen → fängt alles auf, was zu keiner Kategorie passt.
4. Items in `[sort]` legen, Kiste schließen → der Server verteilt automatisch und schickt eine
   kurze Chat-Rückmeldung.

Regeln:
- Ein Item passt in eine Kategorie-Kiste, wenn deren Name in den `Groups` des Items vorkommt.
- **Stapelbare und nicht stapelbare Items** werden gleichermaßen einsortiert (z. B. Werkzeuge,
  Waffen, Rüstung mit Haltbarkeit/Mods) — Haltbarkeit und Item-Mods bleiben erhalten.
- Bei mehreren passenden Kisten gewinnt die **nächstgelegene**.
- Volle Ziel-Kisten werden übersprungen; passt nichts und gibt es keine `[misc]`-Kiste, bleibt das
  Item in der Sortierkiste liegen.
- Gerade von Spielern geöffnete Kisten und unberührte Welt-Loot-Container werden ausgelassen.

## Gültige Kategorien

Standard-Gruppen aus `items.xml`:
`Basic, Ammo/Weapons, Tools/Traps, Building, Resources, Forging/Molds, Decor, Medicine,
Chemicals, Food/Cooking, Clothing, Miscellaneous, Special Items`.

## Konfiguration

`Config/CategorySorter.xml`:

| Feld | Default | Bedeutung |
|------|---------|-----------|
| `VerboseLogging` | `false` | Zusätzliche Log-Ausgaben |
| `MaxDistance` | `20.0` | Suchradius in Blöcken (`-1` = volle 3×3 Chunks) |
| `SortTag` | `[sort]` | Name der Sortierkiste |
| `MiscTag` | `[misc]` | Name der Sammelkiste |

## Bauen

Voraussetzung: .NET SDK (`dotnet`). Die Mod kompiliert plattformübergreifend gegen die
Spiel-DLLs (net48 via Referenz-Assemblies).

```bash
# nutzt standardmäßig die lokale macOS-Steam-Installation
./build.sh

# eigene Pfade (z. B. Dedicated Server unter Linux)
GAME_MANAGED="/srv/7dtd/7DaysToDieServer_Data/Managed" \
HARMONY="/srv/7dtd/Mods/0_TFP_Harmony" \
./build.sh

# direkt ins Mods-Verzeichnis deployen
DEPLOY_DIR="/Pfad/zu/7 Days To Die/Mods" ./build.sh
```

Das fertige Paket liegt unter `dist/CategorySorter/` und enthält `CategorySorter.dll`,
`ModInfo.xml` und `Config/`.

## Installation

Ordner `CategorySorter/` (mit DLL, `ModInfo.xml`, `Config/`) nach `<7DTD>/Mods/` kopieren.
Der offizielle **`0_TFP_Harmony`**-Loader muss vorhanden sein (bei V2.6 standardmäßig dabei).

### Server-Side & EAC

- **Rein serverseitig:** Nur der Server braucht die Mod; verbundene Clients müssen nichts
  installieren (es werden keine neuen Blöcke/Assets eingeführt — nur vorhandene Kisten per Namen
  markiert). Ergebnisse werden via `SetModified()` zu den Clients synchronisiert.
- **EAC:** Wie alle DLL/Harmony-Mods trägt `ModInfo.xml` `SkipWithAntiCheat="true"`. Auf einem
  **Dedicated Server** können Clients mit aktivem EAC verbinden; die Mod läuft nur serverseitig.
  Auf einem selbst gehosteten **Listen-Server (P2P)** muss EAC für den Host deaktiviert sein, damit
  die DLL geladen wird.

## CI & Releases

Zwei GitHub-Workflows (`.github/workflows/`):

- **CI** (`ci.yml`) — läuft bei Pull Requests auf `main` (und manuell). Lädt via SteamCMD
  **anonym** den 7DTD Dedicated Server (App 294420, gecacht) und kompiliert die Mod dagegen.
  Reiner Build-Check, kein Release.
- **Release** (`release.yml`) — **nur manuell** startbar:
  ```bash
  gh workflow run release.yml                      # echtes Release
  gh workflow run release.yml -f dry_run=true      # Probelauf ohne Veröffentlichung
  ```
  Baut die Mod und führt **semantic-release** aus: Version + Release Notes werden **aus den
  Commits** abgeleitet, `CHANGELOG.md` und `ModInfo.xml` werden aktualisiert/zurückgecommittet,
  ein Git-Tag `vX.Y.Z` gesetzt und ein GitHub-Release mit angehängtem `CategorySorter-vX.Y.Z.zip`
  erstellt. **Kein Steam-Account/Secret nötig** — die Build-DLLs kommen anonym über SteamCMD.

### Conventional Commits (Pflicht für Auto-Versionierung)

semantic-release leitet die Version aus den Commit-Typen ab:

| Commit-Präfix | Wirkung | Version |
|---------------|---------|---------|
| `fix: ...` | Bugfix | Patch (x.y.**z**) |
| `feat: ...` | Feature | Minor (x.**y**.0) |
| `feat!: ...` / `BREAKING CHANGE:` im Body | Breaking | Major (**x**.0.0) |
| `chore:`, `docs:`, `ci:`, `refactor:` … | kein Release | – |

Ohne mindestens einen `feat:`/`fix:`-Commit seit dem letzten Tag wird **kein** Release erzeugt.

## Technische Basis

Architektur und APIs sind an die funktionierende, serverseitige Mod
[Relvl/7DTD-SortingChest](https://github.com/Relvl/7DTD-SortingChest) angelehnt; der Kernunterschied
ist die **kategoriebasierte** Zuordnung (Kisten-Name ↔ `ItemClass.Groups`) statt
„existing-item-match". Hook: `GameManager.TEUnlockServer` (serverseitig beim Schließen einer Kiste).
