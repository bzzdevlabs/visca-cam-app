#!/usr/bin/env bash
# Computes the Windows-style version of a commit: major.minor.patch.build
#  - major.minor.patch: next semantic version from the Conventional Commits since the latest
#    v* tag (breaking change: major, "feat": minor, "fix"/"perf": patch). When no commit
#    warrants a release (docs, ci, chore, test...), the latest tag's version is kept.
#  - build: number of commits in the history ("git height"). It grows with every commit and is
#    identical when the same commit is rebuilt. Needs a full clone (fetch-depth: 0).
# Prints GitHub Actions outputs:  version=0.2.0.57  release=true|false
# Usage: build/version.sh [revision]   (defaults to HEAD)
set -euo pipefail

revision="${1:-HEAD}"
if [[ "$(git rev-parse --is-shallow-repository)" == "true" ]]; then
  echo "version.sh needs the full history: check out with fetch-depth: 0." >&2
  exit 1
fi

latest_tag="$(git describe --tags --abbrev=0 --match 'v[0-9]*.[0-9]*.[0-9]*' "$revision" 2>/dev/null || true)"
if [[ -n "$latest_tag" ]]; then
  range="$latest_tag..$revision"
  IFS=. read -r major minor patch _ <<<"${latest_tag#v}"
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
esac

build="$(git rev-list --count "$revision")"
echo "version=$major.$minor.$patch.$build"
echo "release=$([[ "$bump" == none ]] && echo false || echo true)"
