#!/usr/bin/env bash
# Publishes CI screenshots to the "ci-screenshots" branch and embeds them in the job summary,
# so they show directly on the Actions run page. Keeps the screenshots of the last $KEEP runs.
# Usage: build/publish-screenshots.sh <folder with .png files>
# Environment: GH_TOKEN, GITHUB_REPOSITORY, GITHUB_RUN_ID, GITHUB_RUN_ATTEMPT, GITHUB_STEP_SUMMARY.
set -euo pipefail

source_dir="$(cd "$1" && pwd)" # Absolute: the script changes directory before copying.
branch="ci-screenshots"
keep="${KEEP:-30}"
run="${GITHUB_RUN_ID}-${GITHUB_RUN_ATTEMPT:-1}"
remote="https://x-access-token:${GH_TOKEN}@github.com/${GITHUB_REPOSITORY}.git"
work="$(mktemp -d)"

shopt -s nullglob
images=("$source_dir"/*.png)
if (( ${#images[@]} == 0 )); then
  echo "## Screenshots" >> "$GITHUB_STEP_SUMMARY"
  echo ":warning: No screenshot was produced (see the smoke test step)." >> "$GITHUB_STEP_SUMMARY"
  exit 0
fi

# Exit code 2 means "no such branch"; anything else (network, permissions) is a real error.
status=0
git ls-remote --exit-code --heads "$remote" "$branch" > /dev/null || status=$?
if (( status == 0 )); then
  git clone --quiet --depth 1 --branch "$branch" "$remote" "$work"
  cd "$work"
elif (( status == 2 )); then
  cd "$work"
  git init --quiet
  git checkout --quiet --orphan "$branch"
  git remote add origin "$remote"
  echo "Screenshots taken by CI runs. Generated content, safe to delete." > README.md
else
  echo "Could not reach the $branch branch (git ls-remote exit code $status)." >&2
  exit 1
fi

git config user.name "github-actions[bot]"
git config user.email "41898282+github-actions[bot]@users.noreply.github.com"

mkdir -p "runs/$run"
cp "${images[@]}" "runs/$run/"
# Keep only the newest runs (run ids grow over time).
runs=$(ls -1 runs | sort -t- -k1,1n -k2,2n)
excess=$(( $(wc -l <<<"$runs") - keep ))
if (( excess > 0 )); then
  head -n "$excess" <<<"$runs" | while read -r old; do git rm -r --quiet "runs/$old"; done
fi
git add --all
git commit --quiet -m "chore: screenshots of run $run"

# Another run may push at the same time: rebase onto it and retry.
pushed=false
for attempt in 1 2 3; do
  if git push --quiet origin "HEAD:$branch"; then
    pushed=true
    break
  fi
  if ! git pull --quiet --rebase origin "$branch"; then
    git rebase --abort || true
    break
  fi
done
if [[ "$pushed" != true ]]; then
  echo "Could not push the screenshots to $branch." >&2
  exit 1
fi

{
  echo "## Screenshots"
  echo
  for image in "${images[@]}"; do
    name="$(basename "$image")"
    label="${name%.png}"
    label="${label#screenshot-}"
    echo "### ${label^}"
    echo
    echo "![${label} screenshot](https://raw.githubusercontent.com/${GITHUB_REPOSITORY}/${branch}/runs/${run}/${name})"
    echo
  done
} >> "$GITHUB_STEP_SUMMARY"
