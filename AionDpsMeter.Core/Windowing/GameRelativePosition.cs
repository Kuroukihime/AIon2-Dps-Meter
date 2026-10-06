namespace AionDpsMeter.Core.Windowing
{
    /// <summary>
    /// A window's top-left corner as a fraction of the game window's size, so it maps onto the game wherever it runs.
    /// </summary>
    public sealed class GameRelativePosition
    {
        public double X { get; init; }
        public double Y { get; init; }
    }
}
