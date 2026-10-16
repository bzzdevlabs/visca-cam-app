#!/usr/bin/env bash
# Prints the CHANGELOG.md section of a version (its lines between "## [x.y.z]" and the next
# "## [" heading), used as the text of the GitHub release. Prints nothing when the version has no
# section, so the caller can fall back to generated notes.
# Usage: build/changelog-notes.sh <x.y.z[.build]> [CHANGELOG.md]
set -euo pipefail

version="$(cut -d. -f1-3 <<<"$1")"
changelog="${2:-CHANGELOG.md}"
awk -v heading="## [$version]" '
  index($0, heading) == 1 { found = 1; next }
  found && /^## \[/ { exit }
  found && /^\[[^]]+\]: / { exit }
  found { print }
' "$changelog" | sed -e '/./,$!d' | sed -e ':a' -e '/^\n*$/{$d;N;ba' -e '}'
