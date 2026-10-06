# Changelog

## [1.11.4.0] - 2026-10-04

### Added
- **Minimize to tray**: the Hide button and the toggle-visibility hotkey send the meter and its overlays to the system tray. Double-click the tray icon or use its menu (Show / Exit) to bring them back.
- **Timers overlay**: a floating, click-through overlay with the local clock and the Abyss Corridor countdown. Drag it while Settings is open. Toggle it under Settings → Overlays.
- **Abyss Corridor timer**: the countdown to the next spawn. The schedule is read from `GameData/Assets/abyss_corridor.json` (US Eastern wall-clock times, DST-aware). An optional tray notification and sound fire a configurable number of minutes before each spawn (Settings → Events).
- **Show only over the game**: the meter and all its windows are visible only while AION2 (or the meter itself) is focused. They hide when you alt-tab away, stay hidden while the game isn't running, and reappear without taking focus from the game. On by default; toggle it in Settings → Appearance. The tray icon's Show still brings everything back.
- **Windows follow the game**: the meter and overlays are placed over the game window wherever it is, and follow it live when it moves to another monitor. Positions are remembered relative to the game window, never as screen coordinates.
- **Hold-to-move**: the meter and overlays are click-through, so clicks go to the game. Hold Ctrl (configurable: Settings → Hotkeys) to use, drag (the meter by its whole header bar) or resize them; a dashed outline and the resize grip show only while it is held. Dropping them at a screen edge no longer snap-resizes them.
- **Keep Me on Top**: an optional setting that pins your row first in the player list and shows your real DPS rank (Settings → Appearance).
- New defaults: the meter is 473×297 at the game's left edge, the timers overlay centered near the top.

### Changed
- The meter runs as administrator (UAC prompt at launch). AION2 runs elevated, and Windows only delivers its keyboard input to hooks at the same privilege level, which hold-to-move needs.

### Fixed
- Overlays could ignore the mouse in edit mode: click-through restored a stale snapshot of WebView2's child windows, leaving its input window disabled.
