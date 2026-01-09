using System;
using System.Text;

namespace VoteRightWebApp.Utility
{
    public static class DownloadHelper
    {
        public static string EnsureCsvExtension(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "download.csv";
            return name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? name : name + ".csv";
        }

        public static string SanitizeFileName(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name?.Length ?? 0);
            foreach (var ch in name ?? string.Empty)
            {
                sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
            }
            var result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? "download" : result;
        }

        public static string ToAsciiFallback(string input)
        {
            var sb = new StringBuilder(input?.Length ?? 0);
            foreach (var ch in input ?? string.Empty)
            {
                sb.Append(ch <= 0x7F ? ch : '_');
            }
            var result = sb.ToString();
            return string.IsNullOrWhiteSpace(result) ? "download.csv" : result;
        }

        public static string MapDeviceType(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return "Unknown";
            var ua = userAgent.ToLowerInvariant();
            if (ua.Contains("android")) return "Android";
            if (ua.Contains("iphone")) return "iPhone";
            if (ua.Contains("ipad")) return "iPad";
            if (ua.Contains("windows")) return "Windows";
            if (ua.Contains("mac os x") || ua.Contains("macintosh")) return "macOS";
            if (ua.Contains("linux")) return "Linux";
            return "Unknown";
        }
    }
}
