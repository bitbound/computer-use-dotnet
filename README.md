# Bitbound.ComputerUseDotnet

A cross-platform [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server that lets an agent **see the screen** and **simulate input** (mouse + keyboard) on Windows, macOS, and Linux (X11 and Wayland).

Install as a .NET global tool and point any MCP client at the `computer-use-mcp` command.

```json
{
  "mcpServers": {
    "computer-use": {
      "command": "computer-use-mcp"
    }
  }
}
```

```powershell
dotnet tool install --global Bitbound.ComputerUseDotnet
```

> **Warning:** This server lets an agent control your real desktop. Review tool calls carefully and run it only with agents you trust.

## Tools

| Tool | Description |
| --- | --- |
| `take_screenshot` | Captures the screen (whole virtual screen or one display) and returns PNG image content plus coordinate-mapping metadata. Optional `max_side` downscales to save tokens. |
| `get_desktop_info` | Backend, desktop environment, displays (bounds, scale, primary), and virtual-screen bounds. |
| `check_permissions` | Reports screen-capture / input permission state without prompting. |
| `request_permissions` | Asks the OS/portal for missing permissions (prompts the user where supported). |
| `move_mouse` | Warps the pointer to a coordinate. |
| `click` | Clicks at a coordinate (`button`, `click_count` for double-click). |
| `drag` | Drags with a button held from one point to another (interpolated steps). |
| `scroll` | Scrolls wheel clicks at a coordinate (positive `vertical` = up). |
| `type_text` | Types literal text (Unicode where the platform supports it). |
| `press_key` | Presses a key chord like `ctrl+alt+delete`, `cmd+space`, or `a`. |
| `get_cursor_position` | Current pointer position (null on Wayland, where the portal cannot report it). |

## Coordinate model

All coordinates are **logical pixels of the virtual screen**: the union of all display bounds with the origin moved to `(0, 0)` of that union. A full-screen `take_screenshot` returns an image whose pixels map 1:1 to those coordinates (Retina/HiDPI captures are downscaled to logical size; the response text states the exact mapping, including any downscale factor when `max_side` is used).

Example: a 1920×1080 display left of a 2560×1440 primary becomes a 4480×1440 virtual screen; `(0, 0)` is the left display's top-left corner.

## Platform support

| Platform | Capture | Input | Permissions |
| --- | --- | --- | --- |
| Windows | GDI `BitBlt` of the virtual screen | `SendInput` | None required |
| macOS | Per-display `CGDisplayCreateImage` | `CGEvent` posting | Screen Recording (capture) + Accessibility (input) |
| Linux (X11) | `XGetImage` on the root window | XTEST extension (`libXtst`) | Enforced by the X server itself (same-user sessions) |
| Linux (Wayland) | XDG Desktop Portal `Screenshot` | XDG Desktop Portal `RemoteDesktop` | Portal prompts the user once |

### macOS setup notes

1. Run `request_permissions` (or trigger a capture/input action) — macOS shows prompts for **Screen Recording** and **Accessibility**.
2. **Relaunch the MCP server after granting**; macOS only applies TCC grants to a process at launch. The granted binary is the one hosting the tool (e.g. `computer-use-mcp`) — check System Settings › Privacy & Security if prompts never appear.

### Wayland notes

- Input uses `org.freedesktop.portal.RemoteDesktop` (keyboard + pointer, persist mode *restore token*). The token is saved to `~/.config/computer-use-dotnet/wayland-remotedesktop-restore-token` (owner-only `0600`), so later launches skip the prompt. A stale token is discarded automatically and the session is re-established with a fresh prompt.
- Capture uses `org.freedesktop.portal.Screenshot`. This portal has **no** restore-token concept; depending on portal implementation you may be re-prompted for screenshots.
- `get_cursor_position` is unavailable (the RemoteDesktop portal exposes no cursor query).
- The input session auto-initializes on the first input call; screenshots work before the input grant using screenshot dimensions.
- X11/XWayland: a session is treated as Wayland when `WAYLAND_DISPLAY` or `XDG_SESSION_TYPE=wayland` is set, X11 otherwise. Force X11 behavior by unsetting those variables for the server process if you prefer XWayland-based automation.

## Key names

`press_key` accepts chords of `'+'`-separated tokens: modifier aliases (`ctrl`, `alt`, `shift`, `cmd`/`super`/`win`, `opt`, `meta`…) plus one target — a single character or a name such as `enter`, `tab`, `escape`, `up`, `pageup`, `printscreen`, `f5`. Names are case-insensitive with common aliases (`pgup`, `del`, `arrowleft`, …). Characters keep their case, so `press_key("A")` presses shift automatically on platforms without Unicode injection.

## Development

```powershell
dotnet build ComputerUseMcp.slnx
dotnet run --project Bitbound.ComputerUseDotnet.Tests
```

`publish-local.ps1` packs the tool into `.\artifacts` and prints an install command with a local source. Publishing to NuGet is a **manual** action: run the *Publish NuGet Package* workflow via **workflow_dispatch** (the `prerelease` input selects `-dev` vs. release packaging). CI on push/PR only builds and tests.

## Repository layout

```
Bitbound.ComputerUseDotnet/
  ComputerUse/            backend abstraction, MCP tools, key tables
    Windows/              GDI + SendInput
    Mac/                  CoreGraphics + CGEvent
    Linux/                XGetImage + XTEST
    Portal/               XDG portal screenshot + RemoteDesktop + restore tokens
```
