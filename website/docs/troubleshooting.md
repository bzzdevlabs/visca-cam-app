---
sidebar_position: 4
title: Troubleshooting
---

# Troubleshooting

## Common problems

### “does not expose UVC pan/tilt/zoom controls”

The camera's USB driver offers no PTZ controls. Connect its RS-232 port through a
USB-to-serial adapter and use [VISCA](./usage/connect.md).

### VISCA: nothing moves

Check the port, the baud rate and the address set in the camera's menu, and that the
adapter's TX/RX wiring matches the camera's VISCA pinout.

### Black preview

Another program (Teams, OBS, Zoom...) is probably using the camera.

### macOS: “may not use the camera”

Allow Tenveo PTZ in **System Settings > Privacy & Security > Camera**, then restart the app.

## Logs

The app keeps a log of start-up, devices found, connections, status messages and errors with
their details:

| | Folder |
| --- | --- |
| Windows | `%APPDATA%\TenveoPTZ\logs\` |
| macOS | `~/Library/Logs/TenveoPTZ/` |

There is one `tenveo-yyyymmdd.log` file per day; the last 7 days are kept.

## Debug console (Windows)

Press **Ctrl+Shift+D** in the main window to open the hidden debug console:

![The debug console](/img/screenshots/screenshot-light-debug-console.png)

- the live log;
- **Verbose logging**: also records every VISCA packet sent and received (`TX` / `RX`) and
every pan/tilt/zoom request, until the app is closed;
- **Copy diagnostics**: copies the app and Windows versions, the settings and the recent log
to the clipboard, to paste into a bug report;
- **Open log folder**;
- **Raw VISCA** (VISCA mode only): sends bytes typed by hand, either the payload (`01 06 04`)
or the full packet (`81 01 06 04 FF`). The camera's reply is logged, which also makes it
possible to read inquiries such as `09 04 47` (zoom position).

Starting the app with `TenveoPTZ.exe --debug` turns verbose logging on from the start and opens
the console.

## Reporting a bug

Open an [issue](https://github.com/bzzdevlabs/visca-cam-app/issues) with the app version, the
camera model, the control method and, on Windows, the output of **Copy diagnostics**.
