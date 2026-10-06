# Changelog

## [1.11.4.0] - 2026-10-04

### Added
- **Minimize to tray**: the Hide button and the toggle-visibility hotkey send the meter and its overlays to the system tray. Double-click the tray icon or use its menu (Show / Settings / Exit).
- **Timers overlay**: a floating overlay with the local clock (24 h or 12 h) and the Spacetime Rift countdown, configured in the new Settings → Clock & Timers tab. On by default, with the 24 h clock and the Spacetime Rift timer shown.
- **Spacetime Rift timer**: the countdown to the next spawn. The schedule is read from `GameData/Assets/spacetime_rift.json` (US Eastern wall-clock times, DST-aware). An optional tray notification and sound fire a configurable number of minutes before each spawn (Settings → Clock & Timers).
- **Show only over the game**: the meter and all its windows are visible only while AION2 (or the meter itself) is focused. They hide when you alt-tab away, stay hidden while the game isn't running, and reappear without taking focus from the game. On by default; toggle it in Settings → Appearance. The tray icon's Show still brings everything back.
- **Show Meter Only in Combat**: an optional setting (Settings → Appearance) that hides the meter outside combat. It appears when you or your group deal damage and hides 10 s after the last hit; overlays are unaffected, and holding Ctrl or opening Settings shows it anyway.
- **Windows follow the game**: the meter and overlays are placed over the game window wherever it is, and follow it live when it moves to another monitor. Positions are remembered relative to the game window, never as screen coordinates.
- **Hold-to-move**: the meter and overlays are click-through, so clicks go to the game. Hold Ctrl (configurable: Settings → Hotkeys) to use, drag (the meter by its whole header bar) or resize them; a dashed outline and the resize grip show only while it is held. Dropping them at a screen edge no longer snap-resizes them.
- **Keep Me on Top**: pins your row first in the player list and shows your real DPS rank. On by default; toggle it in Settings → Appearance.
- **Party and force awareness**: the meter reads your party and force rosters from the game. Party members get a 👥 before their name and other force members a ⚔️. While you're in a party or force only your group is listed and the total counts only your group; strangers hitting the same target are still recorded, just not shown. Solo, everyone is listed as before.
- **Item level**: shown above the class icon (combat power stays below) for you and your party and force members, read from the group rosters. Combat power is now also filled in from party rosters. Players outside your group show neither, since the game doesn't send them. Toggle it in Settings → Appearance → Show Item Level (on by default).
- **Solo Mode**: the meter shows only your row and its total counts only your own damage. Switch it with SOLO on the meter bar (colored when on, grayed out when off) or in Settings → Appearance. It turns off automatically while you're in a party or force and comes back after you leave. Other players are never shown in solo; until your row is recognized (your first cooldown skills, or a zone change) the meter shows a short hint instead.
- **My Display Name**: an optional name shown for your character everywhere (meter, details, history), set at the top of Settings → Appearance. Empty keeps the game's name.
- **Earlier self-recognition**: the meter now finds your row from your own skill cooldowns within the first few casts, instead of waiting for a zone change, so the highlight, Keep Me on Top, Solo Mode and your display name work right away (including in forces and the open world).
- **Own row highlight**: your row has a gold outline and name, so you can find yourself at a glance.
- New defaults: the meter is 473×297 at the game's left edge, the timers overlay centered near the top, and player rows use class colors.

### Changed
- The Settings window can be moved by dragging anywhere on its header bar (previously only the title text), and is wider so all tabs fit on one row.
- The meter runs as administrator (UAC prompt at launch). AION2 runs elevated, and Windows only delivers its keyboard input to hooks at the same privilege level, which hold-to-move needs.
- The meter refreshes 10 times per second and re-renders only when a displayed value changes; bar movement is animated in CSS. This keeps its embedded browser from falling behind in large fights.

### Fixed
- Player DPS is now damage over the whole fight's duration, so rows add up to the total. It used each player's own first-to-last hit time, so a single hit showed ten times its damage as DPS and late joiners looked far stronger than they were. The total no longer shows a huge number at the first hit of a fight.
- The app slowed down over long sessions: every mob ever hit kept an entry that was scanned on each damage and buff event. Finished entries are now dropped.
- The meter and the player-details window got slower as a fight went on: each refresh copied and rescanned every hit of every player. Hit statistics are now kept as running counts, the party DPS no longer recomputes all player stats, and the details window refreshes 4 times per second (only when the player has new hits) and reads just the new combat-log entries.
- Overlays could ignore the mouse in edit mode: click-through restored a stale snapshot of WebView2's child windows, leaving its input window disabled.
