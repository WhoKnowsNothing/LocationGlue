# Hide "Location In Use" Icon — Research Notes

## What Is This Icon?

The "Location In Use" icon (a circle/dot) appears in the Windows 11 system tray when any
application accesses the device's location. It is NOT a regular `Shell_NotifyIcon` icon from
a third-party app — it is rendered by `explorer.exe` itself as part of the Windows shell.

## Approach Comparison

| Approach | Complexity | Permanence | Risk | Background Process |
|----------|-----------|------------|------|-------------------|
| Reg: `LocationNotificationsAllowed=0` | Minimal | High (survives reboots) | Low | **None** (logon script) |
| Reg: `ShowGlobalPrompts=0` (secondary) | Minimal | High | Low | **None** |
| Reg: ACL hardening | Low | Very High | Low-Medium | **None** |
| Built-in Settings toggle (Build 25977+) | None | High | None | **None** |
| `TrayNotify\IconStreams` patch | Medium | Medium (version-dep) | Medium | **None** |
| `ITrayNotify` COM interface | High | High | High (undocumented) | **None** |
| DLL injection (Windhawk-style) | Very High | Very High | High | **Continuous** |
| Disable Location Service entirely | Minimal | N/A | Low (breaks features) | **None** |

---

## Approach 1: `LocationNotificationsAllowed` Registry Key

**Key:** `HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy`
**Value:** `LocationNotificationsAllowed` (DWORD, 0=hide icon, 1=show icon)

This is the **OFFICIAL** mechanism. Explorer checks this value when deciding whether to
show the location notification icon. Setting it to 0 hides the icon while keeping
location services fully functional.

**Persistence:** Windows does NOT reset this value during normal sessions. It may be
reset during major feature updates. A one-shot logon scheduled task is sufficient for
the vast majority of use cases.

---

## Approach 2: `ShowGlobalPrompts` Registry Key (NEW)

**Key:** `HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location`
**Value:** `ShowGlobalPrompts` (DWORD, 0=disable prompts, 1=enable)

This is a secondary mechanism that controls whether global location prompts are shown.
Setting it to 0 provides defense-in-depth alongside `LocationNotificationsAllowed=0`.

Discovered during research (2026-06-03) — this key is independent of the Privacy key
and provides another layer of icon suppression.

---

## Approach 3: Registry ACL Hardening

Using `SetSecurityInfo` (C++) or .NET `RegistrySecurity` (PowerShell) to set a custom
DACL on the Privacy and ConsentStore registry keys:
- Deny Everyone `KEY_SET_VALUE`
- Allow current user `KEY_ALL_ACCESS`
- Allow Administrators `KEY_ALL_ACCESS` (for recovery)
- Set `PROTECTED_DACL` to prevent parent key inheritance

This prevents any non-admin process (including Windows Settings app, updates, etc.)
from changing our registry values back.

**Trade-off:** If the user wants to re-enable location notifications through Settings,
they must first run `unharden`.

---

## Approach 4: `TrayNotify\IconStreams` Binary Patching (Experimental)

**Key:** `HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify`
**Values:**
- `IconStreams` — binary blob, serialized list of all known notification icons
- `PastIconsStream` — historical icon data

Each `NOTIFYITEM` entry contains a preference byte: 0=hidden, 1=show, 2=show when active.

**Status:** Experimental. The binary format is undocumented and changes between Windows
builds. Use `--skip-iconstreams` to skip this step.

---

## Approach 5: Windhawk-Style DLL Injection (NOT implemented)

The Windhawk mod "Taskbar tray system icon tweaks" works by:
1. Injecting a DLL into `explorer.exe`
2. Manipulating the XAML visual tree (`SystemTray.OmniButton#ControlCenterButton > Grid > ... > ContentPresenter[N]`)
3. Setting `Visibility=Collapsed` on specific icon elements

**Mod source is NOT open source** — it's only distributed as a compiled `.whmod` file
through Windhawk's in-app catalog. The XAML manipulation approach fundamentally requires
runtime code injection and cannot be replicated without a persistent injection mechanism.

**We do NOT implement this approach.** Approaches 1+2+3 together provide equivalent
functionality without DLL injection or background processes.

---

## Windows 11 Build Notes

- **21H2 (22000):** Classic taskbar, `explorer.exe` manages icons via Win32
- **22H2 (22621)+:** XAML-based taskbar, icons managed via `Windows.UI.Xaml`
- **24H2 (26100):** Continued XAML-based approach
- **Build 25977+:** Added "Notify when apps request location" toggle in Settings

The location icon is a shell-managed icon, not an app icon. In the XAML taskbar, it lives
inside the `SystemTray` XAML island and is controlled by shell logic checking the
`LocationNotificationsAllowed` registry value.

---

## Group Policy Confirmation

Microsoft's `ADMX_Taskbar` CSP includes policies for hiding system tray icons
(`HideSCANetwork`, `HideSCAPower`, `HideSCAVolume`, `HideSCAHealth`), but there is **NO**
dedicated GPO for hiding the location icon. The registry-based approach remains the
only non-injection method.

**Confirmed: 2026-06-03** via Microsoft ADMX_Taskbar documentation.
