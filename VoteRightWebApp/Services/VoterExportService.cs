using System.Text;
using Npgsql;
using VoteRightWebApp.Data;

namespace VoteRightWebApp.Services;

public interface IVoterExportService
{
    Task StreamVotersToCsvAsync(string assemblyName, string? boothRange, Stream outputStream, CancellationToken cancellationToken = default);
}

public class VoterExportService : IVoterExportService
{
    private readonly IConfiguration _configuration;
    private readonly ICsvExportService _csvExportService;

    public VoterExportService(IConfiguration configuration, ICsvExportService csvExportService)
    {
        _configuration = configuration;
        _csvExportService = csvExportService;
    }

    public async Task StreamVotersToCsvAsync(string assemblyName, string? boothRange, Stream outputStream, CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("PostgresConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Database connection not configured.");

        int? startPartNo = null;
        int? endPartNo = null;
        var range = (boothRange ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(range))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(range, @"^\d+(?:-\d+)?$"))
            {
                throw new ArgumentException("Invalid booth range. Use '20' or '20-45'.");
            }
            var parts = range.Split('-');
            if (parts.Length == 1)
            {
                if (int.TryParse(parts[0], out var single)) { startPartNo = single; endPartNo = single; }
            }
            else
            {
                if (int.TryParse(parts[0], out var s) && int.TryParse(parts[1], out var e))
                {
                    if (e < s) throw new ArgumentException("Invalid range: end must be >= start.");
                    startPartNo = s; endPartNo = e;
                }
            }
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = new StringBuilder(SqlQueries.Voters.SelectByAssemblyBase);
        if (startPartNo.HasValue && endPartNo.HasValue)
        {
            sql.Append(SqlQueries.Voters.RangeClause);
        }
        sql.Append(SqlQueries.Voters.OrderByPartNoSerialNo);
        await using var cmd = new NpgsqlCommand(sql.ToString(), conn);
        cmd.Parameters.AddWithValue("assembly", "%" + assemblyName + "%");
        if (startPartNo.HasValue && endPartNo.HasValue)
        {
            cmd.Parameters.AddWithValue("start", startPartNo.Value);
            cmd.Parameters.AddWithValue("end", endPartNo.Value);
        }

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        await using var writer = new StreamWriter(outputStream, new UTF8Encoding(true), bufferSize: 64 * 1024, leaveOpen: true);
        var headers = new [] { "document_id","serial_no","epic_no","name","relation_type","father_name","mother_name","husband_name","other_name","house_no","age","gender","street_names_and_numbers","part_no","assembly","epic_valid","deleted" };
        await _csvExportService.WriteAsync(reader, writer, headers, cancellationToken);
        await writer.FlushAsync();
    }
}
