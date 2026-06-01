using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AionDpsMeter.Services.Services.Update
{
    public sealed class UpdateCheckerService
    {
        private const string ReleasesApiUrl = "https://api.github.com/repos/Kuroukihime/AIon2-Dps-Meter/releases/latest";

        private static readonly HttpClient _httpClient = new()
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", "AionDpsMeter-UpdateChecker" }
            },
            Timeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>Returns the latest release if it is newer than the current assembly version, otherwise null.</summary>
        public async Task<ReleaseInfo?> CheckForUpdateAsync()
        {
            try
            {
                var dto = await _httpClient.GetFromJsonAsync<GithubReleaseDto>(ReleasesApiUrl);
                if (dto is null) return null;

                var latestVersion  = ParseVersion(dto.TagName);
                var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);

                var latestCompare  = new Version(latestVersion.Major,  latestVersion.Minor,  latestVersion.Build);
                var currentCompare = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build);

                if (latestCompare > currentCompare)
                {
                    var zipUrl = dto.Assets?.FirstOrDefault(a =>
                        a.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)?.BrowserDownloadUrl
                        ?? dto.ZipballUrl
                        ?? string.Empty;

                    return new ReleaseInfo
                    {
                        TagName = dto.TagName ?? string.Empty,
                        Name    = dto.Name    ?? dto.TagName ?? string.Empty,
                        Body    = dto.Body    ?? string.Empty,
                        HtmlUrl = dto.HtmlUrl ?? string.Empty,
                        ZipUrl  = zipUrl
                    };
                }
            }
            catch
            {

            }

            return null;
        }

        /// <summary>Downloads the release zip to a temp folder next to the executable and returns its path.</summary>
        public async Task<string> DownloadReleaseAsync(ReleaseInfo release, IProgress<int>? progress = null, CancellationToken ct = default)
        {
            var exeDir  = AppContext.BaseDirectory;
            var zipPath = Path.Combine(exeDir, $"update_{release.TagName}.zip");

            using var response = await _httpClient.GetAsync(release.ZipUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var total   = response.Content.Headers.ContentLength ?? -1L;
            var written = 0L;

            await using var src  = await response.Content.ReadAsStreamAsync(ct);
            await using var dest = File.Create(zipPath);

            var buffer = new byte[81920];
            int read;
            while ((read = await src.ReadAsync(buffer, ct)) > 0)
            {
                await dest.WriteAsync(buffer.AsMemory(0, read), ct);
                written += read;
                if (total > 0)
                    progress?.Report((int)(written * 100 / total));
            }

            progress?.Report(100);
            return zipPath;
        }

        private static Version ParseVersion(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return new Version(0, 0, 0);
            var clean = tag.TrimStart('v', 'V');
            return Version.TryParse(clean, out var v) ? v : new Version(0, 0, 0);
        }

        private sealed class GithubReleaseDto
        {
            [JsonPropertyName("tag_name")]    public string?         TagName    { get; init; }
            [JsonPropertyName("name")]        public string?         Name       { get; init; }
            [JsonPropertyName("body")]        public string?         Body       { get; init; }
            [JsonPropertyName("html_url")]    public string?         HtmlUrl    { get; init; }
            [JsonPropertyName("zipball_url")] public string?         ZipballUrl { get; init; }
            [JsonPropertyName("assets")]      public List<AssetDto>? Assets     { get; init; }
        }

        private sealed class AssetDto
        {
            [JsonPropertyName("name")]                 public string? Name                { get; init; }
            [JsonPropertyName("browser_download_url")] public string? BrowserDownloadUrl { get; init; }
        }
    }
}

