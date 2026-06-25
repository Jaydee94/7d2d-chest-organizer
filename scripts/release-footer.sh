#!/usr/bin/env bash
# Prints a static installation section that semantic-release appends to the
# release notes generated from commits.
#
# Usage: scripts/release-footer.sh <version>
set -euo pipefail
V="${1:-}"

cat <<EOF

### Installation
1. Download \`CategorySorter-v${V}.zip\` and unzip it.
2. Copy the \`CategorySorter/\` folder into your server's \`Mods/\` directory.
3. Restart the server. (Server-side only — clients don't need to install anything; EAC can stay enabled on a dedicated server.)

### Contents
\`\`\`
CategorySorter/
├── CategorySorter.dll
├── ModInfo.xml
└── Config/
    └── CategorySorter.xml
\`\`\`

Built for **7 Days to Die V2.6**.
EOF
