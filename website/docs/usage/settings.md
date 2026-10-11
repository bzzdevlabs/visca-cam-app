---
sidebar_position: 5
title: Settings files
---

# Settings files

Settings and presets are stored in:

| | Folder |
| --- | --- |
| Windows | `%APPDATA%\TenveoPTZ\` |
| macOS | `~/Library/Application Support/TenveoPTZ/` |

The folder holds `settings.xml` and `presets.xml`. Close the app before editing them by hand.

## Options set by hand

| Setting | Default | Effect |
| --- | --- | --- |
| `InvertPan` | `false` | `true` reverses left and right, for a ceiling-mounted camera. |
| `InvertTilt` | `false` | `true` reverses up and down, for a ceiling-mounted camera. |
| `CheckForUpdates` | `true` | `false` turns off the [update check](./updates.md) on start-up. |

The other settings (camera, control method, port, baud rate, address, speed...) are saved by the
app itself.
