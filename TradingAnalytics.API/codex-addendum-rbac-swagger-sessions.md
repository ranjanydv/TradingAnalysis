# Codex Prompt — Addendum: RBAC, Swagger Split, Rotating Refresh Tokens
# Apply these changes on top of codex-master-prompt-v3.md
# You are currently on Phase 3. Integrate these into the Identity module before proceeding.

---

# SECTION A — Roles and Permissions (RBAC)

## EF Core entities

### Role
```
Table: "role"
PK: int (serial) — NOT Guid
Properties:
  Id (int), Name (text, unique), Description (text?),
  IsSystemRole (bool, default false),
  CreatedAt, UpdatedAt

No BaseEntity inheritance — uses int PK and has no domain events.
No soft delete.

EF config:
  builder.ToTable("role");
  builder.HasKey(r => r.Id);
  builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();
  builder.Property(r => r.Name).HasColumnName("name").IsRequired();
  builder.Property(r => r.Description).HasColumnName("description");
  builder.Property(r => r.IsSystemRole).HasColumnName("is_system_role").HasDefaultValue(false);
  builder.Property(r => r.CreatedAt).HasColumnName("created_at");
  builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
  builder.HasIndex(r => r.Name).IsUnique();
```

### Permission
```
Table: "permission"
PK: int (serial)
Properties:
  Id (int), Module (text), Action (text), Code (text, unique),
  Description (text?), CreatedAt, UpdatedAt

Code format: "{Module}_{Action}" — e.g. "Finance_CREATE", "Market_READ"

No BaseEntity. No soft delete.

EF config:
  builder.HasIndex(p => p.Code).IsUnique();
  builder.HasIndex(p => new { p.Module, p.Action }).IsUnique();
```

### RolePermission (join table — composite PK)
```
Table: "role_permission"
Properties:
  RoleId (int FK → role.id, cascade delete),
  PermissionId (int FK → permission.id, cascade delete),
  CreatedAt, UpdatedAt

EF config:
  builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
  builder.HasOne<Role>().WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
  builder.HasOne<Permission>().WithMany().HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);
```

Add DbSet properties to AppDbContext:
```csharp
public DbSet<Role> Roles => Set<Role>();
public DbSet<Permission> Permissions => Set<Permission>();
public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
```

## Permission loading at login

When a customer or admin logs in, load their permissions and embed them as JWT claims:

```csharp
// In LoginWithEmailCommand handler, after verifying password:
var permissions = await LoadPermissionsAsync(customer.RoleId, ct);

// Pass permissions to JWT generation:
string jwt = jwtService.GenerateCustomerToken(
    customer.Id, customer.Email, customer.Phone,
    roleName, permissions);
```

Update `IJwtService` and `JwtService`:

```csharp
public interface IJwtService
{
    string GenerateCustomerToken(
        Guid customerId, string? email, string? phone,
        string role, IEnumerable<string> permissions);

    string GenerateAdminToken(
        Guid adminId, string email,
        string role, IEnumerable<string> permissions);
}

// In JwtService — add permissions as individual claims:
// Each permission code becomes a separate claim:
// new Claim("permission", "Finance_CREATE")
// new Claim("permission", "Market_READ")
// etc.
// Keep claim name "permission" (plural codes, one claim per permission).
```

Add to `Constants.ClaimTypes`:
```csharp
public const string Permission = "permission";
public const string RoleName = "role_name"; // human-readable role name in token
```

## Permission helper in Shared.Kernel

```csharp
// src/Shared/Kernel/Auth/PermissionRequirement.cs
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

// src/Shared/Kernel/Auth/PermissionAuthorizationHandler.cs
public class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User
            .FindAll(Constants.ClaimTypes.Permission)
            .Any(c => c.Value == requirement.Permission);

        if (hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

Register in `AddJwtAuthentication` (DependencyInjection.cs):
```csharp
services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

Usage in controllers:
```csharp
// Attribute-based permission check — add to AdminOnly endpoints
[Authorize(Policy = "permission:Finance_READ")]
[HttpGet("finance/reports")]
public async Task<IActionResult> GetReports(...) { ... }

// Register permission policies dynamically in AddJwtAuthentication:
// Policies are registered on-the-fly using a convention:
options.AddPolicy("permission:Finance_READ",
    p => p.Requirements.Add(new PermissionRequirement("Finance_READ")));
// For a cleaner approach, use a policy provider:
```

```csharp
// src/Shared/Infrastructure/Auth/PermissionPolicyProvider.cs
// IAuthorizationPolicyProvider implementation that auto-creates policies
// for any policy name matching "permission:{CODE}" pattern
// so you never need to manually register each permission as a policy.

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PermissionPrefix = "permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPrefix))
        {
            var permission = policyName[PermissionPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}
```

Register:
```csharp
// In AddJwtAuthentication:
services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
```

## LoadPermissionsAsync helper (used in login handlers)

Add this private method to both Login handlers (or extract to a shared service):

```csharp
private async Task<List<string>> LoadPermissionsAsync(int? roleId, CancellationToken ct)
{
    if (roleId == null) return [];

    return await _db.RolePermissions
        .Where(rp => rp.RoleId == roleId)
        .Join(_db.Permissions,
            rp => rp.PermissionId,
            p => p.Id,
            (_, p) => p.Code)
        .ToListAsync(ct);
}
```

Also cache the result to avoid hitting DB on every login for the same role:
```csharp
var cacheKey = $"role_permissions:{roleId}";
var permissions = await _cache.GetOrSetAsync(
    cacheKey,
    () => LoadPermissionsAsync(roleId, ct),
    TimeSpan.FromMinutes(10),
    ct);
```

## Admin RBAC queries and commands

Add these to the Identity module:

```
GetRolesQuery [AdminOnly] → List<RoleDto>
GetRoleByIdQuery(id) [AdminOnly] → RoleDetailDto (includes permissions)
GetPermissionsQuery [AdminOnly] → List<PermissionDto> (grouped by module)

CreateRoleCommand(Name, Description, PermissionIds) [AdminOnly]
  - Reject if name conflicts with existing role
  - Create Role + RolePermission rows

UpdateRolePermissionsCommand(RoleId, PermissionIds) [AdminOnly]
  - Block if role.IsSystemRole
  - Delete existing RolePermissions, insert new ones
  - Invalidate role_permissions cache for this roleId

AssignRoleToCustomerCommand(CustomerId, RoleId) [AdminOnly]
  - customer.RoleId = roleId
  - Log audit

AssignRoleToAdminCommand(AdminId, RoleId) [AdminOnly]
  - adminUser.RoleId = roleId
  - Log audit

DeleteRoleCommand(RoleId) [AdminOnly]
  - Block if role.IsSystemRole → Failure("Cannot delete a system role")
  - Delete role (cascade removes role_permission rows)
```

---

# SECTION B — Separate Swagger Docs (Admin vs Customer)

Two completely separate Swagger UIs:
- `/swagger/customer` — all customer-facing endpoints
- `/swagger/admin` — all admin endpoints

## Tag controllers

Every controller must be tagged with one group. No exceptions.

```csharp
// Customer-facing controllers:
[ApiExplorerSettings(GroupName = SwaggerGroups.Customer)]
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : AppControllerBase { ... }

// Admin-facing controllers:
[ApiExplorerSettings(GroupName = SwaggerGroups.Admin)]
[ApiController]
[Route("api/v1/admin/[controller]")]
public class AdminCustomersController : AppControllerBase { ... }
```

```csharp
// src/Shared/Kernel/Auth/SwaggerGroups.cs
public static class SwaggerGroups
{
    public const string Customer = "customer";
    public const string Admin = "admin";
}
```

## Swagger configuration

Replace `AddSwaggerWithJwt` in DependencyInjection.cs:

```csharp
public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
{
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        // Customer API doc
        c.SwaggerDoc(SwaggerGroups.Customer, new OpenApiInfo
        {
            Title = "Trading Analytics — Customer API",
            Version = "v1",
            Description = "Endpoints for customer-facing mobile and web applications",
        });

        // Admin API doc
        c.SwaggerDoc(SwaggerGroups.Admin, new OpenApiInfo
        {
            Title = "Trading Analytics — Admin API",
            Version = "v1",
            Description = "Endpoints for admin dashboard and internal tooling",
        });

        // Each doc only shows its own group
        c.DocInclusionPredicate((docName, apiDesc) =>
        {
            var groupName = apiDesc.GroupName;
            return docName == groupName;
        });

        // JWT bearer — applies to both docs
        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT token",
        };
        c.AddSecurityDefinition("Bearer", securityScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {{
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }});
    });

    return services;
}
```

## Swagger UI endpoints in Program.cs

Replace the single `app.UseSwaggerUI()` call:

```csharp
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Customer API
    c.SwaggerEndpoint(
        $"/swagger/{SwaggerGroups.Customer}/swagger.json",
        "Customer API v1");
    c.RoutePrefix = "swagger/customer";
});

// Mount a second UI instance for admin
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint(
        $"/swagger/{SwaggerGroups.Admin}/swagger.json",
        "Admin API v1");
    c.RoutePrefix = "swagger/admin";
});
```

Now accessible at:
- `http://localhost:5000/swagger/customer` — customer endpoints only
- `http://localhost:5000/swagger/admin` — admin endpoints only

## Controller structure going forward

Split every module's controllers into two files:

```
Modules/Identity/
  Controllers/
    AuthController.cs           ← [ApiExplorerSettings(GroupName = "customer")]
    DeviceController.cs         ← [ApiExplorerSettings(GroupName = "customer")]
    AdminCustomersController.cs ← [ApiExplorerSettings(GroupName = "admin")]
    AdminRolesController.cs     ← [ApiExplorerSettings(GroupName = "admin")]

Modules/Market/
  Controllers/
    MarketController.cs         ← "customer"
    AdminMarketController.cs    ← "admin"

Modules/Subscriptions/
  Controllers/
    SubscriptionsController.cs  ← "customer"
    PaymentsController.cs       ← "customer"
    AdminSubscriptionsController.cs ← "admin"
```

Admin controllers always:
- Have route prefix `/api/v1/admin/`
- Have `[Authorize(Policy = Policies.AdminOnly)]` at class level
- Have `[ApiExplorerSettings(GroupName = SwaggerGroups.Admin)]`

Customer controllers always:
- Have route prefix `/api/v1/`
- Have appropriate `[Authorize]` at method level
- Have `[ApiExplorerSettings(GroupName = SwaggerGroups.Customer)]`

---

# SECTION C — Rotating Refresh Token Session Strategy

This replaces the simple session token approach from the main prompt.
The pattern matches your NestJS `refresh token strategy`:
  - Login → `sessionId` + `accessToken` + `refreshToken`
  - Refresh → validate `sessionId` + `refreshToken` → issue new `sessionId` + `accessToken` + `refreshToken` → delete old session
  - Reuse detection: if a refresh token is used after it has already been rotated,
    the session won't exist → automatic rejection

## Updated session entities

### CustomerSession (updated)
```
Table: "customer_sessions"
Properties:
  Id (Guid UUIDv7) — this is the sessionId returned to client
  CustomerId (Guid)
  RefreshTokenHash (text) — SHA-256 hash of the raw refresh token
  AccessTokenJti (text?) — JWT ID claim, for future token revocation if needed
  ExpiresAt (DateTime) — refresh token expiry (30 days mobile, 7 days web)
  Type (SessionType — "mobile"/"web")
  UserDeviceId (Guid? nullable)
  IpAddress (text?)
  UserAgent (text?)
  CreatedAt, UpdatedAt

REMOVE: Token field (was a single opaque token — replaced by RefreshTokenHash)

Factory: CustomerSession.Create(customerId, type, deviceId?, ip?, userAgent?)
  → returns (CustomerSession session, string rawRefreshToken)
  Raw refresh token: 48 random bytes → base64url → never stored, only hash stored

IsExpired computed: DateTime.UtcNow >= ExpiresAt
```

### AdminSession (updated)
```
Same change — replace Token with RefreshTokenHash.
Admin sessions expire in 8 hours.
```

## Updated Redis session store

Sessions in Redis are keyed by `sessionId` (the Guid), not by raw token.
Redis stores the session metadata for fast lookups.
Refresh tokens are validated by hashing and comparing — never stored raw anywhere.

```csharp
// Updated SessionData record:
public record SessionData(
    Guid SessionId,
    Guid UserId,
    string ActorType,
    string Role,
    List<string> Permissions,
    Guid? DeviceId,
    string SessionType,
    DateTime ExpiresAt);

// Updated ISessionStore:
public interface ISessionStore
{
    // Key: sessionId
    Task SetAsync(Guid sessionId, SessionData data, TimeSpan ttl, CancellationToken ct = default);
    Task<SessionData?> GetAsync(Guid sessionId, CancellationToken ct = default);
    Task RemoveAsync(Guid sessionId, CancellationToken ct = default);
    Task RemoveAllForUserAsync(Guid userId, CancellationToken ct = default);
}

// RedisSessionStore keys:
// session:{sessionId}  →  SessionData JSON
// user_sessions:{userId}  →  Set of session:{sessionId} keys
```

## Auth response DTO

All login and refresh endpoints return this shape:

```csharp
public class AuthTokensDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;  // raw, sent to client
    public Guid SessionId { get; init; }
    public int AccessTokenExpiresInSeconds { get; init; }
    public int RefreshTokenExpiresInSeconds { get; init; }
}

public class AuthResponseDto
{
    public AuthTokensDto Tokens { get; init; } = default!;
    public CustomerProfileDto Profile { get; init; } = default!;
}
```

## Refresh token hashing helper

Add to `src/Shared/Infrastructure/Auth/`:

```csharp
public static class RefreshTokenHasher
{
    /// <summary>
    /// Generates a cryptographically random refresh token and its SHA-256 hash.
    /// Only the hash is stored. The raw token is sent to the client once and never stored.
    /// </summary>
    public static (string rawToken, string hash) Generate()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLower();
        return (raw, hash);
    }

    public static string Hash(string rawToken)
        => Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLower();
}
```

## Updated CustomerSession.Create

```csharp
public static (CustomerSession session, string rawRefreshToken) Create(
    Guid customerId, SessionType type,
    Guid? deviceId = null, string? ip = null, string? userAgent = null)
{
    var (rawToken, tokenHash) = RefreshTokenHasher.Generate();

    var session = new CustomerSession
    {
        Id = NewId.Next(),           // sessionId — returned to client
        CustomerId = customerId,
        RefreshTokenHash = tokenHash, // hash only — raw never stored
        ExpiresAt = type == SessionType.Mobile
            ? DateTime.UtcNow.AddDays(30)
            : DateTime.UtcNow.AddDays(7),
        Type = type,
        UserDeviceId = deviceId,
        IpAddress = ip,
        UserAgent = userAgent,
    };

    return (session, rawToken);
}
```

## Updated login flow (applies to all login commands)

```csharp
// After credential verification — replace old session creation block with:

// 1. For mobile: revoke all existing mobile sessions for this device
if (sessionType == SessionType.Mobile && deviceId.HasValue)
{
    var oldSessions = await _db.CustomerSessions
        .Where(s => s.CustomerId == customer.Id
            && s.Type == SessionType.Mobile
            && s.UserDeviceId == deviceId)
        .ToListAsync(ct);

    foreach (var old in oldSessions)
        await _sessionStore.RemoveAsync(old.Id, ct);

    _db.CustomerSessions.RemoveRange(oldSessions);
}

// 2. Create new session
var (session, rawRefreshToken) = CustomerSession.Create(
    customer.Id, sessionType, deviceId, ip, userAgent);

// 3. Persist to Postgres
await _db.CustomerSessions.AddAsync(session, ct);
await _db.SaveChangesAsync(ct);

// 4. Persist to Redis (store SessionData with permissions for fast auth)
var permissions = await LoadPermissionsAsync(customer.RoleId, ct);
var roleName = await LoadRoleNameAsync(customer.RoleId, ct);

await _sessionStore.SetAsync(session.Id, new SessionData(
    SessionId: session.Id,
    UserId: customer.Id,
    ActorType: Constants.ActorTypes.Customer,
    Role: roleName,
    Permissions: permissions,
    DeviceId: deviceId,
    SessionType: sessionType.ToString().ToLower(),
    ExpiresAt: session.ExpiresAt),
    session.ExpiresAt - DateTime.UtcNow,
    ct);

// 5. Generate JWT (short-lived, 60 min)
var jwt = _jwtService.GenerateCustomerToken(
    customer.Id, customer.Email, customer.Phone, roleName, permissions);

// 6. Return
return Result<AuthResponseDto>.Success(new AuthResponseDto
{
    Tokens = new AuthTokensDto
    {
        AccessToken = jwt,
        RefreshToken = rawRefreshToken,    // raw — client stores this securely
        SessionId = session.Id,            // client stores this alongside refresh token
        AccessTokenExpiresInSeconds = 3600,
        RefreshTokenExpiresInSeconds = sessionType == SessionType.Mobile ? 2592000 : 604800,
    },
    Profile = MapToProfileDto(customer),
});
```

## RefreshTokenCommand (full rotating refresh flow)

```csharp
public record RefreshTokenCommand(
    Guid SessionId,
    string RawRefreshToken)
    : IRequest<Result<AuthTokensDto>>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokensDto>>
{
    private readonly AppDbContext _db;
    private readonly ISessionStore _sessionStore;
    private readonly IJwtService _jwt;
    private readonly ICacheService _cache;

    public async Task<Result<AuthTokensDto>> Handle(
        RefreshTokenCommand cmd, CancellationToken ct)
    {
        // 1. Hash the incoming refresh token
        var incomingHash = RefreshTokenHasher.Hash(cmd.RawRefreshToken);

        // 2. Load the session from Postgres by sessionId
        //    (Postgres is the source of truth for refresh token validation)
        var session = await _db.CustomerSessions
            .FirstOrDefaultAsync(s => s.Id == cmd.SessionId && !s.IsExpired, ct);

        if (session == null)
            return Result<AuthTokensDto>.Failure(
                "Session not found or expired. Please log in again.");

        // 3. Validate refresh token hash — constant-time comparison
        var hashBytes = Encoding.UTF8.GetBytes(incomingHash);
        var storedBytes = Encoding.UTF8.GetBytes(session.RefreshTokenHash);
        var tokenValid = CryptographicOperations.FixedTimeEquals(hashBytes, storedBytes);

        if (!tokenValid)
        {
            // Possible refresh token reuse attack — invalidate the session immediately
            _db.CustomerSessions.Remove(session);
            await _sessionStore.RemoveAsync(session.Id, ct);
            await _db.SaveChangesAsync(ct);
            return Result<AuthTokensDto>.Failure(
                "Invalid refresh token. Session has been terminated for security.");
        }

        // 4. Load customer
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == session.CustomerId, ct);

        if (customer == null || customer.Banned)
            return Result<AuthTokensDto>.Failure("Account unavailable.");

        // 5. Generate new refresh token
        var (newRawToken, newHash) = RefreshTokenHasher.Generate();

        // 6. Create new session record
        var newSession = new CustomerSession
        {
            Id = NewId.Next(),                   // new sessionId
            CustomerId = customer.Id,
            RefreshTokenHash = newHash,
            ExpiresAt = session.Type == SessionType.Mobile
                ? DateTime.UtcNow.AddDays(30)
                : DateTime.UtcNow.AddDays(7),
            Type = session.Type,
            UserDeviceId = session.UserDeviceId,
            IpAddress = session.IpAddress,
            UserAgent = session.UserAgent,
        };

        // 7. Delete old session, persist new session (atomic)
        _db.CustomerSessions.Remove(session);
        await _db.CustomerSessions.AddAsync(newSession, ct);
        await _db.SaveChangesAsync(ct);

        // 8. Update Redis — remove old, add new
        await _sessionStore.RemoveAsync(session.Id, ct);

        var permissions = await _cache.GetOrSetAsync(
            $"role_permissions:{customer.RoleId}",
            () => LoadPermissionsAsync(customer.RoleId, ct),
            TimeSpan.FromMinutes(10), ct);

        var roleName = await LoadRoleNameAsync(customer.RoleId, ct);

        await _sessionStore.SetAsync(newSession.Id, new SessionData(
            SessionId: newSession.Id,
            UserId: customer.Id,
            ActorType: Constants.ActorTypes.Customer,
            Role: roleName,
            Permissions: permissions,
            DeviceId: newSession.UserDeviceId,
            SessionType: newSession.Type.ToString().ToLower(),
            ExpiresAt: newSession.ExpiresAt),
            newSession.ExpiresAt - DateTime.UtcNow,
            ct);

        // 9. Issue new JWT
        var newJwt = _jwt.GenerateCustomerToken(
            customer.Id, customer.Email, customer.Phone, roleName, permissions);

        return Result<AuthTokensDto>.Success(new AuthTokensDto
        {
            AccessToken = newJwt,
            RefreshToken = newRawToken,
            SessionId = newSession.Id,
            AccessTokenExpiresInSeconds = 3600,
            RefreshTokenExpiresInSeconds = newSession.Type == SessionType.Mobile
                ? 2592000 : 604800,
        });
    }
}
```

## GetMySessionsQuery (updated to reflect new schema)

```csharp
// Returns list of active sessions so user can see and revoke individual devices
public class SessionDto : TimestampedDto
{
    public Guid SessionId { get; init; }
    public string Type { get; init; } = string.Empty;      // "mobile" | "web"
    public string? DeviceName { get; init; }
    public string? DeviceType { get; init; }
    public string? IpAddress { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsCurrentSession { get; init; }            // true if matches current request's sessionId
}

// Handler: join CustomerSessions with UserDevice to get device info
// ICurrentUserService provides current userId
// Current sessionId comes from a custom claim in the JWT:
//   add "session_id" claim in GenerateCustomerToken
//   read in handler to mark IsCurrentSession = true
```

Add `session_id` to JWT claims:
```csharp
// In Constants.ClaimTypes:
public const string SessionId = "session_id";

// In JwtService.GenerateCustomerToken — add sessionId parameter:
public string GenerateCustomerToken(
    Guid customerId, string? email, string? phone,
    string role, IEnumerable<string> permissions,
    Guid sessionId)
{
    var claims = new List<Claim>
    {
        ...existing claims...,
        new(Constants.ClaimTypes.SessionId, sessionId.ToString()),
    };
    return Build(claims);
}
```

## RevokeSessionCommand (single session revoke)

```csharp
public record RevokeSessionCommand(Guid SessionId) : IRequest<Result>;

// Handler:
// 1. Load CustomerSession where Id = sessionId AND CustomerId = currentUser.UserId
// 2. If not found → Failure("Session not found")
// 3. Delete from Postgres
// 4. sessionStore.RemoveAsync(sessionId)
// 5. Return Ok
```

## Updated LogoutCommand

```csharp
public record LogoutCommand : IRequest<Result>;

// Handler:
// 1. Read sessionId from ICurrentUserService (add SessionId property)
//    or read from a separate session context
// 2. Delete from Postgres by sessionId + userId (security: must own the session)
// 3. sessionStore.RemoveAsync(sessionId)
```

Update `ICurrentUserService` to expose `SessionId`:
```csharp
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? SessionId { get; }       // add this
    string? ActorType { get; }
    string? Role { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}

// CurrentUserService implementation:
public Guid? SessionId
{
    get
    {
        var val = User?.FindFirstValue(Constants.ClaimTypes.SessionId);
        return Guid.TryParse(val, out var id) ? id : null;
    }
}
```

## Refresh endpoint in AuthController

```csharp
// POST /api/v1/auth/refresh
// No [Authorize] attribute — this endpoint accepts expired JWTs implicitly
// Client sends sessionId + refreshToken in request body, not in Authorization header
[HttpPost("refresh")]
[AllowAnonymous]
[ApiExplorerSettings(GroupName = SwaggerGroups.Customer)]
public async Task<IActionResult> Refresh(RefreshTokenCommand cmd, CancellationToken ct)
{
    var result = await _sender.Send(cmd, ct);
    return OkResult(result, "Token refreshed");
}
```

---

# SECTION D — Integration notes for Phase 3

Apply all of the above to Phase 3 before finishing Identity module.

## Files to add to Identity module:
```
src/Modules/Identity/
  Domain/
    Entities/
      Role.cs                     ← int PK, no BaseEntity
      Permission.cs               ← int PK, no BaseEntity
      RolePermission.cs           ← composite PK join table
  Application/
    Commands/
      RefreshTokenCommand.cs + Handler
      RevokeSessionCommand.cs + Handler
      CreateRoleCommand.cs + Handler      [AdminOnly]
      UpdateRolePermissionsCommand.cs + Handler [AdminOnly]
      AssignRoleToCustomerCommand.cs + Handler  [AdminOnly]
      DeleteRoleCommand.cs + Handler            [AdminOnly]
    Queries/
      GetRolesQuery.cs + Handler         [AdminOnly]
      GetPermissionsQuery.cs + Handler   [AdminOnly]
    DTOs/
      AuthTokensDto.cs
      AuthResponseDto.cs
      SessionDto.cs
      RoleDto.cs
      PermissionDto.cs
  Infrastructure/
    Persistence/
      Configurations/
        RoleConfiguration.cs
        PermissionConfiguration.cs
        RolePermissionConfiguration.cs
        CustomerSessionConfiguration.cs  ← updated (RefreshTokenHash replaces Token)
        AdminSessionConfiguration.cs     ← updated
  Controllers/
    AuthController.cs                    ← [GroupName = "customer"]
    DevicesController.cs                 ← [GroupName = "customer"]
    AdminCustomersController.cs          ← [GroupName = "admin"]
    AdminRolesController.cs              ← [GroupName = "admin"]
```

## Files to update in Shared.Infrastructure:
```
Auth/
  IJwtService.cs         ← add permissions + sessionId parameters
  JwtService.cs          ← add permission claims + session_id claim
  CurrentUserService.cs  ← add SessionId property
  RefreshTokenHasher.cs  ← new file
  PermissionRequirement.cs         ← new file
  PermissionAuthorizationHandler.cs ← new file
  PermissionPolicyProvider.cs       ← new file
Session/
  ISessionStore.cs       ← updated (keyed by Guid sessionId, includes Permissions)
  RedisSessionStore.cs   ← updated
  SessionData.cs         ← updated (add SessionId, Permissions, remove raw token)
Swagger/
  SwaggerConfig.cs       ← updated (two docs)
DependencyInjection.cs   ← register PermissionPolicyProvider, PermissionAuthorizationHandler
```

## AppDbContext — add these DbSets:
```csharp
public DbSet<Role> Roles => Set<Role>();
public DbSet<Permission> Permissions => Set<Permission>();
public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
public DbSet<Customer> Customers => Set<Customer>();
public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
public DbSet<Account> Accounts => Set<Account>();
public DbSet<CustomerSession> CustomerSessions => Set<CustomerSession>();
public DbSet<AdminSession> AdminSessions => Set<AdminSession>();
public DbSet<UserDevice> UserDevices => Set<UserDevice>();
public DbSet<Verification> Verifications => Set<Verification>();
```

## Summary of what changes from the main prompt

| Area | Old | New |
|---|---|---|
| Session token | single opaque token stored hashed | sessionId (UUID) + refreshToken (hashed separately) |
| Refresh | extend existing session, new JWT | new sessionId + new refreshToken + new JWT, old session deleted |
| Redis key | `session:{token}` | `session:{sessionId}` |
| SessionData in Redis | no permissions | includes permissions list (avoids DB on every request) |
| JWT claims | sub, actor_type, role | + permission[] + session_id |
| Authorization | policy-based (AdminOnly/CustomerOnly) | + permission-based via PermissionPolicyProvider |
| Swagger | single doc at /swagger | two docs: /swagger/customer + /swagger/admin |
| Roles/Permissions | roleId int FK on Customer/Admin | full Role + Permission + RolePermission entities |
