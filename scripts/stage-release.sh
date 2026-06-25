#!/usr/bin/env bash
# Stempelt die von semantic-release berechnete Version in ModInfo.xml und packt das
# Mod-Paket nach dist/CategorySorter-v<version>.zip (enthält Ordner CategorySorter/).
# Erwartet, dass die DLL bereits unter bin/Release/ gebaut wurde.
#
# Verwendung: scripts/stage-release.sh <version>
set -euo pipefail

VERSION="${1:?Version fehlt}"
ASM="CategorySorter"

# 7DTD-ModInfo erwartet eine 4-teilige Version -> ggf. auffüllen.
FOUR="$VERSION"
case "$VERSION" in
  *.*.*.*) ;;
  *.*.*)   FOUR="$VERSION.0" ;;
  *.*)     FOUR="$VERSION.0.0" ;;
  *)       FOUR="$VERSION.0.0.0" ;;
esac

sed -i.bak -E "s#(<Version value=\")[^\"]*(\")#\1${FOUR}\2#" ModInfo.xml && rm -f ModInfo.xml.bak

STAGING="staging/$ASM"
rm -rf staging dist
mkdir -p "$STAGING/Config" dist

cp "bin/Release/$ASM.dll" "$STAGING/"
[ -f "bin/Release/$ASM.pdb" ] && cp "bin/Release/$ASM.pdb" "$STAGING/" || true
cp ModInfo.xml "$STAGING/"
cp "Config/$ASM.xml" "$STAGING/Config/"
cp README.md "$STAGING/" 2>/dev/null || true

( cd staging && zip -r "../dist/${ASM}-v${VERSION}.zip" "$ASM" >/dev/null )
echo "staged dist/${ASM}-v${VERSION}.zip (ModInfo-Version ${FOUR})"
