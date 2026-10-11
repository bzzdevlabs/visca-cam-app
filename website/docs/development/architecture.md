---
sidebar_position: 2
title: Architecture
---

# Architecture

## Projects

| Project | Contents |
| --- | --- |
| `src/TenveoPtz.Core` | Platform-independent logic (.NET Standard 2.0): VISCA protocol, PTZ strategies, presets, settings, presenter. |
| `src/TenveoPtz.App` | Windows shell (.NET Framework 4.8): WinForms views, DirectShow video, serial port. The core sources are compiled into it, so the app is a single `.exe`. |
| `src/TenveoPtz.Installer` | Windows installer (WiX Toolset 5), per-user. |
| `src/TenveoPtz.Mac` | macOS shell (.NET for macOS, universal Intel/Apple silicon): AppKit views, AVFoundation video, UVC camera controls through IOKit, serial port. |
| `tests/TenveoPtz.Core.Tests` | xUnit tests of the core library (test-only packages, never shipped). |
| `website` | This documentation site (Docusaurus). |

## Design

- **Model-View-Presenter.** `MainForm` (Windows) and `MainWindow` (macOS) are passive views
  behind `IMainView`; `MainPresenter` holds all behaviour and is unit-tested against a fake view.
- **Strategy.** `IPtzController` has two implementations, `ViscaPtzController` (serial) and
  `UvcPtzController` (USB camera controls), chosen by `CameraSessionFactory`.
- **Command.** `ViscaCommands` builds immutable `ViscaCommand` objects that `ViscaClient` sends.
  A `BackgroundCommandExecutor` runs them off the UI thread and coalesces superseded moves so
  the camera always follows the latest input.
- **Repository.** `IStore<T>` / `XmlFileStore<T>` persist settings and presets atomically.
- **Composition root.** `Program` (Windows) and `AppDelegate` (macOS) are the only places
  concrete platform services are created.
