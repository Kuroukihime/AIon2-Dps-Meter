namespace AionDpsMeter.UI.ViewModels
{
    /// <summary>
    /// Centralised number and duration formatting used across all ViewModels.
    /// </summary>
    internal static class DamageFormatter
    {
        /// <summary>
        /// Compact abbreviation: 1.23B / 1.23M / 1.23K / raw.
        /// </summary>
        public static string Format(long value)
        {
            if (value >= 1_000_000_000)
                return $"{value / 1_000_000_000.0:F2}B";
            if (value >= 1_000_000)
                return $"{value / 1_000_000.0:F2}M";
            if (value >= 1_000)
                return $"{value / 1_000.0:F2}K";
            return value.ToString();
        }

        /// <summary>
        /// Compact abbreviation accepting a double (truncated to long before formatting).
        /// </summary>
        public static string Format(double value) => Format((long)value);

        /// <summary>
        /// Full numeric representation with thousands separator, e.g. 1,234,567.
        /// </summary>
        public static string FormatFull(long value) => value.ToString("N0");

        /// <summary>
        /// Duration as mm:ss or hh:mm:ss when &gt;= 1 hour.
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        /// <summary>
        /// Rate value as a percentage string with one decimal place, e.g. "42.3%".
        /// </summary>
        public static string FormatRate(double rate) => $"{rate:F1}%";

        /// <summary>
        /// Rate value as a percentage string with zero decimal places, e.g. "42%".
        /// </summary>
        public static string FormatRateRounded(double rate) => $"{rate:F0}%";
    }
}
