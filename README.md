# Tenveo PTZ Controller

A small Windows app to drive a Tenveo (or any VISCA / UVC) PTZ camera connected by USB:
live preview, pan/tilt/zoom/focus, and named presets.

- **No installation, no dependencies.** Runs on the .NET Framework 4.8 that ships with
  Windows 10 (1903 or later) and Windows 11. The download is about 100 KB.
- **Two ways to control the camera:**
  - **UVC (USB)**: pan, tilt and zoom go through the same USB cable as the video. Use this
    when the camera has no serial cable connected.
  - **VISCA (COM port)**: classic Sony VISCA over the camera's RS-232/RS-485 port, through a
    USB-to-serial adapter. Presets are then stored in the camera itself.
- **Live preview** rendered by DirectShow, so it uses almost no CPU.

## Download and run

1. Download the latest `TenveoPTZ-x.y.z.zip` from the
   [Releases](../../releases) page (or a build artifact from the [Actions](../../actions) tab).
2. Extract it to a folder (keep `TenveoPTZ.exe` and `TenveoPtz.Core.dll` together).
3. Run `TenveoPTZ.exe`. The app is not code-signed, so Windows SmartScreen may warn you the
   first time: choose **More info**, then **Run anyway**.

## Using it

1. Pick the camera in **Camera** and the control method in **Control**:
   - *UVC (USB)* needs nothing else.
   - *VISCA (COM port)*: also pick the **Port**, **Baud** (usually 9600) and **Addr** (usually 1).
2. Press **Connect**. The app reconnects automatically the next time it starts.
3. Move the camera with the pad (hold a button), or with the keyboard.

| Keys | Action |
| --- | --- |
| Arrow keys (hold, combine for diagonals) | Pan / tilt |
| `+` / `-` or Page Up / Page Down (hold) | Zoom in / out |
| Home | Home position |
| `1` … `9` | Go to preset 1 … 9 |

**Presets:** frame the shot, press **Save new** and give it a name. Double-click a preset (or
press its number key) to go there. **Update** stores the current position into the selected
preset; ▲/▼ reorder the list, which changes the number keys.

Presets saved in VISCA mode live in the camera's memory (slots 1 to 89; slots from 90 upward
are avoided because many cameras use them for special functions such as opening the on-screen
menu). Presets saved in UVC mode store the absolute position on the computer.

### Settings files

Settings and presets are stored in `%APPDATA%\TenveoPTZ\` (`settings.xml`, `presets.xml`).
To reverse the controls of a ceiling-mounted camera, close the app and set `InvertPan` and/or
`InvertTilt` to `true` in `settings.xml`.

## Troubleshooting

- **"does not expose UVC pan/tilt/zoom controls"**: the camera's USB driver offers no PTZ
  controls. Connect its RS-232 port through a USB-to-serial adapter and use VISCA.
- **VISCA: nothing moves**: check the port, the baud rate and the address set in the camera's
  menu, and that the adapter's TX/RX wiring matches the camera's VISCA pinout.
- **Black preview**: another program (Teams, OBS, Zoom...) is probably using the camera.

## Building from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) 10 or later; works on Windows,
macOS and Linux (the application itself runs only on Windows).

```bash
dotnet build --configuration Release
dotnet test --configuration Release
```

The app is written to `src/TenveoPtz.App/bin/Release/net48/`.

### Continuous integration and releases

- **CI** (`ci.yml`): every pull request is built, unit-tested and smoke-tested on Windows
  (the app is started and its window checked; a screenshot is kept as an artifact).
- **Release** (`release.yml`): merging into `main` computes the next version from the
  [Conventional Commits](https://www.conventionalcommits.org/) since the last tag
  (`feat` bumps the minor version, `fix`/`perf` the patch, a breaking change the major), then
  tags it and publishes a GitHub release with the zipped app. Merges with only `docs`, `ci`,
  `chore` or `test` commits are tested but not released. Run `build/next-version.sh` to preview
  the next version locally.

### Architecture

| Project | Contents |
| --- | --- |
| `src/TenveoPtz.Core` | Platform-independent logic (.NET Standard 2.0): VISCA protocol, PTZ strategies, presets, settings, presenter. |
| `src/TenveoPtz.App` | Windows shell (.NET Framework 4.8): WinForms views, DirectShow video, serial port. |
| `tests/TenveoPtz.Core.Tests` | xUnit tests of the core library (test-only packages, never shipped). |

Design:

- **Model-View-Presenter.** `MainForm` is a passive view behind `IMainView`; `MainPresenter`
  holds all behaviour and is unit-tested against a fake view.
- **Strategy.** `IPtzController` has two implementations, `ViscaPtzController` (serial) and
  `UvcPtzController` (USB camera controls), chosen by `CameraSessionFactory`.
- **Command.** `ViscaCommands` builds immutable `ViscaCommand` objects that `ViscaClient`
  sends. A `BackgroundCommandExecutor` runs them off the UI thread and coalesces superseded
  moves so the camera always follows the latest input.
- **Repository.** `IStore<T>` / `XmlFileStore<T>` persist settings and presets atomically.
- **Composition root.** `Program` is the only place concrete Windows services are created.
