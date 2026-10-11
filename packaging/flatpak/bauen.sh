#!/usr/bin/env bash
# Baut das Flatpak des Linux-Kopfs genau so, wie Flathub es baut (HANDOFF §4.108):
# aus dem Tag im Manifest, ohne Netz, und lintet zweimal (Manifest und Repo).
#
# ⚠ Gebaut wird der TAG aus dem Manifest, nicht der Arbeitsbaum.
#
# Voraussetzungen (einmalig, ohne sudo):
#   flatpak remote-add --user --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo
#   flatpak install --user flathub org.flatpak.Builder
#
# nuget-sources.json neu erzeugen (nach jeder Paketänderung und wenn Flathub die
# dotnet10-Erweiterung anhebt). Generator: flatpak/flatpak-builder-tools, dotnet/.
# ⚠ TMPDIR setzen — das /tmp der Sandbox ist zu klein, NuGet meldet dann „No space left":
#   flatpak run --env=TMPDIR=$HOME/.cache/nuget-tmp --command=sh --runtime=org.freedesktop.Sdk//25.08 \
#     --share=network --filesystem=host org.freedesktop.Sdk.Extension.dotnet10//25.08 -c \
#     'mkdir -p $TMPDIR; PATH=$PATH:/usr/lib/sdk/dotnet10/bin exec dotnet restore "$@"' -- \
#     --packages ~/.cache/nuget-pk src/GonkNote.Avalonia -r linux-x64 -p:SelfContained=true --disable-build-servers
#   danach aus ~/.cache/nuget-pk die JSON-Liste bauen wie flatpak-dotnet-generator.py.
#
# Aufruf:
#   ./bauen.sh              baut, lintet und installiert ins Nutzer-Flatpak
set -euo pipefail

HIER="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ID=io.github.gonkstupid.GonkNote
cd "$HIER"

echo "▶ 1/3  flathub-build"
flatpak run --command=flathub-build org.flatpak.Builder --install "$ID.yml"

echo "▶ 2/3  Lint: Manifest"
flatpak run --command=flatpak-builder-lint org.flatpak.Builder manifest "$ID.yml"

echo "▶ 3/3  Lint: Repo"
flatpak run --command=flatpak-builder-lint org.flatpak.Builder repo repo

echo
echo "✅ Fertig. Starten — NUR mit einer Kopie (HANDOFF Dauerregel 4):"
echo "   flatpak run $ID --db /tmp/gonk-test/gonknote.sqlite"
echo "   In der Sandbox liegt der Bestand unter ~/.var/app/$ID/config/GonkNote."
