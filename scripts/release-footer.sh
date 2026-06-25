#!/usr/bin/env bash
# Gibt einen statischen Installations-Abschnitt aus, der von semantic-release an die
# aus den Commits generierten Release Notes angehängt wird.
#
# Verwendung: scripts/release-footer.sh <version>
set -euo pipefail
V="${1:-}"

cat <<EOF

### Installation
1. Lade \`CategorySorter-v${V}.zip\` herunter und entpacke es.
2. Kopiere den Ordner \`CategorySorter/\` in das \`Mods/\`-Verzeichnis deines Servers.
3. Starte den Server neu. (Rein serverseitig — Clients müssen nichts installieren; EAC kann auf dem Dedicated Server aktiv bleiben.)

### Inhalt
\`\`\`
CategorySorter/
├── CategorySorter.dll
├── ModInfo.xml
└── Config/
    └── CategorySorter.xml
\`\`\`

Gebaut für **7 Days to Die V2.6**.
EOF
