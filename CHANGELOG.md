# Changelog

## [1.11.4.0] - 2026-10-04

### Added
- **Minimize to tray**: the Hide button and the toggle-visibility hotkey send the meter and its overlays to the system tray. Double-click the tray icon or use its menu (Show / Settings / Exit).
- **Show only over the game**: the meter and all its windows are visible only while AION2 (or the meter itself) is focused. They hide when you alt-tab away, stay hidden while the game isn't running, and reappear without taking focus from the game. On by default; toggle it in Settings → Appearance. The tray icon's Show still brings everything back.
- **Windows follow the game**: the meter and overlays are placed over the game window wherever it is, and follow it live when it moves to another monitor. Positions are remembered relative to the game window, never as screen coordinates.
- **Hold-to-move**: the meter and overlays are click-through, so clicks go to the game. Hold Ctrl (configurable: Settings → Hotkeys) to use, drag (the meter by its whole header bar) or resize them; a dashed outline and the resize grip show only while it is held. Dropping them at a screen edge no longer snap-resizes them.
- **Show Meter Only in Combat**: an optional setting (Settings → Appearance) that hides the meter outside combat. It appears when you or your group deal damage and hides 10 s after the last hit; overlays are unaffected, and holding Ctrl or opening Settings shows it anyway.
- **Keep Me on Top**: pins your row first in the player list and shows your real DPS rank. On by default; toggle it in Settings → Appearance.
- **Own row highlight**: your row has a gold outline and name, so you can find yourself at a glance.
- **Rank medals**: the top three damage dealers get gold, silver and bronze rank badges; the rest keep the plain one.
- **Party and force awareness**: the meter reads your party and force rosters from the game. Party members get a 👥 before their name and other force members a ⚔️. While you're in a party or force only your group is listed and the total counts only your group; strangers hitting the same target are still recorded, just not shown. Solo, everyone is listed as before.
- **Item level**: shown above the class icon (combat power stays below) for you and your party and force members, read from the group rosters. Combat power is now also filled in from party rosters. Players outside your group show neither, since the game doesn't send them. Toggle it in Settings → Appearance → Show Item Level (on by default).
- **Earlier self-recognition**: the meter now finds your row from your own skill cooldowns within the first few casts, instead of waiting for a zone change, so the highlight, Keep Me on Top, Solo Mode and your display name work right away (including in forces and the open world).
- **My Display Name**: an optional name shown for your character everywhere (meter, details, history), set at the top of Settings → Appearance. Empty keeps the game's name.
- **Solo Mode**: the meter shows only your row and its total counts only your own damage. Switch it with SOLO on the meter bar (colored when on, grayed out when off) or in Settings → Appearance. It turns off automatically while you're in a party or force and comes back after you leave. Other players are never shown in solo; until your row is recognized (your first cooldown skills, or a zone change) the meter shows a short hint instead.
- **Timers overlay**: a floating overlay with the local clock (24 h or 12 h) and the Spacetime Rift countdown, configured in the new Settings → Clock & Timers tab. On by default, with the 24 h clock and the Spacetime Rift timer shown.
- **Spacetime Rift timer**: the countdown to the next spawn. The schedule is read from `GameData/Assets/spacetime_rift.json` (US Eastern wall-clock times, DST-aware). An optional tray notification and sound fire a configurable number of minutes before each spawn (Settings → Clock & Timers).
- New defaults: the meter is 473×297 at the game's left edge, the timers overlay centered near the top, and player rows use class colors.
- **Spacetime Rift entry and stay timers**: when the portals spawn, the countdown switches to the time left to enter ("Rift open", 10 minutes); after you go through a portal to the other faction's server, it shows the time left inside ("In rift", 60 minutes) until the time runs out or you are ported back; a rift can be entered once per opening, so after that it counts down to the next spawn. The game sends neither duration, so both are set in `spacetime_rift.json`. The countdown turns amber, then red, as time runs out, and blinks in its last minute.

### Changed
- The meter refreshes 10 times per second and re-renders only when a displayed value changes; bar movement is animated in CSS. This keeps its embedded browser from falling behind in large fights.
- A boss fight is marked completed and saved as soon as the boss dies, and fights left idle are completed when the history is opened, so they no longer stay "Active" after a dungeon's last boss.
- Settings, combat history, logs, the icon cache and packet logs are stored in `%LocalAppData%\Aion2DPSMeter` instead of the program folder, so they survive updates and deleting the folder. On first start, data from earlier versions is copied there; nothing is overwritten or removed.
- The Settings window can be moved by dragging anywhere on its header bar (previously only the title text), and is wider so all tabs fit on one row.
- The meter runs as administrator (UAC prompt at launch). AION2 runs elevated, and Windows only delivers its keyboard input to hooks at the same privilege level, which hold-to-move needs.
- Only one meter runs at a time: starting it again brings the running meter back (like Show in the tray menu) instead of opening a second copy.
- Meter windows no longer appear in Alt+Tab or on the taskbar; the meter is reached through its own windows and the tray icon.
- Your row is no longer lost 10 minutes into an instance when the meter recognized you from your cooldowns (meter started after you entered): the cleanup of unknown players skips you.
- Combat history keeps one encounter window and one player-details window: clicking another encounter or player loads it into the open window. The meter's own player-details window works the same way.
- Esc in any meter window closes the most recently opened window (Settings, history, details, and so on), one per press; the meter and its overlays stay open.
- Skill and buff icons ship with the app (383 icons, refreshed with `tools/SkillIconPacker`), so the details windows, overlays and Settings pick-lists show them at once instead of downloading on first use.

### Fixed
- The app slowed down over long sessions: every mob ever hit kept an entry that was scanned on each damage and buff event. Finished entries are now dropped.
- The meter and the player-details window got slower as a fight went on: each refresh copied and rescanned every hit of every player. Hit statistics are now kept as running counts, the party DPS no longer recomputes all player stats, and the details window refreshes 4 times per second (only when the player has new hits) and reads just the new combat-log entries.
- Packet intake and the current-target lookup cost less per packet and per hit, most noticeably with many mobs fought nearby.
- Hits could go missing after a network hiccup: the meter lost its place in the game's data stream and kept discarding new data until it happened to realign.
- Player DPS is now damage over the whole fight's duration, so rows add up to the total. It used each player's own first-to-last hit time, so a single hit showed ten times its damage as DPS and late joiners looked far stronger than they were. The total no longer shows a huge number at the first hit of a fight.
- A fight could be missing from the combat history: a theostone hit from a player whose class wasn't known yet made saving it fail.
- A summoner's pet damage from before the meter knew its owner stayed on a separate row instead of the owner's.
- The meter could stay locked onto another program's encrypted traffic after the game was closed, instead of waiting for the game.
- Skill and buff icons could stay blank in the details window: an icon not yet downloaded never appeared while the window was open.
- Closing the skill cooldown overlay stopped other windows from reacting to edit mode.
- Overlays could ignore the mouse in edit mode: click-through restored a stale snapshot of WebView2's child windows, leaving its input window disabled.
