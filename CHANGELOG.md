# Changelog

What changed in each version of Tenveo PTZ, newest first. Versions follow
[semantic versioning](https://semver.org/); the fourth number of a release tag (`v0.5.4.73`) is
the build number. Each version's section is also the text of its
[GitHub release](https://github.com/bzzdevlabs/visca-cam-app/releases).

## [0.7.0] - 2026-10-16

### Added

- **French interface**, now the default. **Language** in the Settings panel switches between
  French and English; the app offers to restart to apply it. The log and the debug console stay
  in English.

## [0.6.0] - 2026-10-16

### Added

- **Settings panel**, opened with the gear button in the top bar or Ctrl+, (Windows), or
  **Tenveo PTZ > Settings...** (Cmd+,, macOS):
  - reverse left and right, reverse up and down (ceiling-mounted cameras);
  - continuous USB moves (experimental, Windows);
  - numeric keypad while another app is in front (Windows);
  - keep the window on top;
  - theme: same as the system, light or dark, now saved;
  - look for a new version on start-up;
  - open the folder holding the settings and presets;
  - **Reset all settings...**: restores every setting to its default after asking, disconnects
    the camera and keeps the presets.
- This changelog, also used as the text of each GitHub release.

### Changed

- The numeric keypad option moved from the *Pan and tilt* card to the Settings panel.

## [0.5.4] - 2026-10-16

### Fixed

- USB (UVC): the pad, the arrow keys and zoom did nothing on cameras that report continuous
  moves without honouring them. Moves use position steps again; continuous moves are an opt-in
  setting (`UvcContinuousMoves`).
- USB (UVC): presets came back to the wrong position when saved right after a move. They now
  store the position the app commanded, not the one the camera reports while still moving.
- USB (UVC): tilt moved at a third of the pan speed.
- The numeric keypad in the background now also accepts keys sent by macro pads and remapping
  tools (Stream Deck, AutoHotkey).

### Added

- `--simulator` adds a simulated PTZ camera with a live picture, to try the app without
  hardware. It also drives new end-to-end tests, including a live test of the app in CI.

## [0.5.3] - 2026-10-14

### Added

- Windows: the numeric keypad controls the camera while another app (Zoom, Teams...) is in
  front: with Num Lock off 8/4/6/2 move, 7 goes home and 9/3 zoom; with Num Lock on 1-9 recall
  presets; + and - zoom.

## [0.5.2] - 2026-10-14

### Added

- **Precise** switch, or hold Shift: moves at a quarter of the speed for fine framing.
- Windows: the window can be made narrow (down to 440 px); the controls then go under the video.

### Changed

- USB (UVC): continuous moves with the camera's own motion when it reports them, for smoother
  moves. *This left the controls dead on some cameras; fixed in 0.5.4.*
- The **Preview** switch can be toggled while connected.

## [0.5.1] - 2026-10-12

### Added

- **Preview** switch: turned off, the app controls the camera without taking its video, so Zoom,
  Teams or OBS can use the camera at the same time.
- [Documentation site](https://bzzdevlabs.github.io/visca-cam-app/).

### Changed

- Much slower camera moves; every step of the speed slider gives a different speed, and moving
  the slider while the camera moves changes the speed straight away.
- Pan and tilt directions reversed to match Tenveo cameras.
- USB (UVC): smaller, more frequent steps for smoother moves.

### Fixed

- The arrow, + and - keys (main keyboard and numeric keypad) and Page Up/Down keep driving the
  camera after clicking the speed slider or the preset list.

## [0.5.0] - 2026-10-11

### Added

- macOS app for Intel and Apple silicon, delivered as a disk image (`.dmg`).
- Windows: single-file app (`.exe`) and a per-user installer (`.msi`) that needs no administrator
  rights.

### Changed

- **Breaking:** updates download the single `.exe` or the `.dmg`. Versions released as a `.zip`
  cannot update themselves to this version: download it once by hand.

## [0.4.0] - 2026-10-11

### Added

- Daily log file (the last 7 days are kept) and a hidden debug console (Ctrl+Shift+D) showing the
  log, the protocol traffic and a raw VISCA command line.

### Fixed

- A crash that could happen while laying out a text box.

## [0.3.0] - 2026-10-11

### Added

- On start-up, the app offers to install a newer release and restarts into it.
- Buttons are exposed to screen readers and UI automation.

### Fixed

- Dialogs are sized to their content.

## [0.2.0] - 2026-10-09

### Added

- Windows 11 style window: Fluent controls, light and dark themes following Windows and its
  accent colour.
- Application icon.

### Fixed

- The speed slider can be used with the keyboard; Enter activates the focused dialog button.
- Held buttons always see their release, so the camera never keeps moving.
- The header stays on one row, with the serial settings on their own row; the preset toolbar
  fits on small screens.

## [0.1.0] - 2026-10-09

### Added

- First release of the Windows app for Tenveo PTZ cameras: live preview, pan, tilt, zoom and
  focus over USB (UVC) or a serial cable (VISCA), named presets, keyboard shortcuts, and settings
  remembered between runs.

[0.7.0]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.7.0.75
[0.6.0]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.6.0.74
[0.5.4]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.5.4.73
[0.5.3]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.5.3.72
[0.5.2]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.5.2.71
[0.5.1]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.5.1.70
[0.5.0]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.5.0.64
[0.4.0]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.4.0.56
[0.3.0]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.3.0.49
[0.2.0]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.2.0.41
[0.1.0]: https://github.com/bzzdevlabs/visca-cam-app/releases/tag/v0.1.0
