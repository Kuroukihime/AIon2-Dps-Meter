# Changelog

## [Unreleased]

### Added
- **Minimize to tray**: the Hide button and the toggle-visibility hotkey send the meter and its overlays to the system tray. Double-click the tray icon or use its menu (Show / Exit) to bring them back.
- **Timers overlay**: a floating, click-through overlay with the local clock and the Abyss Corridor countdown. Drag it while Settings is open. Toggle it under Settings → Overlays.
- **Abyss Corridor timer**: the countdown to the next spawn. The schedule is read from `GameData/Assets/abyss_corridor.json` (US Eastern wall-clock times, DST-aware). An optional tray notification and sound fire a configurable number of minutes before each spawn (Settings → Events).
- **Show only over the game**: the meter and all its windows are visible only while AION2 (or the meter itself) is focused. They hide when you alt-tab away or when the game isn't running, and reappear without taking focus from the game. On by default; toggle it in Settings → Appearance. The tray icon's Show still brings everything back.
- **Keep Me on Top**: an optional setting that pins your row first in the player list and shows your real DPS rank (Settings → Appearance).
