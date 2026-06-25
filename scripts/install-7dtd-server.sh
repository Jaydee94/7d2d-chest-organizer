#!/usr/bin/env bash
# Lädt den 7 Days to Die Dedicated Server (Steam App 294420) anonym via SteamCMD
# herunter. Dessen Managed-Ordner liefert die Build-DLLs (Assembly-CSharp, Unity, ...).
# Wird in CI verwendet; Ergebnis wird gecacht.
#
# Verwendung: scripts/install-7dtd-server.sh <SERVER_ROOT>
set -euo pipefail

SERVER_ROOT="${1:?SERVER_ROOT (Zielpfad) fehlt}"

mkdir -p "$HOME/steamcmd"
cd "$HOME/steamcmd"
if [ ! -f steamcmd.sh ]; then
  curl -fsSL "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz" -o steamcmd_linux.tar.gz
  tar -xzf steamcmd_linux.tar.gz
fi

mkdir -p "$SERVER_ROOT"

install_server() {
  ./steamcmd.sh \
    +force_install_dir "$SERVER_ROOT" \
    +login anonymous \
    +app_info_update 1 \
    +app_update 294420 validate \
    +quit
}

if ! install_server; then
  echo "Erster SteamCMD-Versuch fehlgeschlagen; appmanifest bereinigen und erneut versuchen..."
  rm -f "$SERVER_ROOT/steamapps/appmanifest_294420.acf"
  install_server
fi

if [ ! -f "$SERVER_ROOT/7DaysToDieServer_Data/Managed/Assembly-CSharp.dll" ] \
   && [ ! -f "$SERVER_ROOT/7DaysToDie_Data/Managed/Assembly-CSharp.dll" ]; then
  echo "::error::Assembly-CSharp.dll nach der Server-Installation nicht gefunden."
  exit 1
fi

echo "7DTD Dedicated Server bereit unter $SERVER_ROOT"
