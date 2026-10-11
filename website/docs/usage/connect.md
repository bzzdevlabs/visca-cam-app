---
sidebar_position: 1
title: Connect to the camera
---

# Connect to the camera

1. Pick the camera in **Camera** and the control method in **Control**:
   - **UVC (USB)** needs nothing else.
   - **VISCA (COM port)**: also pick the **Port**, **Baud** (usually 9600) and **Addr**
     (usually 1). On macOS, USB-to-serial adapters appear as `usbserial-…` or `usbmodem…`.
2. Press **Connect**.

The app remembers the camera and the control method, and reconnects automatically the next time
it starts.

## Which control method?

| | UVC (USB) | VISCA (COM port) |
| --- | --- | --- |
| Cable | The USB cable that carries the video. | An extra RS-232/RS-485 cable through a USB-to-serial adapter. |
| Pan, tilt, zoom | Yes, when the camera's USB driver exposes them. | Yes. |
| Presets stored | On the computer (absolute positions). | In the camera's memory. |

Try UVC first. If the app reports that the camera *does not expose UVC pan/tilt/zoom controls*,
use VISCA.
