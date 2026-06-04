# Changelog

All notable changes to LocationGlue are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.2] — 2026-06-04

### Changed

- **Renamed** `LocationSteady` → `LocationGlue` across all files, binaries, and documentation. The project IS called LocationGlue now.
- **`-target:exe`** replaces `-target:winexe` — console output (status, logs) is now visible in the terminal.

### Added

- **Graceful shutdown via named `EventWaitHandle`.** Uninstall now signals the running instance to cleanly `Stop()` + `Dispose()` the `GeoCoordinateWatcher`, releasing the location session properly so Windows updates the tray icon immediately — **no reboot required.**
- **Explorer restart on uninstall.** Forces the taskbar to re-read location icon state, ensuring the icon disappears instantly after uninstall.
- **Enriched `status` command.** Now shows version, auto-start status with shortcut path, per-instance PID / start time / uptime / private memory, and total running count.

### Fixed

- **Uninstaller killed itself.** `Process.GetProcessesByName` now filters out the uninstaller's own PID so shortcut removal and explorer restart actually execute.
- **Icon description** in `research.md` corrected from "circle/dot" to "arrow."

---

## [1.1.1] — 2026-06-04

### Removed

- **`build.cmd`** — No longer needed. The compiled `LocationGlue.exe` is shipped directly in the repo. Developers can use the documented `csc.exe` one-liner to recompile from source.

## [1.1.0] — 2026-06-03

### Changed

- **`Setup.cmd`** — NEW smart entry point. Auto-detects install state and offers install, uninstall, status, or reinstall in a single menu.
- **`install.bat`** — Rewritten to call `LocationGlue.exe` directly (no PowerShell dependency). Better error handling, auto-finds exe in multiple paths.
- **`uninstall.bat`** — Rewritten with thorough cleanup: kill processes, remove shortcut, offer to delete all program files.
- **`InstallGlue.ps1`** — Marked as alternative method for PowerShell users. Primary UX is now `Setup.cmd` + `.bat` files.
- **README** — Updated to promote `Setup.cmd` as the primary one-click entry point.

### Fixed

- `install.bat` / `uninstall.bat` no longer fail if `InstallGlue.ps1` is missing — they call the exe directly.
- `uninstall.bat` now can uninstall even if `LocationGlue.exe` has been deleted (falls back to direct shortcut removal).

---

## [1.0.0] — 2026-06-03

### Added

- Initial release
- `LocationGlue.exe` — .NET Framework 4.8 background process that maintains a persistent `GeoCoordinateWatcher` session to keep the Windows 11 location icon always visible
- `InstallGlue.ps1` — PowerShell installer with `install`, `uninstall`, and `status` commands
- `install.bat` / `uninstall.bat` — one-click convenience wrappers for non-technical users
- `build.cmd` — one-command build using the built-in Windows C# compiler (`csc.exe`)
- Research documentation covering 5+ approaches to suppressing the location icon

[1.0.0]: https://github.com/example/LocationGlue/releases/tag/v1.0.0
