# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- Service mode to have MIDI Recorder permanently on, or "Autostart" feature.
- Notification icon.

### Fixed

- Relaxed `global.json` SDK pinning so .NET 8, 9, and 10 SDKs work when building or running `dotnet` from a source checkout (#26).

### Changed

- Clarified README installation instructions: NuGet install vs GitHub source archives (#26).

## [1.3.0] - 2026-05-16

### Added

- Press **m** while recording to mark the last saved MIDI file by renaming it with a configurable suffix (`--marker`, default `_good`). Each save can be marked once per session; press any other key to quit.

## [1.2.1] - 2026-05-16

### Fixed

- Note on/off detection uses MIDI command codes so split and save work for devices that send `NoteOff` events (#17).
- `{NumberOfNoteEvents}` in the output path format counts note events correctly.
- Empty split windows no longer produce empty MIDI files.
- Live `NoteOn` events no longer carry a stale note length into saved files.

### Changed

- Centralized NuGet package versions in `Directory.Packages.props`.

## [1.2.0] - 2026-05-15

### Added
- `record` options `--raw-capture`, `--replay`, and `--replay-realtime`: write a Type 1 debug MIDI of the live input stream and replay from file (optional realtime pacing).

### Changed
- Retargeted from .NET 6 to .NET 8 with major-version roll-forward (runs on .NET 8, 9, or 10).
- Migrated solution to SLNX format.
- Updated NuGet dependencies to latest stable versions.
- Replaced Nuke with plain `dotnet` GitHub Actions; NuGet publish uses `NUGET_API_KEY` and GitHub Releases (see `.github/workflows/publish.yml`).

## [1.1.2] - 2023-02-19

### Fixed
- Sustain Pedal On was not correctly detected.

## [1.1.1] - 2023-02-16

### Added
- Check output path format string (option `-f`) before start recording.

## [1.1.0] - 2023-02-16

### Added
- Split files after all notes and sustain pedal are released.
- Create tracks by channel/port

## [1.0.3] - 2021-03-20

### Added

- Option `-r` to specify MIDI file resolution.

## [1.0.2] - 2021-03-20

### Fixed

- The default format didn't work correctly.

## [1.0.1] - 2021-03-20

### Added

- Shortened executable name to `midirec`.
- Binaries for Windows, Linux and Mac.

## [1.0.0] - 2021-03-20

### Added

- Automatic MIDI recording and saving to files
- Configurable list of MIDI inputs
- Configurable delay to trigger file save
- Configurable path for saved files
- `list` verb to list the MIDI inputs on your system
