namespace AionDpsMeter.UI.StatEfficiency
{
    public static class PercentMath
    {
        public static double ToRate(double percent) => percent / 100.0;

        public static double ClampNonNegative(double value) => Math.Max(0, value);

        public static double Clamp01Percent(double percent) => Math.Clamp(percent, 0, 100);

        public static double ApplyPercentMultiplier(double value, double percent)
            => value * (1 + ToRate(percent));
    }
}
