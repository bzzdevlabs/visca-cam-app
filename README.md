# Tenveo PTZ Controller

A small Windows and macOS app to drive a Tenveo (or any VISCA / UVC) PTZ camera connected by
USB: live preview, pan/tilt/zoom/focus, and named presets.

**Documentation:** <https://bzzdevlabs.github.io/visca-cam-app/>

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

## Download and run

Every [release](../../releases) has three files (build artifacts of any run are in the
[Actions](../../actions) tab):

| File | For |
| --- | --- |
| `TenveoPTZ-x.y.z.n.exe` | Windows, no installation: put it anywhere and run it. |
| `TenveoPTZ-x.y.z.n.msi` | Windows installer: adds the app to the Start menu and to *Installed apps*. Installs for the current user only, so no administrator rights are needed. |
| `TenveoPTZ-x.y.z.n.dmg` | macOS (Intel and Apple silicon): open it and drag **Tenveo PTZ** onto **Applications**. |

The app is not code-signed:

- **Windows**: SmartScreen may warn you the first time: choose **More info**, then **Run anyway**.
- **macOS**: the first time, macOS refuses to open it because it cannot check it for malware.
  Open **System Settings > Privacy & Security**, scroll down to the message about Tenveo PTZ
  and choose **Open Anyway**. macOS then asks to allow the camera: choose **Allow**.

## Using it

1. Pick the camera in **Camera** and the control method in **Control**:
   - *UVC (USB)* needs nothing else.
   - *VISCA (COM port)*: also pick the **Port**, **Baud** (usually 9600) and **Addr** (usually 1).
     On macOS, USB-to-serial adapters appear as `usbserial-…` or `usbmodem…`.
2. Press **Connect**. The app reconnects automatically the next time it starts.
3. Move the camera with the pad (hold a button), or with the keyboard.

| Keys | Action |
| --- | --- |
| Arrow keys (hold, combine for diagonals) | Pan / tilt |
| `+` / `-` or Page Up / Page Down (hold; Fn + ↑ / ↓ on a Mac laptop) | Zoom in / out |
| Home (Fn + ← on a Mac laptop) | Home position |
| `1` … `9` | Go to preset 1 … 9 |

**Presets:** frame the shot, press **Save new** and give it a name. Double-click a preset (or
press its number key) to go there. **Update** stores the current position into the selected
preset; ▲/▼ reorder the list, which changes the number keys.

Presets saved in VISCA mode live in the camera's memory (slots 1 to 89; slots from 90 upward
are avoided because many cameras use them for special functions such as opening the on-screen
menu). Presets saved in UVC mode store the absolute position on the computer.

### Updates

On start-up the app checks the [Releases](../../releases) page for a newer version. When one
exists it asks whether to install it: **Update** downloads it, replaces the app (`TenveoPTZ.exe`
on Windows, `Tenveo PTZ.app` on macOS) and restarts it; **Not now** asks again on the next
start. Without internet access the check is silently skipped. If the app's folder is not
writable, the status line says so: download the new version by hand instead. On macOS the app
must have been moved out of the disk image (to **Applications**) to update itself.

Versions released as a `.zip` (before the single `.exe`) cannot update themselves to newer
releases: download the `.exe` or `.msi` once by hand.

### Settings

The gear button in the top bar or Ctrl+, (Windows), or **Settings...** (Cmd+,, macOS) opens the settings:
reversed directions for a ceiling-mounted camera, continuous USB moves, the numeric keypad while
another app is in front, window on top, theme, language (French by default, or English), the
update check, and a reset of every setting (presets are kept). They are stored with the presets in `%APPDATA%\TenveoPTZ\` on Windows and in
`~/Library/Application Support/TenveoPTZ/` on macOS (`settings.xml`, `presets.xml`).

See [CHANGELOG.md](CHANGELOG.md) for what changed in each version.

## Troubleshooting

- **"does not expose UVC pan/tilt/zoom controls"**: the camera's USB driver offers no PTZ
  controls. Connect its RS-232 port through a USB-to-serial adapter and use VISCA.
- **VISCA: nothing moves**: check the port, the baud rate and the address set in the camera's
  menu, and that the adapter's TX/RX wiring matches the camera's VISCA pinout.
- **Black preview**: another program (Teams, OBS, Zoom...) is probably using the camera. To
  control the camera while that program shows its video, turn **Preview** off, then Connect.
- **macOS: "may not use the camera"**: allow Tenveo PTZ in **System Settings > Privacy &
  Security > Camera**, then restart the app.

### Logs and debug console

The app keeps a log in `%APPDATA%\TenveoPTZ\logs\` on Windows and in
`~/Library/Logs/TenveoPTZ/` on macOS (one `tenveo-yyyymmdd.log` file per day, the last 7 days
are kept): start-up, devices found, connections, status messages and errors with their details.

On Windows, press **Ctrl+Shift+D** in the main window to open the hidden debug console:

- the live log;
- **Verbose logging**: also records every VISCA packet sent and received (`TX` / `RX`) and
  every pan/tilt/zoom request, until the app is closed;
- **Copy diagnostics**: copies the app and Windows versions, the settings and the recent log
  to the clipboard, to paste into a bug report;
- **Open log folder**;
- **Raw VISCA** (VISCA mode only): sends bytes typed by hand, either the payload (`01 06 04`)
  or the full packet (`81 01 06 04 FF`); the camera's reply is logged, which also makes it
  possible to read inquiries such as `09 04 47` (zoom position).

Starting the app with `TenveoPTZ.exe --debug` turns verbose logging on from the start and
opens the console.

## Building from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) 10 or later. The Windows app
and the tests build on Windows, macOS and Linux:

```bash
dotnet build --configuration Release
dotnet test --configuration Release
```

The Windows app is written to `src/TenveoPtz.App/bin/Release/net48/TenveoPTZ.exe`.

The Windows installer builds on Windows only (WiX Toolset 5, restored automatically), from the
app built above:

```bash
dotnet build src/TenveoPtz.Installer --configuration Release
```

The macOS app builds on a Mac with Xcode 26 and the .NET macOS workload; it is kept out of
`TenveoPtz.sln` so that the solution builds everywhere:

```bash
dotnet workload install macos
dotnet build src/TenveoPtz.Mac --configuration Release
build/make-dmg.sh "src/TenveoPtz.Mac/bin/Release/net10.0-macos26.0/Tenveo PTZ.app" dist/TenveoPTZ.dmg
```

### Continuous integration and releases

- **CI** (`ci.yml`): every pull request is built, unit-tested and smoke-tested on Windows (the
  app is started in the light and dark themes, and the `.msi` is installed and uninstalled) and
  on macOS (the app is started in the light theme as Apple silicon code and in the dark theme as
  Intel code, through Rosetta). Both run on GitHub-hosted runners, which are free for public
  repositories. The run page shows the version, a test report with every test's result, and
  screenshots of the windows (stored on the `ci-screenshots` branch, which keeps the last 30 runs).
- **Release** (`release.yml`): merging into `main` publishes a GitHub release with the Windows
  `.exe` and `.msi` and the macOS `.dmg`.
- **Versioning**: `major.minor.patch.build`, as Windows file versions. `major.minor.patch` comes
  from the [Conventional Commits](https://www.conventionalcommits.org/) since the last tag
  (`feat` bumps the minor version, `fix`/`perf` the patch, a breaking change the major); `build`
  is the number of commits in the history, so it grows with every change and a rebuild of the
  same commit gets the same number. Merges with only `docs`, `ci`, `chore` or `test` commits are
  tested but not released. Run `build/version.sh` to see the current version locally.

### Architecture

| Project | Contents |
| --- | --- |
| `src/TenveoPtz.Core` | Platform-independent logic (.NET Standard 2.0): VISCA protocol, PTZ strategies, presets, settings, presenter. |
| `src/TenveoPtz.App` | Windows shell (.NET Framework 4.8): WinForms views, DirectShow video, serial port. The core sources are compiled into it, so the app is a single `.exe`. |
| `src/TenveoPtz.Installer` | Windows installer (WiX Toolset 5), per-user. |
| `src/TenveoPtz.Mac` | macOS shell (.NET for macOS, universal Intel/Apple silicon): AppKit views, AVFoundation video, UVC camera controls through IOKit, serial port. |
| `tests/TenveoPtz.Core.Tests` | xUnit tests of the core library (test-only packages, never shipped). |

Design:

- **Model-View-Presenter.** `MainForm` (Windows) and `MainWindow` (macOS) are passive views behind `IMainView`; `MainPresenter`
  holds all behaviour and is unit-tested against a fake view.
- **Strategy.** `IPtzController` has two implementations, `ViscaPtzController` (serial) and
  `UvcPtzController` (USB camera controls), chosen by `CameraSessionFactory`.
- **Command.** `ViscaCommands` builds immutable `ViscaCommand` objects that `ViscaClient`
  sends. A `BackgroundCommandExecutor` runs them off the UI thread and coalesces superseded
  moves so the camera always follows the latest input.
- **Repository.** `IStore<T>` / `XmlFileStore<T>` persist settings and presets atomically.
- **Composition root.** `Program` (Windows) and `AppDelegate` (macOS) are the only places
  concrete platform services are created.
