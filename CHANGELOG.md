# Changelog

All notable changes to LocationGlue are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.0] — 2026-06-03

### Changed

- **`Setup.cmd`** — NEW smart entry point. Auto-detects install state and offers install, uninstall, status, or reinstall in a single menu.
- **`install.bat`** — Rewritten to call `LocationSteady.exe` directly (no PowerShell dependency). Better error handling, auto-finds exe in multiple paths.
- **`uninstall.bat`** — Rewritten with thorough cleanup: kill processes, remove shortcut, offer to delete all program files.
- **`InstallSteady.ps1`** — Marked as alternative method for PowerShell users. Primary UX is now `Setup.cmd` + `.bat` files.
- **README** — Updated to promote `Setup.cmd` as the primary one-click entry point.

### Fixed

- `install.bat` / `uninstall.bat` no longer fail if `InstallSteady.ps1` is missing — they call the exe directly.
- `uninstall.bat` now can uninstall even if `LocationSteady.exe` has been deleted (falls back to direct shortcut removal).

---

## [1.0.0] — 2026-06-03

### Added

- Initial release
- `LocationSteady.exe` — .NET Framework 4.8 background process that maintains a persistent `GeoCoordinateWatcher` session to keep the Windows 11 location icon always visible
- `InstallSteady.ps1` — PowerShell installer with `install`, `uninstall`, and `status` commands
- `install.bat` / `uninstall.bat` — one-click convenience wrappers for non-technical users
- `build.cmd` — one-command build using the built-in Windows C# compiler (`csc.exe`)
- Research documentation covering 5+ approaches to suppressing the location icon

[1.0.0]: https://github.com/example/LocationGlue/releases/tag/v1.0.0
