---
sidebar_position: 2
title: Download and install
---

# Download and install

Every [release](https://github.com/bzzdevlabs/visca-cam-app/releases) has three files:

| File | For |
| --- | --- |
| `TenveoPTZ-x.y.z.n.exe` | Windows, no installation: put it anywhere and run it. |
| `TenveoPTZ-x.y.z.n.msi` | Windows installer: adds the app to the Start menu and to *Installed apps*. Installs for the current user only, so no administrator rights are needed. |
| `TenveoPTZ-x.y.z.n.dmg` | macOS (Intel and Apple silicon): open it and drag **Tenveo PTZ** onto **Applications**. |

The build artifacts of every CI run, including unreleased ones, are in the
[Actions](https://github.com/bzzdevlabs/visca-cam-app/actions) tab.

## Requirements

- **Windows** 10 (version 1903 or later) or Windows 11. The app runs on the .NET Framework 4.8
  that ships with Windows: nothing else to install.
- **macOS** 12 Monterey or later, on an Intel or Apple silicon Mac.
- A PTZ camera connected by USB. For VISCA control, a USB-to-serial adapter wired to the
  camera's RS-232 or RS-485 port.

## First start

The app is not code-signed, so the operating system warns about it the first time.

### Windows

SmartScreen may warn you the first time: choose **More info**, then **Run anyway**.

### macOS

The first time, macOS refuses to open the app because it cannot check it for malware:

1. Open **System Settings > Privacy & Security**.
2. Scroll down to the message about Tenveo PTZ and choose **Open Anyway**.
3. macOS then asks to allow the camera: choose **Allow**.

![The main window on macOS](/img/screenshots/screenshot-macOS-light.png)

## Uninstall

- **`.exe`**: delete the file.
- **`.msi`**: remove **Tenveo PTZ** from **Settings > Apps > Installed apps**.
- **macOS**: move **Tenveo PTZ** from **Applications** to the Bin.

Settings, presets and logs are kept; see [Settings files](./usage/settings.md) to remove them
too.
