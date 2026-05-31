using System.Windows.Input;

namespace AionDpsMeter.UI.Utils
{
    public static class HotkeyParser
    {
        // MOD_NONE=0, MOD_ALT=1, MOD_CTRL=2, MOD_SHIFT=4, MOD_WIN=8
        public const uint MOD_NONE  = 0;
        public const uint MOD_ALT   = 0x0001;
        public const uint MOD_CTRL  = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN   = 0x0008;

        /// <summary>
        /// Parses a hotkey string like "Ctrl+Shift+D" into (modifiers, virtualKey).
        /// Returns (0,0) if parsing fails or no main key found.
        /// </summary>
        public static (uint modifiers, uint vk) Parse(string? hotkey)
        {
            if (string.IsNullOrWhiteSpace(hotkey))
                return (0, 0);

            var parts = hotkey.Split('+');
            uint mods = 0;
            Key mainKey = Key.None;

            foreach (var raw in parts)
            {
                var part = raw.Trim();
                switch (part.ToUpperInvariant())
                {
                    case "CTRL":  mods |= MOD_CTRL;  break;
                    case "SHIFT": mods |= MOD_SHIFT; break;
                    case "ALT":   mods |= MOD_ALT;   break;
                    case "WIN":   mods |= MOD_WIN;   break;
                    default:
                        if (Enum.TryParse<Key>(part, ignoreCase: true, out var k))
                            mainKey = k;
                        break;
                }
            }

            if (mainKey == Key.None)
                return (0, 0);

            var vk = (uint)KeyInterop.VirtualKeyFromKey(mainKey);
            return (mods, vk);
        }
    }
}
