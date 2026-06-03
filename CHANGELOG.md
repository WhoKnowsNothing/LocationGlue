# Changelog

All notable changes to LocationGlue are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
