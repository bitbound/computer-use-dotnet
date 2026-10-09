---
name: computer-use-dotnet
description: 'Drive a real desktop (Windows, macOS, Linux X11 and Wayland) through the computer-use-dotnet MCP server. See the screen and simulate mouse and keyboard input. USE FOR: clicking, typing, scrolling, dragging, pressing key chords, taking screenshots, reading desktop layout and permissions, automating a GUI app or OS dialog that has no API. DO NOT USE FOR: headless browser automation (use a browser tool or CDP), HTTP or service calls, or any task that already has a purpose-built connector, CLI, or API.'
argument-hint: 'The GUI task to perform, or a question about these tools'
user-invocable: true
---

# computer-use-dotnet

This MCP server gives you eyes and hands on a real desktop. Every input tool takes a position as a fraction of the desktop, never a pixel. Understand the coordinate model before your first click, because it is the one thing most likely to send a click to the wrong place.

## When to use

- A task needs a GUI that has no API or CLI, such as a settings dialog, an installer, a native file picker, or a desktop app.
- You need to see the screen, or verify that an action had the visible effect you expected.
- The host is Windows, macOS, Linux X11, or Linux Wayland.

Prefer a purpose-built tool when one exists. Reach for this only when the app exposes a UI and nothing else.

## The coordinate model (read this first)

`move_mouse`, `click`, `drag`, and `scroll` place the pointer with fractions of the whole desktop.

- `x` runs `0.0` at the left edge to `1.0` at the right edge. `y` runs `0.0` at the top to `1.0` at the bottom.
- `(0.5, 0.5)` is the center of the combined displays.
- The desktop is the union of every display, and its origin is the top-left of that union.

Two rules decide what a number means.

- A value from `1` to `1000` is read as thousandths, for models that think on a 0-1000 grid. Sending `x = 500` means `0.5`, not pixel 500.
- Anything else out of range comes back as a message instead of moving the pointer. The message converts the number you sent into the fraction you meant, so read it.

To convert a point you measured in a screenshot, divide: `x = pixel_x / image_width`, `y = pixel_y / image_height`. Do not send a raw pixel number, because a value of 1000 or less is read as thousandths and a larger one is rejected outright.

Every input result, and `get_cursor_position`, echoes the resolved fraction and its pixel. Read that echo. A wrong target shows up there before it costs you another round trip.

## Tools

| Tool | Inputs | What it does |
| --- | --- | --- |
| `get_desktop_info` | none | Backend, environment type, desktop size and origin, cursor, per-display bounds, scale and fraction ranges, and permission state. Call this first. |
| `take_screenshot` | `display=-1`, `max_side=0` | Captures the desktop (or one display) and returns a metadata text block plus a PNG. |
| `check_permissions` | none | Reports capture and input permission state without prompting. |
| `request_permissions` | none | Triggers the OS or portal prompts for missing permissions. |
| `move_mouse` | `x`, `y` | Moves the pointer without clicking. |
| `click` | `x`, `y`, `button=left`, `click_count=1` | Clicks at a fraction. `click_count=2` is a double-click. |
| `drag` | `start_x`, `start_y`, `end_x`, `end_y`, `button=left`, `steps=10` | Presses, moves in steps, releases. For selections, sliders, and window moves. |
| `scroll` | `x`, `y`, `scroll_y=0`, `scroll_x=0` | Scrolls wheel clicks. Positive `scroll_y` is up, positive `scroll_x` is right. |
| `type_text` | `text` | Types literal text at the current keyboard focus. Newlines become Enter presses. |
| `press_key` | `keys` | Presses a key or chord, such as `enter`, `f5`, `ctrl+c`, `cmd+space`, `ctrl+shift+t`. |
| `get_cursor_position` | none | Current pointer position as a fraction and a pixel. Unsupported on Wayland. |
| `read_logs` | `lines=100`, `contains=null` | Tails the server log, where the full exception detail lives. |

`button` accepts `left`, `right`, `middle`, `extra`, or `side`. `click_count` is clamped to 1 through 5 and `steps` to 1 through 100.

## The see-act loop

1. Call `get_desktop_info` once at the start. It tells you the desktop size, the per-display fraction ranges, and whether capture and input are permitted.
2. Call `take_screenshot` for the whole desktop. Pass `max_side` (for example `1280`) to downscale and save tokens. Downscaling never changes a coordinate.
3. Find the target in the image and convert its position to a fraction of the image.
4. Act: `click`, `type_text`, `press_key`, or `drag`.
5. Take another screenshot to confirm the result before the next step.

Two habits keep this reliable.

- Click a field before typing into it. `type_text` goes to whatever holds keyboard focus.
- Capture again after any action that redraws the screen. The UI moves, and a stale screenshot will point you at the old location. If a step appears to do nothing, an immediate screenshot catching a half-drawn frame is the usual cause, so capture again.

## Platforms and permissions

- Windows uses GDI capture and `SendInput`. No permissions are required.
- macOS needs Screen Recording for capture and Accessibility for input. Run `request_permissions`, then relaunch the server, because macOS only applies the grant to a process at launch.
- Linux X11 is enforced by the X server in a same-user session. No prompts.
- Linux Wayland uses the XDG portal. Screenshots work before the input grant, the input session starts on the first input call, and the portal prompts the user once per session unless a restore token is stored.

## Traps

- Pixels are not fractions. Convert a screenshot pixel to a fraction before you send it (see the coordinate model above).
- A single-display capture is a crop of the desktop, so its fractions are not desktop fractions. The response states the exact conversion, and the simplest fix is to capture the whole desktop with `display=-1`.
- `max_side` only resizes the returned image. It never moves a coordinate, so it is always safe to use.
- Use `type_text` for content and `press_key` for shortcuts and special keys. A newline in `type_text` presses Enter.

## When a tool errors

A tool error reads like `click failed: <message> (see read_logs for the full stack trace)`. Call `read_logs` and narrow it with `contains`, using the tool name or the exception type.

The same log also sits at `%LOCALAPPDATA%/Bitbound/ComputerUseDotnet/Logs/computer-use-dotnet.log` (Windows) or `~/.local/share/Bitbound/ComputerUseDotnet/Logs/computer-use-dotnet.log` (Linux and macOS).
