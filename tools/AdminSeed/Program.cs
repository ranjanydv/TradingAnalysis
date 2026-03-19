using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;
using Npgsql;

var connectionString = "Host=localhost;Port=5430;Database=trading;Username=postgres;Password=postgres;SSL Mode=Disable";
var passwordHash = HashPassword("AdminPassw0rd!");
var now = DateTime.UtcNow;
var seededAdminId = Guid.NewGuid();

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();
await using var tx = await conn.BeginTransactionAsync();

var roleId = await UpsertRoleAsync(conn, tx, now);
seededAdminId = await UpsertAdminAsync(conn, tx, roleId, seededAdminId, now);
await UpsertAccountAsync(conn, tx, seededAdminId, passwordHash, now);

await tx.CommitAsync();

Console.WriteLine($"SEEDED_ADMIN_ID={seededAdminId}");
Console.WriteLine($"SEEDED_ROLE_ID={roleId}");
Console.WriteLine("SEEDED_EMAIL=admin@example.com");
Console.WriteLine("SEEDED_PASSWORD=AdminPassw0rd!");

static string HashPassword(string plaintext)
{
    var config = new Argon2Config
    {
        Type = Argon2Type.HybridAddressing,
        TimeCost = 3,
        MemoryCost = 65536,
        Lanes = 2,
        Threads = 2,
        HashLength = 32,
        Password = Encoding.UTF8.GetBytes(plaintext),
        Salt = RandomNumberGenerator.GetBytes(16),
    };

    using var argon2 = new Argon2(config);
    using var hash = argon2.Hash();
    return config.EncodeString(hash.Buffer);
}

static async Task<int> UpsertRoleAsync(NpgsqlConnection conn, NpgsqlTransaction tx, DateTime now)
{
    await using var cmd = new NpgsqlCommand(
        """
        insert into role (name, description, is_system_role, created_at, updated_at)
        values (@name, @description, @is_system_role, @created_at, @updated_at)
        on conflict (name) do update set updated_at = excluded.updated_at
        returning id;
        """,
        conn,
        tx);

    cmd.Parameters.AddWithValue("name", "admin");
    cmd.Parameters.AddWithValue("description", "Seed admin role");
    cmd.Parameters.AddWithValue("is_system_role", true);
    cmd.Parameters.AddWithValue("created_at", now);
    cmd.Parameters.AddWithValue("updated_at", now);
    return (int)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Role seed failed."));
}

static async Task<Guid> UpsertAdminAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int roleId, Guid newAdminId, DateTime now)
{
    await using var selectCmd = new NpgsqlCommand(
        """
        select "Id"
        from admin_users
        where "Email" = @email
        limit 1;
        """,
        conn,
        tx);
    selectCmd.Parameters.AddWithValue("email", "admin@example.com");
    var existingAdminId = await selectCmd.ExecuteScalarAsync();

    if (existingAdminId is Guid adminId)
    {
        await using var updateCmd = new NpgsqlCommand(
            """
            update admin_users
            set "Name" = @name,
                "EmailVerified" = @email_verified,
                "RoleId" = @role_id,
                "Banned" = @banned,
                "BanReason" = @ban_reason,
                "UpdatedAt" = @updated_at
            where "Id" = @id;
            """,
            conn,
            tx);
        updateCmd.Parameters.AddWithValue("id", adminId);
        updateCmd.Parameters.AddWithValue("name", "Local Admin");
        updateCmd.Parameters.AddWithValue("email_verified", true);
        updateCmd.Parameters.AddWithValue("role_id", roleId);
        updateCmd.Parameters.AddWithValue("banned", false);
        updateCmd.Parameters.AddWithValue("ban_reason", DBNull.Value);
        updateCmd.Parameters.AddWithValue("updated_at", now);
        await updateCmd.ExecuteNonQueryAsync();
        return adminId;
    }

    await using var insertCmd = new NpgsqlCommand(
        """
        insert into admin_users ("Id", "Name", "Email", "EmailVerified", "Phone", "PhoneVerified", "Image", "RoleId", "Banned", "BanReason", "CreatedAt", "UpdatedAt")
        values (@id, @name, @email, @email_verified, @phone, @phone_verified, @image, @role_id, @banned, @ban_reason, @created_at, @updated_at)
        returning "Id";
        """,
        conn,
        tx);

    insertCmd.Parameters.AddWithValue("id", newAdminId);
    insertCmd.Parameters.AddWithValue("name", "Local Admin");
    insertCmd.Parameters.AddWithValue("email", "admin@example.com");
    insertCmd.Parameters.AddWithValue("email_verified", true);
    insertCmd.Parameters.AddWithValue("phone", DBNull.Value);
    insertCmd.Parameters.AddWithValue("phone_verified", false);
    insertCmd.Parameters.AddWithValue("image", DBNull.Value);
    insertCmd.Parameters.AddWithValue("role_id", roleId);
    insertCmd.Parameters.AddWithValue("banned", false);
    insertCmd.Parameters.AddWithValue("ban_reason", DBNull.Value);
    insertCmd.Parameters.AddWithValue("created_at", now);
    insertCmd.Parameters.AddWithValue("updated_at", now);

    return (Guid)(await insertCmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Admin seed failed."));
}

static async Task UpsertAccountAsync(NpgsqlConnection conn, NpgsqlTransaction tx, Guid adminId, string passwordHash, DateTime now)
{
    await using var cmd = new NpgsqlCommand(
        """
        insert into account ("Id", "AccountId", "ProviderId", "ActorType", "CustomerId", "AdminId", "AccessToken", "RefreshToken", "IdToken", "AccessTokenExpiresAt", "RefreshTokenExpiresAt", "Scope", "Password", "CreatedAt", "UpdatedAt")
        values (@id, @account_id, @provider_id, @actor_type, @customer_id, @admin_id, @access_token, @refresh_token, @id_token, @access_token_expires_at, @refresh_token_expires_at, @scope, @password, @created_at, @updated_at)
        on conflict ("ProviderId", "AccountId", "ActorType") do update
        set "AdminId" = excluded."AdminId", "Password" = excluded."Password", "UpdatedAt" = excluded."UpdatedAt";
        """,
        conn,
        tx);

    cmd.Parameters.AddWithValue("id", Guid.NewGuid());
    cmd.Parameters.AddWithValue("account_id", "admin@example.com");
    cmd.Parameters.AddWithValue("provider_id", "credential");
    cmd.Parameters.AddWithValue("actor_type", "admin");
    cmd.Parameters.AddWithValue("customer_id", DBNull.Value);
    cmd.Parameters.AddWithValue("admin_id", adminId);
    cmd.Parameters.AddWithValue("access_token", DBNull.Value);
    cmd.Parameters.AddWithValue("refresh_token", DBNull.Value);
    cmd.Parameters.AddWithValue("id_token", DBNull.Value);
    cmd.Parameters.AddWithValue("access_token_expires_at", DBNull.Value);
    cmd.Parameters.AddWithValue("refresh_token_expires_at", DBNull.Value);
    cmd.Parameters.AddWithValue("scope", DBNull.Value);
    cmd.Parameters.AddWithValue("password", passwordHash);
    cmd.Parameters.AddWithValue("created_at", now);
    cmd.Parameters.AddWithValue("updated_at", now);
    await cmd.ExecuteNonQueryAsync();
}
