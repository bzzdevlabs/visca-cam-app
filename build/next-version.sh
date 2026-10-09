#!/usr/bin/env bash
# Prints the next semantic version from the Conventional Commits since the latest v* tag:
# a breaking change bumps major, "feat" bumps minor, "fix"/"perf" bump patch.
# Prints nothing when no commit warrants a release (docs, ci, chore, test...).
# Usage: build/next-version.sh [revision]   (defaults to HEAD)
set -euo pipefail

revision="${1:-HEAD}"
latest_tag="$(git describe --tags --abbrev=0 --match 'v[0-9]*.[0-9]*.[0-9]*' "$revision" 2>/dev/null || true)"

if [[ -n "$latest_tag" ]]; then
  range="$latest_tag..$revision"
  IFS=. read -r major minor patch <<<"${latest_tag#v}"
else
  range="$revision"
  major=0 minor=0 patch=0
fi

bump=none
while IFS= read -r -d $'\x1e' message; do
  message="${message#$'\n'}" # git log puts a newline after each record separator.
  subject="${message%%$'\n'*}"
  if [[ "$subject" =~ ^[a-z]+(\([^\)]*\))?!: ]] || [[ "$message" == *"BREAKING CHANGE"* ]]; then
    bump=major
    break
  elif [[ "$subject" =~ ^feat(\([^\)]*\))?: ]]; then
    bump=minor
  elif [[ "$subject" =~ ^(fix|perf)(\([^\)]*\))?: ]] && [[ "$bump" == none ]]; then
    bump=patch
  fi
done < <(git log --format='%B%x1e' "$range")

case "$bump" in
  # Before 1.0.0 a breaking change only bumps the minor version.
  major) if (( major == 0 )); then minor=$((minor + 1)); patch=0; else major=$((major + 1)); minor=0; patch=0; fi ;;
  minor) minor=$((minor + 1)); patch=0 ;;
  patch) patch=$((patch + 1)) ;;
  none) exit 0 ;;
esac

echo "$major.$minor.$patch"
