using System.Data.Common;
using System.Text;

namespace VoteRightWebApp.Services;

public interface ICsvExportService
{
    Task WriteAsync(DbDataReader reader, TextWriter writer, string[] headers, CancellationToken cancellationToken = default);
}

public class CsvExportService : ICsvExportService
{
    public async Task WriteAsync(DbDataReader reader, TextWriter writer, string[] headers, CancellationToken cancellationToken = default)
    {
        await writer.WriteLineAsync(string.Join(',', headers));
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                values[i] = CsvEscape(reader[i]);
            }
            await writer.WriteLineAsync(string.Join(',', values));
        }
    }

    private static string CsvEscape(object? val)
    {
        if (val == null || val is DBNull) return string.Empty;
        var s = Convert.ToString(val) ?? string.Empty;
        var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        if (s.Contains('"')) s = s.Replace("\"", "\"\"");
        return needsQuotes ? "\"" + s + "\"" : s;
    }
}
