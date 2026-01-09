using System.Collections.Generic;
using Npgsql;
using VoteRightWebApp.Utility;
using VoteRightWebApp.Models;

namespace VoteRightWebApp.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PostgresConnection")
                ?? throw new InvalidOperationException("PostgresConnection not configured.");
        }

        public async Task<List<dynamic>> GetAssembliesAsync()
        {
            var list = new List<dynamic>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(SqlQueries.Assemblies.DistinctWithBoothCount, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var number = reader.GetString(1);
                var boothCount = reader.GetInt32(2);
                list.Add(new { Name = name, Number = number, BoothCount = boothCount });
            }
            return list;
        }

        public async Task<List<string>> GetDistinctDistrictsAsync()
        {
            var districts = new List<string>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(SqlQueries.Metadata.DistinctDistricts, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                districts.Add(reader.GetString(0));
            }
            return districts;
        }

        public async Task AddFileDownloadEntryAsync(FileDownloadEntry download)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(SqlQueries.Downloads.Insert, conn);
            cmd.Parameters.AddWithValue("userId", download.UserId);
            cmd.Parameters.AddWithValue("assembly", download.Assembly);
            cmd.Parameters.AddWithValue("booths", (object?)download.Booths ?? DBNull.Value);
            cmd.Parameters.AddWithValue("deviceType", (object?)download.DeviceType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("downloadedAt", download.DownloadedAt);
            var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            download.Id = newId;
        }
    }
}