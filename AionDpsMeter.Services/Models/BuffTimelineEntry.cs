namespace AionDpsMeter.Services.Models
{
    
    public record BuffTimelineEntry(
        int BuffId,
        string BuffName,
        string? BuffIcon,
        double StartSec,
        double EndSec);
}
