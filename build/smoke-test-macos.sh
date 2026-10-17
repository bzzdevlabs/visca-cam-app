#!/usr/bin/env bash
# Starts the macOS app once per theme, checks that it shows its window and exits cleanly when
# the window is closed, and saves a screenshot of the window. The light run uses the native
# (Apple silicon) code and the dark run the Intel code through Rosetta, so both halves of the
# universal binary are exercised. Used by CI; also handy on a test Mac.
# Usage: build/smoke-test-macos.sh <TenveoPTZ.app> <screenshot folder> [--sample-presets]
#   --sample-presets writes sample presets and settings to ~/Library/Application Support/TenveoPTZ
#   first (the light run uses USB control in French, the dark run serial control in English). Do not use it on a Mac
#   whose settings matter.
set -euo pipefail

app="$1"
screenshots="$(mkdir -p "$2" && cd "$2" && pwd)"
sample="${3:-}"
timeout_seconds=30
executable="$app/Contents/MacOS/TenveoPTZ"
data="$HOME/Library/Application Support/TenveoPTZ"

echo "Architectures: $(lipo -archs "$executable")"
codesign --verify --deep --strict "$app"
echo "Code signature is valid."

if [[ "$sample" == "--sample-presets" ]]; then
  mkdir -p "$data"
  cat > "$data/presets.xml" <<'XML'
<?xml version="1.0" encoding="utf-8"?>
<Presets>
  <Preset><Name>Wide shot</Name><Slot>1</Slot></Preset>
  <Preset><Name>Lectern</Name><Slot>2</Slot></Preset>
  <Preset><Name>Platform left</Name><Slot>3</Slot></Preset>
  <Preset><Name>Platform right</Name><Slot>4</Slot></Preset>
  <Preset><Name>Audience</Name><Slot>5</Slot></Preset>
</Presets>
XML
fi

run() {
  local theme="$1" mode="$2" arch="$3" language="$4"
  if [[ "$sample" == "--sample-presets" ]]; then
    cat > "$data/settings.xml" <<XML
<?xml version="1.0" encoding="utf-8"?>
<AppSettings>
  <Connection><Mode>$mode</Mode><BaudRate>9600</BaudRate><Address>1</Address></Connection>
  <Speed>0.5</Speed>
  <Language>$language</Language>
</AppSettings>
XML
  fi

  local image="$screenshots/screenshot-macOS-$theme.png"
  rm -f "$image"
  # The app saves the screenshot itself once its window is up, then closes the window.
  arch "-$arch" "$executable" "--theme=$theme" "--screenshot=$image" &
  local pid=$! elapsed=0
  while kill -0 "$pid" 2>/dev/null; do
    if (( elapsed >= timeout_seconds )); then
      kill "$pid" || true
      echo "[$theme/$arch] The application did not close its window within $timeout_seconds seconds (an error dialog?)." >&2
      exit 1
    fi
    sleep 1
    elapsed=$((elapsed + 1))
  done

  local status=0
  wait "$pid" || status=$?
  if (( status != 0 )); then
    echo "[$theme/$arch] The application exited with code $status." >&2
    exit 1
  fi
  if [[ ! -s "$image" ]]; then
    echo "[$theme/$arch] No screenshot was saved." >&2
    exit 1
  fi
  echo "[$theme/$arch] Window shown and closed cleanly; screenshot saved to $image"
}

run light Uvc arm64 French
run dark Visca x86_64 English
