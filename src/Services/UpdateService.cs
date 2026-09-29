using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FileOrganizer.Config;

namespace FileOrganizer.Services
{
    /// <summary>
    /// Checks the public GitHub Releases API for a newer version of TimeFold.
    /// Unauthenticated read-only — no credentials or personal data are used.
    /// </summary>
    public static class UpdateService
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders = { { "User-Agent", $"TimeFold/{AppConstants.AppVersion}" } }
        };

        public record UpdateInfo(
            string TagName,
            string Version,
            string ReleasePageUrl,
            string Summary,
            bool HasFullNotes);

        /// <summary>
        /// Result of an update check.
        /// <list type="bullet">
        ///   <item><c>Success=false</c>: could not reach GitHub (offline, timeout, API error)</item>
        ///   <item><c>Success=true, Info=null</c>: reachable, already on latest version</item>
        ///   <item><c>Success=true, Info!=null</c>: update available</item>
        /// </list>
        /// </summary>
        public record UpdateCheckResult(bool Success, UpdateInfo? Info);

        /// <summary>
        /// Checks GitHub for the latest release. Never throws.
        /// </summary>
        public static async Task<UpdateCheckResult> CheckAsync()
        {
            try
            {
                using var response = await _http.GetAsync(AppConstants.ReleasesApiUrl).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new UpdateCheckResult(false, null);

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
                string body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? string.Empty : string.Empty;
                string htmlUrl = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() ?? AppConstants.ReleasesPageUrl : AppConstants.ReleasesPageUrl;

                string remoteVersion = tagName.TrimStart('v', 'V');

                // Successfully reached GitHub but no newer version
                if (!IsNewer(remoteVersion, AppConstants.AppVersion))
                    return new UpdateCheckResult(true, null);

                string summary = ExtractSummary(body);
                bool hasFullNotes = !string.IsNullOrWhiteSpace(body);

                return new UpdateCheckResult(true, new UpdateInfo(tagName, remoteVersion, htmlUrl, summary, hasFullNotes));
            }
            catch
            {
                // Network error, timeout, DNS failure, offline — not an update answer
                return new UpdateCheckResult(false, null);
            }
        }

        /// <summary>
        /// Returns true if remoteVersion is strictly greater than localVersion.
        /// </summary>
        public static bool IsNewer(string remoteVersion, string localVersion)
        {
            if (Version.TryParse(NormalizeVersion(remoteVersion), out var remote) &&
                Version.TryParse(NormalizeVersion(localVersion), out var local))
            {
                return remote > local;
            }
            return false;
        }

        private static string NormalizeVersion(string v)
        {
            var parts = v.Split('.');
            return parts.Length switch
            {
                1 => $"{v}.0.0",
                2 => $"{v}.0",
                _ => v
            };
        }

        private static string ExtractSummary(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return string.Empty;

            int start = body.IndexOf(AppConstants.ReleaseSummaryStart, StringComparison.Ordinal);
            int end = body.IndexOf(AppConstants.ReleaseSummaryEnd, StringComparison.Ordinal);
            if (start >= 0 && end > start)
            {
                string extracted = body.Substring(start + AppConstants.ReleaseSummaryStart.Length, end - start - AppConstants.ReleaseSummaryStart.Length).Trim();
                if (!string.IsNullOrWhiteSpace(extracted)) return extracted;
            }

            // Fallback: first ~300 chars, stripped of HTML comments
            string stripped = Regex.Replace(body, @"<!--.*?-->", string.Empty, RegexOptions.Singleline).Trim();
            if (stripped.Length <= 300) return stripped;
            int cutAt = stripped.LastIndexOf(' ', 300);
            return (cutAt > 0 ? stripped[..cutAt] : stripped[..300]) + "…";
        }
    }
}
