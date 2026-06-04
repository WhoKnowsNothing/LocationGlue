<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows%2011-0078D6?logo=windows&logoColor=white" alt="Windows 11">
  <img src="https://img.shields.io/badge/.NET-4.8-512BD4?logo=.net&logoColor=white" alt=".NET Framework 4.8">
  <img src="https://img.shields.io/badge/size-13.5%20KB-brightgreen" alt="13.5 KB">
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License">
</p>

<h1 align="center">📍 LocationGlue</h1>
<p align="center"><strong>Keep the Windows 11 "Location In Use" tray icon calm — no more blinking.</strong></p>

---

[English](#english) | [中文](#中文)

---

<a name="english"></a>
## English

### What Is This?

Windows 11 shows a "Location In Use" icon (an arrow) in the system tray whenever any app accesses your location. The icon **disappears and reappears** whenever an app starts or stops using location, causing a distracting blink.

**LocationGlue** maintains a lightweight, persistent location session so the icon stays **always visible** — no blinking, no distraction.

### How It Works

Instead of fighting Windows to hide the icon (which doesn't work reliably on newer builds), LocationGlue opens a single `GeoCoordinateWatcher` session with minimal accuracy and a 10km movement threshold. The icon stays on steadily. Auto-starts via Task Scheduler at every logon — no window, no console. That's it.

| Metric | Value |
|--------|-------|
| Binary size | 13.5 KB |
| Private memory | ~8 MB |
| CPU usage | 0% when idle |
| Window | None (Task Scheduler runs hidden at logon) |
| Dependencies | .NET Framework 4.8 (built into Windows 11) |
| Battery impact | Minimal (30-min interval, low accuracy) |

### Quick Start

**Double-click `Setup.cmd`** — the only file you need. It auto-detects your install state:

- **Not installed?** Press `I` to install and start immediately (auto-start via Task Scheduler at every logon).
- **Already installed?** Press `U` to uninstall, `S` for status, `R` to reinstall.

One file, one click. Done.

> **Terminal users:** `LocationGlue.exe install|uninstall|status` works directly, or `powershell -File src\InstallGlue.ps1 -Command install`.

### Build from Source

No extra tools required — uses the C# compiler included with Windows:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe `
  -out:src\LocationGlue.exe -target:winexe -platform:x64 -optimize `
  -reference:System.Device.dll -reference:Microsoft.CSharp.dll `
  src\ProgramFx.cs
```

### FAQ

**Q: Is this a virus?** No. The source code is ~250 lines of C# — read it yourself. It only calls `GeoCoordinateWatcher.Start()` and sleeps. No network, no files. Creates only a Task Scheduler task for auto-start.

**Q: Will this affect my privacy?** No. The location data is never read, stored, or transmitted. The watcher is opened purely to keep the icon visible.

**Q: Why not just hide the icon via registry?** We researched that — see [`research.md`](research.md). Registry approaches exist but can be reset by Windows updates. LocationGlue is a complementary, defense-in-depth solution.

**Q: Does it work on Windows 10?** Not tested, but likely yes — .NET Framework 4.8 and `System.Device.Location` are both available on Windows 10.

---

<a name="中文"></a>
## 中文

### 这是什么？

Windows 11 在系统托盘区显示一个"位置正在使用"图标（一个箭头）。每当任何应用访问位置时，图标就会出现/消失，造成**闪烁**，非常分散注意力。

**LocationGlue** 维持一个轻量、持久的位置会话，让图标**始终可见**——不再闪烁、不再干扰。

### 工作原理

与其跟 Windows 斗智斗勇试图隐藏图标（在较新版本上不可靠），LocationGlue 打开一个 `GeoCoordinateWatcher` 会话，使用最低精度和 10 公里移动阈值。图标稳定常亮。就这么简单。

| 指标 | 数值 |
|------|------|
| 二进制大小 | 11.5 KB |
| 私有内存 | ~8 MB |
| CPU 占用 | 空闲时 0% |
| 窗口 | 无（后台进程） |
| 依赖 | .NET Framework 4.8（Windows 11 内置） |
| 电池影响 | 极低（30 分钟间隔，低精度） |

### 快速开始

**双击 `Setup.cmd`** — 唯一需要的文件，自动检测安装状态：

- **未安装？** 按 `I` 安装并立即启动。
- **已安装？** 按 `U` 卸载、`S` 查看状态、`R` 重新安装。

一个文件，一键搞定。

> **终端用户:** `LocationGlue.exe install|uninstall|status` 直接可用，或 `powershell -File src\InstallGlue.ps1 -Command install`。

### 从源码编译

无需额外工具——使用 Windows 自带的 C# 编译器：

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe `
  -out:src\LocationGlue.exe -target:winexe -platform:x64 -optimize `
  -reference:System.Device.dll -reference:Microsoft.CSharp.dll `
  src\ProgramFx.cs
```

### 常见问题

**Q: 这是病毒吗？** 不是。源代码只有 ~250 行 C#——自己看。它只调用 `GeoCoordinateWatcher.Start()` 然后休眠。无网络、无文件写入。仅创建一个 Task Scheduler 任务用于开机自启。

**Q: 会影响隐私吗？** 不会。位置数据从未被读取、存储或传输。打开 watcher 纯粹是为了保持图标可见。

**Q: 为什么不直接通过注册表隐藏图标？** 我们研究过——见 [`research.md`](research.md)。注册表方法存在但可能被 Windows 更新重置。LocationGlue 是互补的纵深防御方案。

**Q: Windows 10 能用吗？** 未测试，但大概率可以——.NET Framework 4.8 和 `System.Device.Location` 在 Windows 10 上都可用。

---

## License

MIT © 2026 LocationGlue contributors. See [LICENSE](LICENSE) for details.

## Related Research

See [`research.md`](research.md) for a deep dive into Windows 11 location icon suppression approaches, including registry methods, ACL hardening, and COM interface analysis.
