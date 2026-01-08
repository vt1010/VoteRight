using System.Collections.Generic;
using System.Linq;
using Npgsql;
using VoteRightWebApp.Models;
using VoteRightWebApp.Data;

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

        public async Task<User?> FindUserAsync(int phoneNumber)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(SqlQueries.Users.FindByPhone, conn);
            cmd.Parameters.AddWithValue("phone", phoneNumber);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    PhoneNumber = reader.GetInt32(2),
                    District = reader.GetString(3)
                };
            }
            return null;
        }

        public async Task AddUserAsync(User user)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(SqlQueries.Users.Insert, conn);
            cmd.Parameters.AddWithValue("name", user.Name);
            cmd.Parameters.AddWithValue("phone", user.PhoneNumber);
            cmd.Parameters.AddWithValue("wa", (object?)user.WhatsAppNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("district", user.District);
            cmd.Parameters.AddWithValue("org", user.PoliticalPartyOrganization);
            cmd.Parameters.AddWithValue("pos", (object?)user.OrganizationalPosition ?? DBNull.Value);
            cmd.Parameters.AddWithValue("reg", user.RegisteredAt);
            var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            user.Id = newId;
        }

        public List<User> GetUsers(string district, string assembly)
        {
            var users = new List<User>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = string.IsNullOrEmpty(assembly)
                ? SqlQueries.Users.SelectByDistrict
                : SqlQueries.Users.SelectByDistrictAndAssemblyJoinDownloads;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("district", district);
            if (!string.IsNullOrEmpty(assembly))
                cmd.Parameters.AddWithValue("assembly", assembly);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    PhoneNumber = reader.GetInt16(2),
                    WhatsAppNumber = reader.GetInt16(3),
                    District = reader.GetString(4),
                    PoliticalPartyOrganization = reader.GetString(5),
                    OrganizationalPosition = reader.IsDBNull(6) ? null : reader.GetString(6),
                    RegisteredAt = reader.GetDateTime(7)
                });
            }
            return users;
        }

        public async Task AddDownloadAsync(Download download)
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