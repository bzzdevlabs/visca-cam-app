---
sidebar_position: 5
title: Settings
---

# Settings

Open **Settings** with the gear button in the top bar (Windows) or **Tenveo PTZ > Settings...**
(Cmd+,) on macOS. Changes apply when you select **Save**.

| Section | Setting | Effect |
| --- | --- | --- |
| Camera | Reverse left and right / up and down | For a ceiling-mounted camera, or a camera that turns the wrong way. |
| Camera | Continuous USB moves (experimental, Windows) | USB (UVC): smoother moves using the camera's own motion. Some cameras ignore it or report wrong positions afterwards: turn it off if the controls stop working or presets come back wrong. |
| Keyboard | Numeric keypad works while another app is in front (Windows) | See [Move the camera](./controls.md#numeric-keypad-while-another-app-is-in-front-windows). |
| Window | Keep the window on top, Theme | Theme: same as the system, light or dark. |
| Updates | Look for a new version on start-up | Turns the [update check](./updates.md) on or off. |

**Reset all settings...** restores every setting to its default after asking: it disconnects the
camera and resets the camera choice, speed, keys, theme and other options. Presets are kept.
**Open folder** shows where settings and presets are stored.

## Settings files

Settings and presets are stored in:

| | Folder |
| --- | --- |
| Windows | `%APPDATA%\TenveoPTZ\` |
| macOS | `~/Library/Application Support/TenveoPTZ/` |

The folder holds `settings.xml` and `presets.xml`. Close the app before editing them by hand.

## Settings in settings.xml

Everything in the Settings panel is saved in `settings.xml`, alongside what the app remembers by
itself (camera, control method, port, baud rate, address, speed...):

| Setting | Default | Effect |
| --- | --- | --- |
| `InvertPan` | `false` | `true` reverses left and right, for a ceiling-mounted camera. |
| `InvertTilt` | `false` | `true` reverses up and down, for a ceiling-mounted camera. |
| `CheckForUpdates` | `true` | `false` turns off the [update check](./updates.md) on start-up. |
| `BackgroundNumpad` | `false` | `true` lets the numeric keypad drive the camera while another app is in front (Windows). |
| `AlwaysOnTop` | `false` | `true` keeps the window on top of other windows. |
| `Theme` | `System` | `Light` or `Dark` instead of following the system. |
| `UvcContinuousMoves` | `false` | USB (UVC): `true` moves with the camera's own continuous motion, which is smoother, instead of position steps. Experimental: some cameras report it without honouring it (the controls then do nothing) or report wrong positions after it (presets saved wrong). |

