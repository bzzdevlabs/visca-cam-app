---
slug: /
sidebar_position: 1
title: Introduction
---

# Tenveo PTZ Controller

A small Windows and macOS app to drive a Tenveo (or any VISCA / UVC) PTZ camera connected by
USB: live preview, pan/tilt/zoom/focus, and named presets.

![The main window on Windows](/img/screenshots/screenshot-light.png)

## Features

- **Windows: a single file, no dependencies.** Runs on the .NET Framework 4.8 that ships with
  Windows 10 (1903 or later) and Windows 11. Use it as is, or install it with the `.msi`.
- **macOS: one app for Intel and Apple silicon Macs**, macOS 12 Monterey or later.
- **Two ways to control the camera:**
  - **UVC (USB)**: pan, tilt and zoom go through the same USB cable as the video. Use this
    when the camera has no serial cable connected.
  - **VISCA (COM port)**: classic Sony VISCA over the camera's RS-232/RS-485 port, through a
    USB-to-serial adapter. Presets are then stored in the camera itself.
- **Live preview** rendered by the system (DirectShow on Windows, AVFoundation on macOS), so it
  uses almost no CPU.
- **Named presets**, recalled with a double-click or the number keys `1` to `9`.
- **Keyboard control** of pan, tilt and zoom.
- **Automatic updates** from the GitHub releases.

## Where to go next

- [Install the app](./install.md) on Windows or macOS.
- [Connect to the camera](./usage/connect.md) and [move it](./usage/controls.md).
- [Save presets](./usage/presets.md) for the shots you use often.
- Something does not work? See [Troubleshooting](./troubleshooting.md).
