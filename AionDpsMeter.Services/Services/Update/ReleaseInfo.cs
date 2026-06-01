namespace AionDpsMeter.Services.Services.Update
{
    public sealed class ReleaseInfo
    {
        public string TagName { get; init; } = string.Empty;
        public string Name    { get; init; } = string.Empty;
        public string Body    { get; init; } = string.Empty;
        public string HtmlUrl { get; init; } = string.Empty;
        public string ZipUrl  { get; init; } = string.Empty;
    }
}
