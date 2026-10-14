---
sidebar_position: 2
title: Move the camera
---

# Move the camera

Move the camera with the pad (hold a button), or with the keyboard. The **Speed** slider sets
how fast pan, tilt and zoom move, from very slow on the left for fine framing; moving it while
the camera is moving changes the speed straight away. For fine framing, turn on **Precise** (or
hold Shift): moves then run at a quarter of the slider speed. **Autofocus** can be turned off to
focus by hand with **Near** and **Far**.

Over USB (UVC), the app uses the camera's own continuous motion when the camera offers it, which
is smooth at any speed. Cameras without it can only be moved one position step (often one
degree) at a time, so slow moves advance in small jumps.

On Windows, make the window narrower to put the controls under the video.

## Keyboard shortcuts

| Keys | Action |
| --- | --- |
| Arrow keys (hold, combine for diagonals) | Pan / tilt |
| Shift (hold, with the other keys or the pad) | Precise (slow) moves |
| `+` / `-` (main keyboard or numeric keypad) or Page Up / Page Down (hold; Fn + ↑ / ↓ on a Mac laptop) | Zoom in / out |
| Home (Fn + ← on a Mac laptop) | Home position |
| `1` … `9` | Go to preset 1 … 9 |

Holding a key moves the camera continuously until you release it. The keys keep driving the
camera after you click the speed slider or the preset list; only text fields take them.

## Ceiling-mounted cameras

To reverse the controls of a camera mounted upside down, set `InvertPan` and/or `InvertTilt`
in the [settings file](./settings.md).
