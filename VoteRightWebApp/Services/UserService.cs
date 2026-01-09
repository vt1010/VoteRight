using Npgsql;
using VoteRightWebApp.Models;
using VoteRightWebApp.Utility;

namespace VoteRightWebApp.Services
{
    public class UserService : IUserService
    {
        private readonly string _connectionString;

        public UserService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PostgresConnection")
                ?? throw new InvalidOperationException("PostgresConnection not configured.");
        }

        public Task<User?> FindUserAsync(int phoneNumber)
        {
            return FindUserAsyncCore(phoneNumber);
        }

        public Task AddUserAsync(User user)
        {
            return AddUserAsyncCore(user);
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

        private async Task<User?> FindUserAsyncCore(int phoneNumber)
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

        private async Task AddUserAsyncCore(User user)
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
    }
}
