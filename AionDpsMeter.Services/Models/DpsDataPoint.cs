namespace AionDpsMeter.Services.Models
{
    public record DpsDataPoint(
        double SecondOffset,
        long PerSecondDamage,
        double CumulativeDps);
}
