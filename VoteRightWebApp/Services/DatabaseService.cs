using System.Collections.Generic;
using System.Linq;
using Npgsql;
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

        public async Task<List<string>> GetDistinctDistrictsAsync()
        {
            var districts = new List<string>();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"SELECT DISTINCT district
                                                    FROM public.metadata
                                                    WHERE district IS NOT NULL AND district <> ''
                                                    ORDER BY district", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                districts.Add(reader.GetString(0));
            }
            return districts;
        }

        public async Task<User?> FindUserAsync(string phoneNumber)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"SELECT id, name, phoneNumber, whatsAppNumber, district, politicalPartyOrganization, organizationalPosition, registeredAt
                                                     FROM Users WHERE phoneNumber = @phone LIMIT 1", conn);
            cmd.Parameters.AddWithValue("phone", phoneNumber);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    PhoneNumber = reader.GetString(2),
                    WhatsAppNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                    District = reader.GetString(4),
                    PoliticalPartyOrganization = reader.GetString(5),
                    OrganizationalPosition = reader.IsDBNull(6) ? null : reader.GetString(6),
                    RegisteredAt = reader.GetDateTime(7)
                };
            }
            return null;
        }

        public async Task AddUserAsync(User user)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"INSERT INTO Users (name, phoneNumber, whatsAppNumber, district, politicalPartyOrganization, organizationalPosition, registeredAt)
                                                     VALUES (@name, @phone, @wa, @district, @org, @pos, @reg)
                                                     RETURNING id", conn);
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
                ? @"SELECT DISTINCT id, name, phoneNumber, whatsAppNumber, district, politicalPartyOrganization, organizationalPosition, registeredAt
                    FROM Users WHERE district = @district"
                : @"SELECT DISTINCT u.id, u.name, u.phoneNumber, u.whatsAppNumber, u.district, u.politicalPartyOrganization, u.organizationalPosition, u.registeredAt
                    FROM Users u INNER JOIN Downloads d ON u.id = d.userId
                    WHERE u.district = @district AND d.assembly = @assembly";

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
                    PhoneNumber = reader.GetString(2),
                    WhatsAppNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
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
            await using var cmd = new NpgsqlCommand(@"INSERT INTO Downloads (userId, assembly, booths, deviceType, downloadedAt)
                                                     VALUES (@userId, @assembly, @booths, @deviceType, @downloadedAt)
                                                     RETURNING id", conn);
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