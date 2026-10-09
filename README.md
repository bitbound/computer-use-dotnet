# Bitbound.ComputerUseDotnet

A cross-platform [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server that lets an agent **see the screen** and **simulate input** (mouse + keyboard) on Windows, macOS, and Linux (X11 and Wayland).

## Requirements

- .NET 10 SDK
- Linux/macOS/Windows (SkiaSharp native assets are included per-platform)

## Usage with GitHub Copilot

Add the following to your GitHub Copilot config (`mcp.json`):

```json
{
  "mcpServers": {
    "computer-use-dotnet": {
      "type": "stdio",
      "command": "dnx",
      "args": [
        "-y",
        "Bitbound.ComputerUseDotnet"
      ]
    }
  }
}
```

> **Warning:** This server lets an agent control your real desktop. Review tool calls carefully and run it only with agents you trust.

## Tools

| Tool | Description |
| --- | --- |
| `take_screenshot` | Captures the screen (whole desktop or one display) and returns PNG image content plus coordinate-mapping metadata. Optional `max_side` downscales to save tokens; it never changes a coordinate. |
| `get_desktop_info` | Backend, desktop environment, displays (bounds, scale, fraction range), desktop size, and cursor position. |
| `check_permissions` | Reports screen-capture / input permission state without prompting. |
| `request_permissions` | Asks the OS/portal for missing permissions (prompts the user where supported). |
| `move_mouse` | Warps the pointer to a desktop fraction. |
| `click` | Clicks at a desktop fraction (`button`, `click_count` for double-click). |
| `drag` | Drags with a button held from one desktop fraction to another (interpolated steps). |
| `scroll` | Scrolls wheel clicks at a desktop fraction (positive `vertical` = up). |
| `type_text` | Types literal text (Unicode where the platform supports it). |
| `press_key` | Presses a key chord like `ctrl+alt+delete`, `cmd+space`, or `a`. |
| `get_cursor_position` | Current pointer position as a fraction and a pixel (unavailable on Wayland, where the portal cannot report it). |
| `read_logs` | Returns recent lines from the server's rolling log file (tail, optional substring filter). Use after a tool error to see the full stack trace. |

## Logs

The server writes Information-and-above log lines to a rolling file at:
- Windows: `%LOCALAPPDATA%\Bitbound\ComputerUseDotnet\Logs\computer-use-dotnet.log`
- Linux/macOS: `~/.local/share/Bitbound/ComputerUseDotnet/Logs/computer-use-dotnet.log`

The file rotates at 2 MB and keeps three backups. Tool errors are logged with the full exception (message, type, stack trace, inner exceptions); the tool result includes the message and a pointer to the `read_logs` tool, so a fresh-session agent that sees an error can find the rest without being told the log exists.

## Coordinate model

Input tools take a position as a **fraction of the desktop**, not as pixels. `x` runs `0.0` at the left edge to `1.0` at the right edge and `y` runs `0.0` at the top to `1.0` at the bottom, so `(0.5, 0.5)` is the middle of the screen. The desktop is the union of all display bounds, with the top-left of that union as the origin.

Fractions are what makes this survive real use. A screenshot is routinely resized before a model reasons over it, whether the server downscales it or the vision pipeline does, and a pixel coordinate measured on that smaller image points somewhere else once applied to the real display. A fraction points at the same spot at any image size. A full-desktop `take_screenshot` maps straight onto the grid, so something 40% across the image is `x: 0.4`.

Values from 1 to 1000 are read as thousandths, for models trained on a 0-1000 grid. Anything else out of range returns a message instead of moving the pointer, and that message converts the number you sent into the fraction you meant.

Every input result, plus `get_cursor_position`, echoes the resolved fraction and its pixel, so a wrong target is easy to see and correct. `get_desktop_info` reports the desktop size in pixels and each display's fraction range.

Example: a 1920×1080 display left of a 2560×1440 primary becomes a 4480×1440 desktop. The left display covers `x` 0 to 0.428, the primary covers `x` 0.429 to 1.0.

A `take_screenshot` of a single display is a crop of that desktop, so its fractions are not desktop fractions. The response text states the exact conversion, and the simplest fix is to capture the whole desktop instead.

## Platform support

| Platform | Capture | Input | Permissions |
| --- | --- | --- | --- |
| Windows | GDI `BitBlt` of the virtual screen | `SendInput` | None required |
| macOS | Per-display `CGDisplayCreateImage` | `CGEvent` posting | Screen Recording (capture) + Accessibility (input) |
| Linux (X11) | `XGetImage` on the root window | XTEST extension (`libXtst`) | Enforced by the X server itself (same-user sessions) |
| Linux (Wayland) | XDG Desktop Portal `Screenshot` | XDG Desktop Portal `RemoteDesktop` | Portal prompts the user once |

### macOS setup notes

1. Run `request_permissions` (or trigger a capture/input action) — macOS shows prompts for **Screen Recording** and **Accessibility**.
2. **Relaunch the MCP server after granting**; macOS only applies TCC grants to a process at launch. The granted binary is the one hosting the tool (e.g. `computer-use-dotnet`) — check System Settings › Privacy & Security if prompts never appear.

### Wayland notes

- Input uses `org.freedesktop.portal.RemoteDesktop` (keyboard + pointer, persist mode *restore token*). The token is saved to `~/.config/computer-use-dotnet/wayland-remotedesktop-restore-token` (owner-only `0600`), so later launches skip the prompt. A stale token is discarded automatically and the session is re-established with a fresh prompt.
- Capture uses `org.freedesktop.portal.Screenshot`. This portal has **no** restore-token concept; depending on portal implementation you may be re-prompted for screenshots.
- `get_cursor_position` is unavailable (the RemoteDesktop portal exposes no cursor query).
- The input session auto-initializes on the first input call; screenshots work before the input grant using screenshot dimensions.
- X11/XWayland: a session is treated as Wayland when `WAYLAND_DISPLAY` or `XDG_SESSION_TYPE=wayland` is set, X11 otherwise. Force X11 behavior by unsetting those variables for the server process if you prefer XWayland-based automation.

## Key names

`press_key` accepts chords of `'+'`-separated tokens: modifier aliases (`ctrl`, `alt`, `shift`, `cmd`/`super`/`win`, `opt`, `meta`…) plus one target — a single character or a name such as `enter`, `tab`, `escape`, `up`, `pageup`, `printscreen`, `f5`. Names are case-insensitive with common aliases (`pgup`, `del`, `arrowleft`, …). Characters keep their case, so `press_key("A")` presses shift automatically on platforms without Unicode injection.

## Agent skill

`skills/computer-use-dotnet/SKILL.md` teaches a fresh agent to use these tools: the fraction coordinate model, the see-act loop, platform and permission notes, and the common traps. Load it by copying the `computer-use-dotnet` folder into `.qwen/skills/` (project) or `~/.qwen/skills/` (user), or install it from this repo's GitHub URL. The committed copy sits under `skills/` rather than `.qwen/skills/` so it survives a global `.qwen/` ignore rule.

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
