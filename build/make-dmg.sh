#!/usr/bin/env bash
# Packs the macOS app into a compressed disk image with a shortcut to /Applications, so the
# user can drag the app there.
# Usage: build/make-dmg.sh <TenveoPTZ.app> <output .dmg>
set -euo pipefail

app="$1"
output="$2"
staging="$(mktemp -d)"
trap 'rm -rf "$staging"' EXIT

ditto "$app" "$staging/$(basename "$app")"
ln -s /Applications "$staging/Applications"
mkdir -p "$(dirname "$output")"
hdiutil create -volname "Tenveo PTZ" -srcfolder "$staging" -fs HFS+ -format UDZO -ov "$output"
hdiutil verify "$output"
