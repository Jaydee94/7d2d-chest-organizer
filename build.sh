#!/usr/bin/env bash
# Lokaler Build + Mod-Paket unter dist/CategorySorter/ (Ordner, für manuelles Deploy).
#
# Verwendung:
#   ./build.sh                                  # nutzt lokale macOS-Steam-Installation
#   GAME_ROOT=/opt/7dtd ./build.sh              # eigene Installation (Linux/Server)
#   MANAGED_DIR="/Pfad/zu/Managed" ./build.sh   # Managed-Ordner direkt vorgeben
#   DEPLOY_DIR="/Pfad/zu/7 Days To Die/Mods" ./build.sh   # zusätzlich ins Mods-Verzeichnis kopieren
set -euo pipefail

cd "$(dirname "$0")"
ASM="CategorySorter"

ARGS=(-c Release)
[ -n "${GAME_ROOT:-}" ]   && ARGS+=(-p:GameRoot="$GAME_ROOT")
[ -n "${MANAGED_DIR:-}" ] && ARGS+=(-p:ManagedDir="$MANAGED_DIR")

echo ">> dotnet build ${ARGS[*]}"
dotnet build "$ASM.csproj" "${ARGS[@]}"

OUT="dist/$ASM"
rm -rf "$OUT"
mkdir -p "$OUT/Config"
cp "bin/Release/$ASM.dll" "$OUT/"
[ -f "bin/Release/$ASM.pdb" ] && cp "bin/Release/$ASM.pdb" "$OUT/" || true
cp ModInfo.xml "$OUT/"
cp "Config/$ASM.xml" "$OUT/Config/"

echo ">> fertig: $OUT"
ls -la "$OUT"

if [ -n "${DEPLOY_DIR:-}" ]; then
  rm -rf "$DEPLOY_DIR/$ASM"
  mkdir -p "$DEPLOY_DIR"
  cp -R "$OUT" "$DEPLOY_DIR/$ASM"
  echo ">> deployed to $DEPLOY_DIR/$ASM"
fi
