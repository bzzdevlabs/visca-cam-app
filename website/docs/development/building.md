---
sidebar_position: 1
title: Building from source
---

# Building from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) 10 or later.

## Windows app and tests

They build on Windows, macOS and Linux:

```bash
dotnet build --configuration Release
dotnet test --configuration Release
```

The Windows app is written to `src/TenveoPtz.App/bin/Release/net48/TenveoPTZ.exe`.

## Simulated camera

Start the app with `--simulator` to add a **PTZ Simulator** camera to the list: a software PTZ
camera with a live picture (a panorama with a degree grid and lettered landmarks), motors that
take time to move, and continuous moves for `UvcContinuousMoves`. It logs
`Simulator at pan P, tilt T, zoom Z.` each time it stops. Give it the quirks of some real cameras
with `--simulator=LaggingReadback,ReadbackIsLastTarget,IgnoresContinuousMoves`.

The same simulator drives the end-to-end scenario tests (`tests/TenveoPtz.Core.Tests/Scenarios`)
on a virtual clock, and `build/simulator-test.ps1` runs the real app on it in CI: held arrow and
zoom keys, presets saved with the **New** button and recalled with the number keys, and the
numeric keypad while Notepad is in front.

## Windows installer

The installer builds on Windows only (WiX Toolset 5, restored automatically), from the app built
above:

```bash
dotnet build src/TenveoPtz.Installer --configuration Release
```

## macOS app

The macOS app builds on a Mac with Xcode 26 and the .NET macOS workload. It is kept out of
`TenveoPtz.sln` so that the solution builds everywhere:

```bash
dotnet workload install macos
dotnet build src/TenveoPtz.Mac --configuration Release
build/make-dmg.sh "src/TenveoPtz.Mac/bin/Release/net10.0-macos26.0/Tenveo PTZ.app" dist/TenveoPTZ.dmg
```

## Version

Run `build/version.sh` to see the version the current commit would get (see
[Versioning](./ci.md#versioning)).
