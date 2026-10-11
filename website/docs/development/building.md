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
