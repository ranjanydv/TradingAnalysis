# Trading Analytics — Codex Master Prompt v3
# Single ASP.NET Core 10 Web API · Modular Monolith · Clean Architecture
# Implement one phase at a time. Do not proceed until instructed.

---

## Project overview

Trading Analytics platform for NEPSE (Nepal Stock Exchange).
Single deployable ASP.NET Core 10 Web API, modular monolith architecture.

Infrastructure:
- PostgreSQL — primary data store, EF Core owns the schema, migrations used
- MongoDB — notifications inbox + audit logs only
- Redis — session storage, caching, SignalR backplane
- Hangfire (Postgres-backed) — background jobs and scheduled tasks
- Firebase Admin SDK — FCM push notifications

---

## Absolute rules — every line of code, every phase

1. `net10.0` target framework
2. One `.sln`, one `.csproj` (`TradingAnalytics.API`), everything else is folders
3. No ASP.NET Core Identity — no `UserManager`, `SignInManager`, `RoleManager`, `IdentityUser`, or any `Microsoft.AspNetCore.Identity` package
4. No `Guid.NewGuid()` — always `NewId.Next()` (UUIDv7 via `UUIDNext`)
5. No BCrypt — Argon2id via `Isopoh.Cryptography.Argon2` for passwords, HMAC-SHA256 for OTPs
6. No exceptions for business logic — always return `Result<T>` or `Result`
7. All `DateTime` values are UTC — `DateTime.UtcNow` everywhere
8. `ApiResponse<T>` is the only response envelope — never return raw objects
9. Cursor-based pagination for all list endpoints (offset only for admin exports)
10. All entity properties use `private set` — state via domain methods only
11. Constructor injection everywhere — no service locator, no static resolution
12. Modules never reference each other's classes — only via MediatR or Shared.Kernel interfaces
13. Every command/query handler returns `Result<T>` — never throws for business failures
14. All async methods take `CancellationToken ct = default` as last parameter
15. Every entity has a `private` parameterless constructor for EF Core
16. Use explicit namespaces that match the folder/module structure; never use the global namespace
17. Add XML documentation comments for all public classes, interfaces, methods, and properties
18. Prefer primary constructors for DI-based services, handlers, repositories, and providers
19. Shared.Kernel must not reference infrastructure or persistence-specific types
20. User-facing and reusable log/error strings must come from resources, not inline literals
21. Pin package versions explicitly in the project file; do not rely on floating latest restore behavior
22. Use `ConfigureAwait(false)` in library/infrastructure code where no ASP.NET request context is required

---

## Project standards overlay

Apply these standards in every phase in addition to the phase-specific requirements:

- Namespace convention:
  - Shared kernel and infrastructure use `TradingAnalytics.Shared.{Feature}`
  - Modules use `TradingAnalytics.Modules.{ModuleName}.{Domain|Application|Infrastructure}`
  - Controllers use `TradingAnalytics.API.Controllers`
- Documentation:
  - Every public API surface must include XML docs with parameter and return descriptions where applicable
- Dependency injection:
  - Prefer primary constructor syntax
  - Guard injected dependencies with `ArgumentNullException.ThrowIfNull(...)` when not using a primary constructor-only assignment style
  - Register abstractions behind interfaces unless the type is a pure framework composition root detail
- Command/query handling:
  - Standardize command handlers behind shared abstractions such as `ICommandHandler<TCommand, TResponse>` and a reusable base handler where validation/logging/cross-cutting behavior belongs
  - Keep MediatR requests thin; business rules belong in domain methods and application services
- Resources:
  - Add `Resources/ErrorMessages.resx` and `Resources/LogMessages.resx`
  - Access reusable messages through `ResourceManager`
- Architecture boundaries:
  - Shared.Kernel contracts may expose DTOs/value objects, but never MongoDB, EF Core, Redis, Firebase, or other infrastructure-specific models
- Testing:
  - Use MSTest + FluentAssertions + Moq for automated tests created from this prompt

---

## Folder structure

```
TradingAnalytics.sln
TradingAnalytics.API/
  TradingAnalytics.API.csproj
  Program.cs
  appsettings.json
  appsettings.Development.json
  src/
    Shared/
      Kernel/
        Entities/
          BaseEntity.cs
          AggregateRoot.cs
        Interfaces/
          IDomainEvent.cs
          IRepository.cs
          ICurrentUserService.cs
          IDateTimeService.cs
          IAuditLogger.cs
          INotificationService.cs
          IMarketPriceService.cs
        Pagination/
          CursorResult.cs
          CursorEncoder.cs
          PagedResult.cs
        Http/
          ApiResponse.cs
          QueryParams.cs
          TimestampedDto.cs
        Auth/
          Policies.cs
        Results/
          Result.cs
        Extensions/
          StringExtensions.cs
        Constants.cs
        NewId.cs
      Infrastructure/
        Persistence/
          AppDbContext.cs
          UuidV7ValueGenerator.cs
          CursorQueryExtensions.cs
        Auth/
          IJwtService.cs + JwtService.cs
          JwtConfig.cs
          IPasswordService.cs + PasswordService.cs
          OtpHasher.cs
          CurrentUserService.cs
        Cache/
          ICacheService.cs
          RedisCacheService.cs
          CacheKeys.cs
        Session/
          ISessionStore.cs
          RedisSessionStore.cs
        MongoDB/
          Documents/
            NotificationDocument.cs
            NotificationSettingDocument.cs
            AuditLogDocument.cs
          IMongoRepository.cs
          MongoRepository.cs
          MongoIndexInitializer.cs
          AuditLogger.cs
          NotificationService.cs
        Firebase/
          IFirebaseMessagingService.cs
          FirebaseMessagingService.cs
          FirebaseConfig.cs
        Behaviours/
          ValidationBehaviour.cs
          LoggingBehaviour.cs
        Http/
          AppControllerBase.cs
          GlobalExceptionMiddleware.cs
        Swagger/
          SwaggerConfig.cs
        DependencyInjection.cs
    Modules/
      Identity/
        Domain/ Application/ Infrastructure/
        IdentityModule.cs
      Market/
        Domain/ Application/ Infrastructure/
        MarketModule.cs
      Portfolio/
        Domain/ Application/ Infrastructure/
        PortfolioModule.cs
      Subscriptions/
        Domain/ Application/ Infrastructure/
        SubscriptionsModule.cs
      Courses/
        Domain/ Application/ Infrastructure/
        CoursesModule.cs
      Content/
        Domain/ Application/ Infrastructure/
        ContentModule.cs
```

---

## NuGet packages (TradingAnalytics.API.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>TradingAnalytics</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <!-- Database — Postgres -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="x.y.z" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="x.y.z" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="x.y.z" />

    <!-- Database — MongoDB -->
    <PackageReference Include="MongoDB.Driver" Version="x.y.z" />

    <!-- Cache + Session — Redis -->
    <PackageReference Include="StackExchange.Redis" Version="x.y.z" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="x.y.z" />

    <!-- Auth -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="x.y.z" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="x.y.z" />

    <!-- CQRS + Validation -->
    <PackageReference Include="MediatR" Version="x.y.z" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="x.y.z" />

    <!-- IDs -->
    <PackageReference Include="UUIDNext" Version="x.y.z" />

    <!-- Hashing -->
    <PackageReference Include="Isopoh.Cryptography.Argon2" Version="x.y.z" />

    <!-- Background jobs -->
    <PackageReference Include="Hangfire.AspNetCore" Version="x.y.z" />
    <PackageReference Include="Hangfire.PostgreSql" Version="x.y.z" />

    <!-- Firebase FCM -->
    <PackageReference Include="FirebaseAdmin" Version="x.y.z" />

    <!-- SignalR Redis backplane -->
    <PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="x.y.z" />

    <!-- Resilience -->
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="x.y.z" />

    <!-- File processing -->
    <PackageReference Include="CsvHelper" Version="x.y.z" />

    <!-- Swagger -->
    <PackageReference Include="Swashbuckle.AspNetCore" Version="x.y.z" />
  </ItemGroup>
</Project>
```

Replace every `x.y.z` placeholder with an explicit tested version before implementation. Do not leave package versions floating.

---

## appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=trading_analytics;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "Database": "trading_analytics"
  },
  "Jwt": {
    "Secret": "CHANGE_THIS_TO_256_BIT_SECRET_MIN_32_CHARS",
    "Issuer": "TradingAnalytics",
    "Audience": "TradingAnalytics",
    "ExpiryMinutes": 60
  },
  "Auth": {
    "OtpHmacSecret": "CHANGE_THIS_32_BYTE_MIN_RANDOM_SECRET"
  },
  "Firebase": {
    "CredentialFilePath": "firebase-service-account.json"
  },
  "ESewa": {
    "ProductCode": "EPAYTEST",
    "SecretKey": "8gBm/:&EnhH.1/q",
    "BaseUrl": "https://rc-epay.esewa.com.np"
  },
  "Khalti": {
    "SecretKey": "your-test-secret-key",
    "BaseUrl": "https://a.khalti.com"
  },
  "FonePay": {
    "MerchantCode": "TEST",
    "SecretKey": "test-secret",
    "BaseUrl": "https://dev-clientapi.fonepay.com"
  },
  "Nepse": {
    "BaseUrl": "https://nepalstock.com/api",
    "PollingIntervalMinutes": 5,
    "TradingStartUtcHour": 4,
    "TradingEndUtcHour": 9
  },
  "Cache": {
    "MarketDataTtlSeconds": 60,
    "UserAccessTtlSeconds": 300,
    "StockQuoteTtlSeconds": 30
  },
  "AllowedOrigins": "http://localhost:3000,http://localhost:5173"
}
```

Note: NEPSE trading hours are 10:00–15:00 NPT = 04:15–09:15 UTC.
Use UTC hours 4–9 as the polling window (approximate, good enough for polling).

---

# PHASE 1 — Shared Kernel

Implement `src/Shared/Kernel/` only. No infrastructure, no modules.

## NewId.cs
```csharp
using UUIDNext;
public static class NewId
{
    /// <summary>UUIDv7 — monotonically increasing, DB-friendly, timestamp-embedded.</summary>
    public static Guid Next() => Uuid.NewDatabaseFriendly(Database.PostgreSql);
}
```

## Result.cs
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

public class Result : Result<Unit>
{
    private Result(bool isSuccess, string? error) : base(isSuccess, Unit.Value, error) { }
    public static Result Success() => new(true, null);
    public static new Result Failure(string error) => new(false, error);
}

public record Unit
{
    public static readonly Unit Value = new();
}
```

## BaseEntity.cs
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = NewId.Next();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

public abstract class AggregateRoot : BaseEntity { }
```

## IDomainEvent.cs
```csharp
using MediatR;
public interface IDomainEvent : INotification { }
```

## IRepository.cs
```csharp
public interface IRepository<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
```

## ICurrentUserService.cs
```csharp
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? ActorType { get; }   // "customer" | "admin"
    string? Role { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}
```

## IDateTimeService.cs
```csharp
public interface IDateTimeService
{
    DateTime UtcNow { get; }
}
```

## IAuditLogger.cs
```csharp
public interface IAuditLogger
{
    Task LogAsync(AuditLogEntry entry, CancellationToken ct = default);
}

public class AuditLogEntry
{
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserRole { get; init; }
    public required string Action { get; init; }
    public required string Module { get; init; }
    public string Status { get; init; } = "success"; // "success"|"failure"|"pending"|"partial"
    public string? ResourceId { get; init; }
    public string? ResourceType { get; init; }
    public List<AuditChange>? Changes { get; init; }
    public string? Reason { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public AuditRequestContext? Request { get; init; }
    public AuditErrorContext? Error { get; init; }
}

public record AuditChange(string Field, object? From, object? To);

public record AuditRequestContext(
    string? Ip, string? UserAgent, string? Endpoint,
    string? Method, int? StatusCode, long? DurationMs);

public record AuditErrorContext(string? Code, string? Message, string? Stack);
```

## INotificationService.cs
```csharp
public interface INotificationService
{
    /// <summary>Create and dispatch a notification across specified surfaces.</summary>
    Task SendAsync(SendNotificationRequest request, CancellationToken ct = default);

    /// <summary>Get unread in-app notifications for a recipient.</summary>
    Task<List<NotificationInboxItem>> GetInAppAsync(
        string recipientId, NotificationSurface surface,
        int limit = 20, string? afterId = null,
        CancellationToken ct = default);

    /// <summary>Update surface state (delivered, seen, read, archived).</summary>
    Task UpdateSurfaceStateAsync(
        string notificationId, NotificationSurface surface,
        NotificationSurfaceState newState,
        CancellationToken ct = default);

    /// <summary>Bulk mark all in-app notifications as read for a user.</summary>
    Task MarkAllReadAsync(string recipientId, NotificationSurface surface,
        CancellationToken ct = default);
}

public class NotificationInboxItem
{
    public required string Id { get; init; }
    public required string RecipientId { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public string? Image { get; init; }
    public Dictionary<string, object>? Data { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public NotificationSurface Surface { get; init; }
    public NotificationSurfaceState State { get; init; }
}

public class SendNotificationRequest
{
    public required string RecipientId { get; init; }
    public string? ExternalRef { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public string? Image { get; init; }
    public Dictionary<string, object>? Data { get; init; }
    public NotificationType Type { get; init; } = NotificationType.Transactional;
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;
    public List<NotificationSurface> Surfaces { get; init; } = [NotificationSurface.WebInApp];
}

public enum NotificationSurface
{
    MobileInApp, WebInApp, MobilePush, WebPush, Email, Sms
}

public enum NotificationSurfaceState
{
    Pending, Delivered, Seen, Read, Archived, Deleted
}

public enum NotificationType { Transactional, Promotional, System, Personal }
public enum NotificationPriority { High, Normal, Low }
```

## IMarketPriceService.cs
```csharp
/// <summary>
/// Cross-module contract. Portfolio depends on this interface.
/// Market module provides the implementation.
/// Never import Market module classes directly from Portfolio.
/// </summary>
public interface IMarketPriceService
{
    Task<IReadOnlyDictionary<string, decimal>> GetCurrentPricesAsync(
        IEnumerable<string> symbols, CancellationToken ct = default);
}
```

## Constants.cs
```csharp
public static class Constants
{
    public static class ClaimTypes
    {
        public const string UserId = "sub";
        public const string ActorType = "actor_type";
        public const string Role = "role";
        public const string Email = "email";
        public const string Phone = "phone";
    }

    public static class ActorTypes
    {
        public const string Customer = "customer";
        public const string Admin = "admin";
    }

    public static class Providers
    {
        public const string Credential = "credential";
        public const string Google = "google";
    }

    public static class Roles
    {
        public const string SuperAdmin = "super_admin";
        public const string Admin = "admin";
        public const string Moderator = "moderator";
    }
}
```

## StringExtensions.cs
```csharp
public static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        return string.Concat(str.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }

    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        return string.Concat(str.Split('_')
            .Select(s => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower()));
    }
}
```

## Pagination (CursorResult.cs, CursorEncoder.cs, PagedResult.cs)
```csharp
// CursorResult.cs
public class CursorResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public string? NextCursor { get; init; }
    public string? PrevCursor { get; init; }
    public bool HasMore => NextCursor != null;

    public CursorResult(IReadOnlyList<T> items, string? nextCursor, string? prevCursor = null)
    {
        Items = items;
        NextCursor = nextCursor;
        PrevCursor = prevCursor;
    }
}

// CursorEncoder.cs
public static class CursorEncoder
{
    public static string Encode(Guid id)
    {
        var json = JsonSerializer.Serialize(new { id = id.ToString() });
        return Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    public static Guid? DecodeId(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            var bytes = Base64UrlDecode(cursor);
            var doc = JsonSerializer.Deserialize<JsonElement>(bytes);
            return Guid.TryParse(doc.GetProperty("id").GetString(), out var id) ? id : null;
        }
        catch { return null; }
    }

    private static string Base64UrlEncode(byte[] b) =>
        Convert.ToBase64String(b).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(s);
    }
}

// PagedResult.cs
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items; TotalCount = totalCount; Page = page; PageSize = pageSize;
    }
}
```

## ApiResponse.cs + QueryParams.cs + TimestampedDto.cs
```csharp
// ApiResponse.cs
public class ApiResponse<T>
{
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public int? Count { get; init; }
    public int? CurrentPage { get; init; }
    public int? TotalPage { get; init; }
    public string? NextCursor { get; init; }
    public string? PrevCursor { get; init; }

    private ApiResponse() { }

    public static ApiResponse<T> Ok(string message, T? data = default)
        => new() { Message = message, Data = data };

    public static ApiResponse<T> Paged(string message, T data, int count, int page, int size)
        => new()
        {
            Message = message, Data = data, Count = count, CurrentPage = page,
            TotalPage = (int)Math.Ceiling((double)count / size),
        };

    public static ApiResponse<T> Cursored(string message, T data,
        string? nextCursor, string? prevCursor = null)
        => new() { Message = message, Data = data, NextCursor = nextCursor, PrevCursor = prevCursor };
}

public class ApiResponse
{
    public string Message { get; init; } = string.Empty;
    private ApiResponse() { }
    public static ApiResponse Ok(string message) => new() { Message = message };
}

// QueryParams.cs
public class QueryParams
{
    [FromQuery(Name = "after")] public string? After { get; set; }
    [FromQuery(Name = "before")] public string? Before { get; set; }

    private int _limit = 20;
    [FromQuery(Name = "limit")]
    public int Limit { get => _limit; set => _limit = Math.Clamp(value, 1, 100); }

    [FromQuery(Name = "page")] public int Page { get; set; } = 1;

    private int _size = 20;
    [FromQuery(Name = "size")]
    public int Size { get => _size; set => _size = Math.Clamp(value, 1, 100); }

    [FromQuery(Name = "sort")] public string Sort { get; set; } = "createdAt";
    [FromQuery(Name = "order")] public SortOrder Order { get; set; } = SortOrder.Desc;
    [FromQuery(Name = "search")] public string? Search { get; set; }

    public bool IsCursorPagination => After != null || Before != null;
    public int OffsetSkip => (Page - 1) * Size;
}

public enum SortOrder { Asc, Desc }

// TimestampedDto.cs
public abstract class TimestampedDto
{
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
```

## Policies.cs
```csharp
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string CustomerOnly = "CustomerOnly";
    public const string AnyActor = "AnyActor";
}
```

---

# PHASE 2 — Shared Infrastructure

Implement `src/Shared/Infrastructure/` only.

## UuidV7ValueGenerator.cs
```csharp
public class UuidV7ValueGenerator : ValueGenerator<Guid>
{
    public override Guid Next(EntityEntry entry) => NewId.Next();
    public override bool GeneratesTemporaryValues => false;
}
```

## AppDbContext.cs
```csharp
public class AppDbContext : DbContext
{
    private readonly IMediator _mediator;

    public AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator)
        : base(options) => _mediator = mediator;

    // DbSet<T> properties added here incrementally as modules are implemented.
    // Each phase adds its own DbSets to this file.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Apply UUIDv7 generator globally to all Guid Id properties
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProp = entityType.FindProperty("Id");
            if (idProp?.ClrType == typeof(Guid))
                idProp.SetValueGeneratorFactory((_, _) => new UuidV7ValueGenerator());
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Auto-stamp UpdatedAt on every modified entity
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified))
            entry.Entity.UpdatedAt = DateTime.UtcNow;

        var result = await base.SaveChangesAsync(ct);
        await DispatchDomainEventsAsync(ct);
        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count != 0)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var evt in events)
            await _mediator.Publish(evt, ct);
    }
}
```

## CursorQueryExtensions.cs
```csharp
public static class CursorQueryExtensions
{
    public static async Task<CursorResult<T>> ToCursorResultAsync<T>(
        this IQueryable<T> query,
        string? afterCursor,
        int limit,
        CancellationToken ct = default)
        where T : BaseEntity
    {
        var afterId = CursorEncoder.DecodeId(afterCursor);
        if (afterId.HasValue)
            query = query.Where(e => e.Id.CompareTo(afterId.Value) > 0);

        var items = await query.OrderBy(e => e.Id).Take(limit + 1).ToListAsync(ct);

        string? nextCursor = null;
        if (items.Count > limit)
        {
            items.RemoveAt(items.Count - 1);
            nextCursor = CursorEncoder.Encode(items[^1].Id);
        }

        return new CursorResult<T>(items, nextCursor);
    }
}
```

## Auth — IJwtService.cs + JwtService.cs
```csharp
public interface IJwtService
{
    string GenerateCustomerToken(Guid customerId, string? email, string? phone, string role);
    string GenerateAdminToken(Guid adminId, string email, string role);
}

public class JwtConfig
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}

public class JwtService : IJwtService
{
    private readonly JwtConfig _config;
    public JwtService(IOptions<JwtConfig> config) => _config = config.Value;

    public string GenerateCustomerToken(Guid customerId, string? email, string? phone, string role)
        => Build([
            new(Constants.ClaimTypes.UserId, customerId.ToString()),
            new(Constants.ClaimTypes.ActorType, Constants.ActorTypes.Customer),
            new(Constants.ClaimTypes.Role, role),
            .. (email != null ? new[] { new Claim(Constants.ClaimTypes.Email, email) } : []),
            .. (phone != null ? new[] { new Claim(Constants.ClaimTypes.Phone, phone) } : []),
        ]);

    public string GenerateAdminToken(Guid adminId, string email, string role)
        => Build([
            new(Constants.ClaimTypes.UserId, adminId.ToString()),
            new(Constants.ClaimTypes.ActorType, Constants.ActorTypes.Admin),
            new(Constants.ClaimTypes.Role, role),
            new(Constants.ClaimTypes.Email, email),
        ]);

    private string Build(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Secret));
        var token = new JwtSecurityToken(
            issuer: _config.Issuer,
            audience: _config.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.ExpiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

## Auth — IPasswordService.cs + PasswordService.cs (Argon2id)
```csharp
public interface IPasswordService
{
    string Hash(string plaintext);
    bool Verify(string plaintext, string hash);
}

public class PasswordService : IPasswordService
{
    // Argon2id — OWASP recommended. m=65536 (64MB), t=3, p=2
    public string Hash(string plaintext)
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

    public bool Verify(string plaintext, string hash)
        => Argon2.Verify(hash, plaintext, Encoding.UTF8);
}
```

## Auth — OtpHasher.cs (HMAC-SHA256)
```csharp
// OTPs are 6-digit, short-lived, rate-limited. HMAC-SHA256 is appropriate.
// Argon2 on OTPs would add 200ms per attempt for zero security gain.
public static class OtpHasher
{
    private static byte[]? _secret;

    public static void Configure(string secret)
        => _secret = Encoding.UTF8.GetBytes(secret);

    public static string Hash(string otp)
    {
        EnsureConfigured();
        return Convert.ToHexString(
            HMACSHA256.HashData(_secret!, Encoding.UTF8.GetBytes(otp))).ToLower();
    }

    public static bool Verify(string rawOtp, string storedHash)
    {
        var expected = Hash(rawOtp);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(storedHash));
    }

    private static void EnsureConfigured()
    {
        if (_secret == null)
            throw new InvalidOperationException("OtpHasher not configured.");
    }
}
```

## Auth — CurrentUserService.cs
```csharp
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var val = User?.FindFirstValue(Constants.ClaimTypes.UserId);
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public string? ActorType => User?.FindFirstValue(Constants.ClaimTypes.ActorType);
    public string? Role => User?.FindFirstValue(Constants.ClaimTypes.Role);
    public bool IsAdmin => ActorType == Constants.ActorTypes.Admin;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
}
```

## Cache — ICacheService.cs + RedisCacheService.cs
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl,
        CancellationToken ct = default);
}

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        return bytes == null ? default : JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await _cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _cache.RemoveAsync(key, ct);

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory,
        TimeSpan ttl, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached != null) return cached;
        var value = await factory();
        await SetAsync(key, value, ttl, ct);
        return value;
    }
}

// CacheKeys.cs — centralized key patterns, no magic strings scattered around
public static class CacheKeys
{
    public static string UserAccess(Guid userId, Guid moduleId)
        => $"user_access:{userId}:{moduleId}";

    public static string StockQuote(string symbol)
        => $"stock:{symbol.ToUpper()}";

    public static string MarketSummary()
        => "market:summary";

    public static string MarketIndices()
        => "market:indices";

    public static string TopGainers()
        => "market:gainers";

    public static string TopLosers()
        => "market:losers";

    public static string SubscriptionPlans(string moduleSlug)
        => $"plans:{moduleSlug}";
}
```

## Session — ISessionStore.cs + RedisSessionStore.cs
```csharp
public interface ISessionStore
{
    Task SetAsync(string token, SessionData data, TimeSpan ttl, CancellationToken ct = default);
    Task<SessionData?> GetAsync(string token, CancellationToken ct = default);
    Task RemoveAsync(string token, CancellationToken ct = default);
    Task RemoveAllForUserAsync(Guid userId, CancellationToken ct = default);
}

public record SessionData(
    Guid UserId,
    string ActorType,
    string Role,
    Guid? DeviceId,
    string SessionType,   // "mobile" | "web"
    DateTime ExpiresAt);

public class RedisSessionStore : ISessionStore
{
    private readonly IDatabase _db;
    private const string UserSessionsSetPrefix = "user_sessions:";

    public RedisSessionStore(IConnectionMultiplexer redis)
        => _db = redis.GetDatabase();

    public async Task SetAsync(string token, SessionData data,
        TimeSpan ttl, CancellationToken ct = default)
    {
        var key = SessionKey(token);
        var json = JsonSerializer.Serialize(data);
        await _db.StringSetAsync(key, json, ttl);

        // Track all session keys per user for bulk revocation
        await _db.SetAddAsync(UserSetKey(data.UserId), key);
        // Set expiry on the set itself (rolling — approximate)
        await _db.KeyExpireAsync(UserSetKey(data.UserId), TimeSpan.FromDays(35));
    }

    public async Task<SessionData?> GetAsync(string token, CancellationToken ct = default)
    {
        var val = await _db.StringGetAsync(SessionKey(token));
        return val.IsNullOrEmpty ? null : JsonSerializer.Deserialize<SessionData>(val!);
    }

    public async Task RemoveAsync(string token, CancellationToken ct = default)
    {
        var data = await GetAsync(token, ct);
        await _db.KeyDeleteAsync(SessionKey(token));
        if (data != null)
            await _db.SetRemoveAsync(UserSetKey(data.UserId), SessionKey(token));
    }

    public async Task RemoveAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var members = await _db.SetMembersAsync(UserSetKey(userId));
        if (members.Length == 0) return;
        var keys = members.Select(m => (RedisKey)(string)m!).ToArray();
        await _db.KeyDeleteAsync(keys);
        await _db.KeyDeleteAsync(UserSetKey(userId));
    }

    private static string SessionKey(string token) => $"session:{token}";
    private static string UserSetKey(Guid userId) => $"{UserSessionsSetPrefix}{userId}";
}
```

## MongoDB — Documents

Map the Mongoose schemas exactly. Do not simplify.

```csharp
// NotificationDocument.cs
[BsonCollection("notifications")]
public class NotificationDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("recipientId")]
    public string RecipientId { get; set; } = string.Empty;

    [BsonElement("externalRef")]
    public string? ExternalRef { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("body")]
    public string Body { get; set; } = string.Empty;

    [BsonElement("image")]
    public string? Image { get; set; }

    [BsonElement("data")]
    public BsonDocument? Data { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "transactional";

    [BsonElement("priority")]
    public string Priority { get; set; } = "normal";

    [BsonElement("surfaces")]
    public List<string> Surfaces { get; set; } = ["web_in_app"];

    [BsonElement("statusBySurface")]
    public Dictionary<string, SurfaceStatusDocument> StatusBySurface { get; set; } = [];

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } =
        DateTime.UtcNow.AddDays(15);  // DEFAULT_NOTIFICATION_RETENTION_DAYS = 15

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SurfaceStatusDocument
{
    [BsonElement("state")]
    public string State { get; set; } = "pending";

    [BsonElement("deliveredAt")]
    public DateTime? DeliveredAt { get; set; }

    [BsonElement("seenAt")]
    public DateTime? SeenAt { get; set; }

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }

    [BsonElement("archivedAt")]
    public DateTime? ArchivedAt { get; set; }

    [BsonElement("providerMetadata")]
    public BsonDocument? ProviderMetadata { get; set; }
}

// NotificationSettingDocument.cs
[BsonCollection("notification_settings")]
public class NotificationSettingDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("notificationType")]
    public string NotificationType { get; set; } = string.Empty;

    [BsonElement("surface")]
    public string Surface { get; set; } = string.Empty;

    [BsonElement("enabled")]
    public bool Enabled { get; set; } = true;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// AuditLogDocument.cs
[BsonCollection("audit_logs")]
public class AuditLogDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    // Who
    [BsonElement("userId")] public string? UserId { get; set; }
    [BsonElement("userName")] public string? UserName { get; set; }
    [BsonElement("userRole")] public string? UserRole { get; set; }

    // What
    [BsonElement("action")] public string Action { get; set; } = string.Empty;
    [BsonElement("module")] public string Module { get; set; } = string.Empty;
    [BsonElement("status")] public string Status { get; set; } = "success";

    // On what
    [BsonElement("resource")] public AuditResourceDocument? Resource { get; set; }

    // Changes (structured array, not a blob)
    [BsonElement("changes")] public List<AuditChangeDocument>? Changes { get; set; }

    [BsonElement("reason")] public string? Reason { get; set; }
    [BsonElement("metadata")] public BsonDocument? Metadata { get; set; }

    // Request context
    [BsonElement("request")] public AuditRequestDocument? Request { get; set; }

    // Error context (only when status = "failure")
    [BsonElement("error")] public AuditErrorDocument? Error { get; set; }

    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditResourceDocument
{
    [BsonElement("id")] public string? Id { get; set; }
    [BsonElement("type")] public string? Type { get; set; }
}

public class AuditChangeDocument
{
    [BsonElement("field")] public string Field { get; set; } = string.Empty;
    [BsonElement("from")] public BsonValue? From { get; set; }
    [BsonElement("to")] public BsonValue? To { get; set; }
}

public class AuditRequestDocument
{
    [BsonElement("ip")] public string? Ip { get; set; }
    [BsonElement("userAgent")] public string? UserAgent { get; set; }
    [BsonElement("endpoint")] public string? Endpoint { get; set; }
    [BsonElement("method")] public string? Method { get; set; }
    [BsonElement("statusCode")] public int? StatusCode { get; set; }
    [BsonElement("duration")] public long? DurationMs { get; set; }
}

public class AuditErrorDocument
{
    [BsonElement("code")] public string? Code { get; set; }
    [BsonElement("message")] public string? Message { get; set; }
    [BsonElement("stack")] public string? Stack { get; set; }
}

// BsonCollectionAttribute.cs
[AttributeUsage(AttributeTargets.Class)]
public class BsonCollectionAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
```

## MongoDB — IMongoRepository + MongoRepository
```csharp
public interface IMongoRepository<T>
{
    Task InsertAsync(T document, CancellationToken ct = default);
    Task<T?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<List<T>> FindAsync(FilterDefinition<T> filter,
        SortDefinition<T>? sort = null, int skip = 0, int limit = 50,
        CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<T> update, CancellationToken ct = default);
    Task UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update,
        CancellationToken ct = default);
    Task<long> CountAsync(FilterDefinition<T> filter, CancellationToken ct = default);
    IMongoCollection<T> Collection { get; }
}

public class MongoRepository<T> : IMongoRepository<T>
{
    public IMongoCollection<T> Collection { get; }

    public MongoRepository(IMongoDatabase db)
    {
        var name = typeof(T).GetCustomAttribute<BsonCollectionAttribute>()?.Name
            ?? typeof(T).Name.ToLower();
        Collection = db.GetCollection<T>(name);
    }

    public Task InsertAsync(T doc, CancellationToken ct = default)
        => Collection.InsertOneAsync(doc, cancellationToken: ct);

    public async Task<T?> FindByIdAsync(string id, CancellationToken ct = default)
        => await Collection.Find(Builders<T>.Filter.Eq("_id", id))
            .FirstOrDefaultAsync(ct);

    public async Task<List<T>> FindAsync(FilterDefinition<T> filter,
        SortDefinition<T>? sort = null, int skip = 0, int limit = 50,
        CancellationToken ct = default)
    {
        var query = Collection.Find(filter).Skip(skip).Limit(limit);
        if (sort != null) query = query.Sort(sort);
        return await query.ToListAsync(ct);
    }

    public Task UpdateAsync(string id, UpdateDefinition<T> update, CancellationToken ct = default)
        => Collection.UpdateOneAsync(Builders<T>.Filter.Eq("_id", id), update,
            cancellationToken: ct);

    public Task UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update,
        CancellationToken ct = default)
        => Collection.UpdateManyAsync(filter, update, cancellationToken: ct);

    public Task<long> CountAsync(FilterDefinition<T> filter, CancellationToken ct = default)
        => Collection.CountDocumentsAsync(filter, cancellationToken: ct);
}
```

## MongoDB — MongoIndexInitializer.cs
```csharp
public static class MongoIndexInitializer
{
    public static async Task EnsureIndexesAsync(IMongoDatabase db)
    {
        // notifications — replicate Mongoose indexes exactly
        var notif = db.GetCollection<NotificationDocument>("notifications");
        await notif.Indexes.CreateManyAsync([
            new(Builders<NotificationDocument>.IndexKeys
                .Ascending(x => x.RecipientId).Descending(x => x.CreatedAt)),
            new(Builders<NotificationDocument>.IndexKeys
                .Ascending(x => x.RecipientId)
                .Ascending("surfaces")
                .Descending(x => x.CreatedAt)),
            new(Builders<NotificationDocument>.IndexKeys
                .Ascending(x => x.RecipientId)
                .Ascending("statusBySurface.web_in_app.readAt")
                .Descending(x => x.CreatedAt),
                new() { Sparse = true }),
            new(Builders<NotificationDocument>.IndexKeys
                .Ascending(x => x.RecipientId)
                .Ascending("statusBySurface.mobile_in_app.readAt")
                .Descending(x => x.CreatedAt),
                new() { Sparse = true }),
            new(Builders<NotificationDocument>.IndexKeys.Ascending(x => x.ExternalRef),
                new() { Unique = true, Sparse = true }),
            new(Builders<NotificationDocument>.IndexKeys.Ascending(x => x.ExpiresAt),
                new() { ExpireAfter = TimeSpan.Zero }),  // TTL index
        ]);

        // notification_settings
        var settings = db.GetCollection<NotificationSettingDocument>("notification_settings");
        await settings.Indexes.CreateOneAsync(
            new(Builders<NotificationSettingDocument>.IndexKeys
                .Ascending(x => x.UserId)
                .Ascending(x => x.NotificationType)
                .Ascending(x => x.Surface),
                new() { Unique = true }));

        // audit_logs — replicate Mongoose indexes
        var audit = db.GetCollection<AuditLogDocument>("audit_logs");
        await audit.Indexes.CreateManyAsync([
            new(Builders<AuditLogDocument>.IndexKeys
                .Ascending(x => x.Module).Descending(x => x.CreatedAt)),
            new(Builders<AuditLogDocument>.IndexKeys
                .Ascending(x => x.UserId).Descending(x => x.CreatedAt)),
            new(Builders<AuditLogDocument>.IndexKeys
                .Ascending(x => x.Module).Ascending(x => x.Action).Descending(x => x.CreatedAt)),
            new(Builders<AuditLogDocument>.IndexKeys
                .Ascending(x => x.Module).Ascending(x => x.Status).Descending(x => x.CreatedAt)),
            new(Builders<AuditLogDocument>.IndexKeys
                .Ascending(x => x.Module).Ascending(x => x.UserId).Descending(x => x.CreatedAt)),
            new(Builders<AuditLogDocument>.IndexKeys
                .Ascending("resource.id").Ascending("resource.type")),
            new(Builders<AuditLogDocument>.IndexKeys.Ascending(x => x.CreatedAt),
                new() { ExpireAfter = TimeSpan.FromDays(90) }),  // 90-day TTL
        ]);
    }
}
```

## MongoDB — AuditLogger.cs
```csharp
public class AuditLogger : IAuditLogger
{
    private readonly IMongoRepository<AuditLogDocument> _repo;

    public AuditLogger(IMongoRepository<AuditLogDocument> repo) => _repo = repo;

    public Task LogAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        var doc = new AuditLogDocument
        {
            UserId = entry.UserId,
            UserName = entry.UserName,
            UserRole = entry.UserRole,
            Action = entry.Action,
            Module = entry.Module,
            Status = entry.Status,
            Resource = entry.ResourceId != null ? new AuditResourceDocument
            {
                Id = entry.ResourceId,
                Type = entry.ResourceType,
            } : null,
            Changes = entry.Changes?.Select(c => new AuditChangeDocument
            {
                Field = c.Field,
                From = c.From != null ? BsonValue.Create(c.From) : BsonNull.Value,
                To = c.To != null ? BsonValue.Create(c.To) : BsonNull.Value,
            }).ToList(),
            Reason = entry.Reason,
            Metadata = entry.Metadata != null
                ? BsonDocument.Parse(JsonSerializer.Serialize(entry.Metadata)) : null,
            Request = entry.Request != null ? new AuditRequestDocument
            {
                Ip = entry.Request.Ip,
                UserAgent = entry.Request.UserAgent,
                Endpoint = entry.Request.Endpoint,
                Method = entry.Request.Method,
                StatusCode = entry.Request.StatusCode,
                DurationMs = entry.Request.DurationMs,
            } : null,
            Error = entry.Error != null ? new AuditErrorDocument
            {
                Code = entry.Error.Code,
                Message = entry.Error.Message,
                Stack = entry.Error.Stack,
            } : null,
        };
        return _repo.InsertAsync(doc, ct);
    }
}
```

## MongoDB — NotificationService.cs
```csharp
public class NotificationService : INotificationService
{
    private readonly IMongoRepository<NotificationDocument> _repo;
    private readonly IFirebaseMessagingService _fcm;
    private readonly IMongoRepository<NotificationSettingDocument> _settings;

    public NotificationService(
        IMongoRepository<NotificationDocument> repo,
        IFirebaseMessagingService fcm,
        IMongoRepository<NotificationSettingDocument> settings)
    {
        _repo = repo;
        _fcm = fcm;
        _settings = settings;
    }

    public async Task SendAsync(SendNotificationRequest request, CancellationToken ct = default)
    {
        // Check notification settings — skip surfaces where user has opted out
        var enabledSurfaces = await FilterByUserSettingsAsync(
            request.RecipientId, request.Type, request.Surfaces, ct);

        if (enabledSurfaces.Count == 0) return;

        // Build surface status map — all start as "pending"
        var statusBySurface = enabledSurfaces.ToDictionary(
            s => SurfaceToString(s),
            _ => new SurfaceStatusDocument { State = "pending" });

        var doc = new NotificationDocument
        {
            RecipientId = request.RecipientId,
            ExternalRef = request.ExternalRef,
            Title = request.Title,
            Body = request.Body,
            Image = request.Image,
            Data = request.Data != null
                ? BsonDocument.Parse(JsonSerializer.Serialize(request.Data)) : null,
            Type = request.Type.ToString().ToLower(),
            Priority = request.Priority.ToString().ToLower(),
            Surfaces = enabledSurfaces.Select(SurfaceToString).ToList(),
            StatusBySurface = statusBySurface,
        };

        await _repo.InsertAsync(doc, ct);

        // Dispatch push surfaces asynchronously
        foreach (var surface in enabledSurfaces)
        {
            if (surface == NotificationSurface.MobilePush || surface == NotificationSurface.WebPush)
            {
                // Fire and forget — push failures should not block the response
                _ = Task.Run(() => _fcm.SendAsync(new FcmMessage
                {
                    RecipientId = request.RecipientId,
                    Title = request.Title,
                    Body = request.Body,
                    Data = request.Data?.ToDictionary(
                        kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? ""),
                    NotificationId = doc.Id,
                }, ct), ct);
            }
        }
    }

    public async Task<List<NotificationInboxItem>> GetInAppAsync(
        string recipientId, NotificationSurface surface,
        int limit = 20, string? afterId = null, CancellationToken ct = default)
    {
        var surfaceStr = SurfaceToString(surface);
        var filter = Builders<NotificationDocument>.Filter.And(
            Builders<NotificationDocument>.Filter.Eq(x => x.RecipientId, recipientId),
            Builders<NotificationDocument>.Filter.AnyEq("surfaces", surfaceStr));

        if (afterId != null)
            filter &= Builders<NotificationDocument>.Filter
                .Lt("_id", new ObjectId(afterId));

        var docs = await _repo.FindAsync(filter,
            sort: Builders<NotificationDocument>.Sort.Descending(x => x.CreatedAt),
            limit: limit, ct: ct);

        return docs.Select(x => new NotificationInboxItem
        {
            Id = x.Id,
            RecipientId = x.RecipientId,
            Title = x.Title,
            Body = x.Body,
            Image = x.Image,
            Data = x.Data,
            CreatedAtUtc = x.CreatedAt,
            Surface = surface,
            State = ParseSurfaceState(x.StatusBySurface[surfaceStr].State),
        }).ToList();
    }

    public async Task UpdateSurfaceStateAsync(
        string notificationId, NotificationSurface surface,
        NotificationSurfaceState newState, CancellationToken ct = default)
    {
        var surfaceStr = SurfaceToString(surface);
        var stateStr = StateToString(newState);
        var field = $"statusBySurface.{surfaceStr}";

        var update = Builders<NotificationDocument>.Update
            .Set($"{field}.state", stateStr);

        update = newState switch
        {
            NotificationSurfaceState.Delivered =>
                update.Set($"{field}.deliveredAt", DateTime.UtcNow),
            NotificationSurfaceState.Seen =>
                update.Set($"{field}.seenAt", DateTime.UtcNow),
            NotificationSurfaceState.Read =>
                update.Set($"{field}.readAt", DateTime.UtcNow),
            NotificationSurfaceState.Archived =>
                update.Set($"{field}.archivedAt", DateTime.UtcNow),
            _ => update,
        };

        update = update.Set("updatedAt", DateTime.UtcNow);
        await _repo.UpdateAsync(notificationId, update, ct);
    }

    public Task MarkAllReadAsync(string recipientId, NotificationSurface surface,
        CancellationToken ct = default)
    {
        var surfaceStr = SurfaceToString(surface);
        var filter = Builders<NotificationDocument>.Filter.And(
            Builders<NotificationDocument>.Filter.Eq(x => x.RecipientId, recipientId),
            Builders<NotificationDocument>.Filter.AnyEq("surfaces", surfaceStr),
            Builders<NotificationDocument>.Filter.Not(
                Builders<NotificationDocument>.Filter
                    .Exists($"statusBySurface.{surfaceStr}.readAt")));

        var update = Builders<NotificationDocument>.Update
            .Set($"statusBySurface.{surfaceStr}.state", "read")
            .Set($"statusBySurface.{surfaceStr}.readAt", DateTime.UtcNow)
            .Set("updatedAt", DateTime.UtcNow);

        return _repo.UpdateManyAsync(filter, update, ct);
    }

    private async Task<List<NotificationSurface>> FilterByUserSettingsAsync(
        string recipientId, NotificationType type,
        List<NotificationSurface> requested, CancellationToken ct)
    {
        var typeStr = type.ToString().ToLower();
        var filter = Builders<NotificationSettingDocument>.Filter.And(
            Builders<NotificationSettingDocument>.Filter.Eq(x => x.UserId, recipientId),
            Builders<NotificationSettingDocument>.Filter.Eq(x => x.NotificationType, typeStr),
            Builders<NotificationSettingDocument>.Filter.Eq(x => x.Enabled, false));

        var optedOut = await _settings.FindAsync(filter, ct: ct);
        var optedOutSurfaces = optedOut.Select(s => s.Surface).ToHashSet();

        return requested
            .Where(s => !optedOutSurfaces.Contains(SurfaceToString(s)))
            .ToList();
    }

    private static string SurfaceToString(NotificationSurface s) => s switch
    {
        NotificationSurface.MobileInApp => "mobile_in_app",
        NotificationSurface.WebInApp => "web_in_app",
        NotificationSurface.MobilePush => "mobile_push",
        NotificationSurface.WebPush => "web_push",
        NotificationSurface.Email => "email",
        NotificationSurface.Sms => "sms",
        _ => s.ToString().ToSnakeCase(),
    };

    private static string StateToString(NotificationSurfaceState s) => s switch
    {
        NotificationSurfaceState.Pending => "pending",
        NotificationSurfaceState.Delivered => "delivered",
        NotificationSurfaceState.Seen => "seen",
        NotificationSurfaceState.Read => "read",
        NotificationSurfaceState.Archived => "archived",
        NotificationSurfaceState.Deleted => "deleted",
        _ => s.ToString().ToLower(),
    };

    private static NotificationSurfaceState ParseSurfaceState(string state) => state switch
    {
        "pending" => NotificationSurfaceState.Pending,
        "delivered" => NotificationSurfaceState.Delivered,
        "seen" => NotificationSurfaceState.Seen,
        "read" => NotificationSurfaceState.Read,
        "archived" => NotificationSurfaceState.Archived,
        "deleted" => NotificationSurfaceState.Deleted,
        _ => NotificationSurfaceState.Pending,
    };
}
```

## Firebase — IFirebaseMessagingService.cs + FirebaseMessagingService.cs
```csharp
public interface IFirebaseMessagingService
{
    Task<string?> SendAsync(FcmMessage message, CancellationToken ct = default);
    Task<List<string>> SendMulticastAsync(List<string> tokens, FcmMessage message,
        CancellationToken ct = default);
}

public record FcmMessage(
    string RecipientId,
    string Title,
    string Body,
    Dictionary<string, string>? Data = null,
    string? NotificationId = null,
    string? ImageUrl = null);

public class FirebaseConfig
{
    public string CredentialFilePath { get; set; } = "firebase-service-account.json";
}

// FirebaseMessagingService.cs
// Uses Firebase Admin SDK (FirebaseAdmin NuGet package)
// Requires a valid firebase-service-account.json at startup
public class FirebaseMessagingService : IFirebaseMessagingService
{
    private readonly ILogger<FirebaseMessagingService> _logger;
    private readonly AppDbContext _db; // to look up FCM tokens from user_device table

    public FirebaseMessagingService(
        ILogger<FirebaseMessagingService> logger,
        AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<string?> SendAsync(FcmMessage message, CancellationToken ct = default)
    {
        // Look up FCM tokens for this recipient from user_device table
        var tokens = await _db.Set<UserDevice>()
            .Where(d => d.CustomerId.ToString() == message.RecipientId
                && d.IsActive && d.FcmToken != null)
            .Select(d => d.FcmToken!)
            .ToListAsync(ct);

        if (tokens.Count == 0) return null;

        try
        {
            var result = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(
                new MulticastMessage
                {
                    Tokens = tokens,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = message.Title,
                        Body = message.Body,
                        ImageUrl = message.ImageUrl,
                    },
                    Data = message.Data ?? [],
                    Android = new AndroidConfig
                    {
                        Priority = AndroidMessagePriority.High,
                    },
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            ["apns-priority"] = "10",
                        },
                    },
                }, ct);

            _logger.LogInformation(
                "FCM sent to {Recipient}: {Success} success, {Failure} failure",
                message.RecipientId, result.SuccessCount, result.FailureCount);

            return result.SuccessCount > 0 ? "sent" : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM failed for recipient {RecipientId}", message.RecipientId);
            return null;
        }
    }

    public async Task<List<string>> SendMulticastAsync(List<string> tokens,
        FcmMessage message, CancellationToken ct = default)
    {
        if (tokens.Count == 0) return [];

        var result = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(
            new MulticastMessage
            {
                Tokens = tokens,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = message.Title,
                    Body = message.Body,
                },
                Data = message.Data ?? [],
            }, ct);

        // Return list of failed tokens for cleanup
        var failed = new List<string>();
        for (var i = 0; i < result.Responses.Count; i++)
        {
            if (!result.Responses[i].IsSuccess)
                failed.Add(tokens[i]);
        }
        return failed;
    }
}
```

## MediatR Behaviours
```csharp
// ValidationBehaviour.cs
public class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0) return await next();

        var errors = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // Return Result.Failure without throwing — maintains the no-exception rule
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var method = typeof(TResponse).GetMethod("Failure", [typeof(string)]);
            if (method != null)
                return (TResponse)method.Invoke(null, [errors])!;
        }

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(errors);

        throw new ValidationException(failures);
    }
}

// LoggingBehaviour.cs
public class LoggingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
            _logger.LogWarning("Slow handler {Request} took {Ms}ms", name, sw.ElapsedMilliseconds);
        else
            _logger.LogDebug("Handled {Request} in {Ms}ms", name, sw.ElapsedMilliseconds);

        return response;
    }
}
```

## GlobalExceptionMiddleware.cs
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(ctx, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        var (status, message) = ex switch
        {
            KeyNotFoundException => (404, ex.Message),
            UnauthorizedAccessException => (401, "Unauthorized"),
            _ => (500, "An unexpected error occurred"),
        };

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsJsonAsync(ApiResponse<object?>.Ok(message));
    }
}
```

## AppControllerBase.cs
```csharp
[ApiController]
public abstract class AppControllerBase : ControllerBase
{
    protected IActionResult OkResult<T>(Result<T> result, string message, int status = 200)
    {
        if (result.IsFailure)
            return BadRequest(ApiResponse<object?>.Ok(result.Error!));
        var response = ApiResponse<T>.Ok(message, result.Value);
        return status == 201 ? StatusCode(201, response) : Ok(response);
    }

    protected IActionResult CreatedResult<T>(Result<T> result, string message)
        => OkResult(result, message, 201);

    protected IActionResult OkMessage(string message)
        => Ok(ApiResponse.Ok(message));

    protected IActionResult CursoredResult<T>(
        Result<CursorResult<T>> result, string message)
    {
        if (result.IsFailure)
            return BadRequest(ApiResponse<object?>.Ok(result.Error!));
        return Ok(ApiResponse<List<T>>.Cursored(
            message,
            result.Value!.Items.ToList(),
            result.Value.NextCursor,
            result.Value.PrevCursor));
    }
}
```

## DependencyInjection.cs
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // ── PostgreSQL ────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        // ── Redis ─────────────────────────────────────────────────
        var redisConnection = config.GetConnectionString("Redis")!;
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = redisConnection);
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ISessionStore, RedisSessionStore>();

        // ── MongoDB ───────────────────────────────────────────────
        var mongoConn = config["MongoDB:ConnectionString"]!;
        var mongoDb = config["MongoDB:Database"]!;
        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConn));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDb));
        services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

        // ── Firebase ──────────────────────────────────────────────
        var credPath = config["Firebase:CredentialFilePath"]!;
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(credPath),
        });
        services.AddScoped<IFirebaseMessagingService, FirebaseMessagingService>();

        // ── MediatR ───────────────────────────────────────────────
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // ── Pipeline behaviours (order: logging wraps validation) ─
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        // ── FluentValidation ──────────────────────────────────────
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ── Shared services ───────────────────────────────────────
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPasswordService, PasswordService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtConfig>(config.GetSection("Jwt"));

        var jwt = config.GetSection("Jwt").Get<JwtConfig>()!;
        var key = Encoding.UTF8.GetBytes(jwt.Secret);

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,  ValidIssuer = jwt.Issuer,
                ValidateAudience = true, ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
            // SignalR: JWT comes via query string for WebSocket connections
            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) &&
                        ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        ctx.Token = token;
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(o =>
        {
            o.AddPolicy(Policies.AdminOnly, p =>
                p.RequireAuthenticatedUser()
                 .RequireClaim(Constants.ClaimTypes.ActorType, Constants.ActorTypes.Admin));
            o.AddPolicy(Policies.CustomerOnly, p =>
                p.RequireAuthenticatedUser()
                 .RequireClaim(Constants.ClaimTypes.ActorType, Constants.ActorTypes.Customer));
            o.AddPolicy(Policies.AnyActor, p => p.RequireAuthenticatedUser());
        });

        services.AddScoped<IJwtService, JwtService>();

        return services;
    }

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Trading Analytics API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization", Type = SecuritySchemeType.Http,
                Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
            });
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
}

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```

## Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure OTP hasher at startup — must happen before any handler runs
OtpHasher.Configure(
    builder.Configuration["Auth:OtpHmacSecret"]
    ?? throw new InvalidOperationException("Auth:OtpHmacSecret not configured"));

builder.Services
    .AddSharedInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddSwaggerWithJwt();

builder.Services.AddControllers();

// SignalR with Redis backplane (required for multi-instance deployments)
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis")!);

// Hangfire — Postgres-backed job queue (BullMQ equivalent for .NET)
builder.Services.AddHangfire(c =>
    c.UsePostgreSqlStorage(o =>
        o.UseNpgsqlConnection(
            builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer(o => o.WorkerCount = 5);

builder.Services.AddCors(o =>
    o.AddPolicy("Frontend", p =>
        p.WithOrigins(builder.Configuration["AllowedOrigins"]!.Split(","))
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

// Module registrations — uncomment as each phase is completed:
// builder.Services.AddIdentityModule(builder.Configuration);
// builder.Services.AddMarketModule(builder.Configuration);
// builder.Services.AddPortfolioModule(builder.Configuration);
// builder.Services.AddSubscriptionsModule(builder.Configuration);
// builder.Services.AddCoursesModule(builder.Configuration);
// builder.Services.AddContentModule(builder.Configuration);

var app = builder.Build();

// Ensure MongoDB indexes on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    await MongoIndexInitializer.EnsureIndexesAsync(db);
}

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard (admin-only in production — add auth filter before deploying)
app.UseHangfireDashboard("/hangfire");

app.MapControllers();

// Hub endpoints added as phases are completed:
// app.MapHub<MarketHub>("/hubs/market");
// app.MapHub<ChatHub>("/hubs/chat");

app.Run();
```

---

# PHASE 3 — Identity Module

Implement `src/Modules/Identity/` completely.
Add Identity DbSets to AppDbContext.
Uncomment `AddIdentityModule` in Program.cs.

## Entities

### Customer
```
Table: "customers"
Properties: Id, Name, Email (nullable), EmailVerified (default false),
            Phone (nullable), PhoneVerified (default false),
            Image (nullable), RoleId (int? FK to roles table — do not create roles entity,
            just store the int), Banned (default false), BanReason (nullable)

No IsDeleted/soft delete on customers — they are banned, not deleted.
Ignore BaseEntity.IsDeleted and BaseEntity.DeletedAt in EF config for Customer.

Factory: Customer.Create(name, email, phone) → Result<Customer>
  - name required
  - email OR phone required (or both)
  - raises CustomerRegisteredEvent

Methods:
  MarkEmailVerified()
  MarkPhoneVerified()
  UpdateProfile(name, image)
  Ban(reason) → Result       raises CustomerBannedEvent
  Unban() → Result
```

### AdminUser
```
Table: "admin_users"
Same columns as Customer.
No IsDeleted on AdminUser either.

Factory: AdminUser.Create(name, email, roleId?) → Result<AdminUser>
Methods: MarkEmailVerified(), AssignRole(roleId)
```

### Account
```
Table: "account"
Properties: Id, AccountId (string — OAuth sub or email),
            ProviderId (string — "credential"/"google"/etc),
            ActorType (enum: Customer|Admin — stored as "customer"/"admin"),
            CustomerId (Guid? — nullable), AdminId (Guid? — nullable),
            AccessToken, RefreshToken, IdToken,
            AccessTokenExpiresAt, RefreshTokenExpiresAt,
            Scope, Password (Argon2 hash string, null for OAuth accounts)

Domain rule: exactly one of CustomerId or AdminId must be non-null.
Enforce in factory methods. Do not add a DB constraint — rely on domain.

No IsDeleted on Account.

Factories:
  Account.CreateCredentialForCustomer(customerId, email, passwordHash)
  Account.CreateCredentialForAdmin(adminId, email, passwordHash)
  Account.CreateOAuthForCustomer(customerId, providerId, oauthSub)

Methods:
  UpdatePassword(newHash)
  UpdateTokens(access, refresh, idToken, accessExpiry, refreshExpiry)
  VerifyPassword(IPasswordService, plaintext) → bool
```

### CustomerSession
```
Table: "customer_sessions"
Properties: Id, CustomerId, Token (random 64-char base64url string),
            ExpiresAt, Type (enum SessionType — "mobile"/"web"),
            UserDeviceId (Guid? nullable), IpAddress (nullable), UserAgent (nullable)

No UpdatedAt, no IsDeleted on sessions.
IsExpired computed: DateTime.UtcNow >= ExpiresAt

Factory: CustomerSession.Create(customerId, type, deviceId?, ip?, userAgent?)
  → returns (CustomerSession session, string rawToken)
  Web sessions: 7 days. Mobile sessions: 30 days.

NOTE: Sessions are stored in Redis via ISessionStore for fast access checks.
The Postgres CustomerSession table is kept as a durable record for:
  - Audit/history
  - Admin "view active sessions" screens
  - Recovery if Redis is flushed
Both Redis and Postgres are written on session creation.
On logout: delete from both.
On auth check: read from Redis first, fall back to Postgres.
```

### AdminSession
```
Table: "admin_sessions"
Properties: Id, AdminId, Token, ExpiresAt, Type, IpAddress, UserAgent
Expires in 24 hours.
Same dual-store pattern as CustomerSession.
Factory: AdminSession.Create(adminId, type, ip?, userAgent?) → (AdminSession, rawToken)
```

### UserDevice
```
Table: "user_device"
Properties: Id, CustomerId, DeviceId (from mobile app), FcmToken (nullable),
            DeviceType ("android"/"ios"/"web"), DeviceName (nullable),
            IsActive (default true), LastActiveAt

Factory: UserDevice.Register(customerId, deviceId, deviceType, deviceName?, fcmToken?)
Methods: UpdateFcmToken(token?), RecordActivity(), Deactivate()
```

### Verification
```
Table: "verification"
Properties: Id, Identifier (who requested — userId or email/phone),
            Target (where sent — email address or phone number),
            Purpose (enum stored as snake_case string),
            Channel (enum: Email="email" | Sms="sms"),
            TokenHash (SHA-256 hex of magic-link token),
            OtpHash (HMAC-SHA256 via OtpHasher, null for email-only flows),
            ExpiresAt, ConsumedAt (nullable),
            Attempts (default 0), MaxAttempts (default 5)

No IsDeleted on Verification.

Computed: IsExpired, IsConsumed, IsExhausted, IsUsable

Factory: Verification.Create(identifier, target, purpose, channel, expiryMinutes=10)
  → returns (Verification, string rawToken, string? rawOtp)
  Always generates token (SHA-256 hash stored).
  Generates rawOtp (6-digit) only for Sms channel or phone purposes.
  rawOtp hashed via OtpHasher.Hash(rawOtp).

Methods:
  TryConsumeWithToken(rawToken) → Result
    Hash incoming token, compare to TokenHash
  TryConsumeWithOtp(rawOtp) → Result
    Increment Attempts first, then OtpHasher.Verify(rawOtp, OtpHash)
    Return Failure if exhausted/expired/consumed before checking OTP

VerificationPurpose enum (stored as snake_case string):
  EmailVerification, PasswordReset, PhoneVerification,
  PhoneLogin, PhoneRegistration
```

## Domain Events
```
CustomerRegisteredEvent(CustomerId, Email?, Phone?)
CustomerBannedEvent(CustomerId, Reason)
```

## Commands (full handlers required)

```
RegisterWithEmailCommand(Name, Email, Password)
  1. Check duplicate email → Failure if exists
  2. Customer.Create(name, email, null)
  3. hash = passwordService.Hash(password)
  4. Account.CreateCredentialForCustomer(customer.Id, email, hash)
  5. SaveChangesAsync (persists customer + account)
  6. Verification.Create(customer.Id.ToString(), email, EmailVerification, Email)
  7. notificationService.SendAsync (email surface, type=transactional)
  8. jwt = jwtService.GenerateCustomerToken(...)
  9. Return Result<AuthResponseDto> with jwt + customer profile

RegisterWithPhoneCommand(Name, Phone)
  1. Check duplicate phone
  2. Customer.Create(name, null, phone)
  3. SaveChangesAsync
  4. Verification.Create(customer.Id.ToString(), phone, PhoneRegistration, Sms)
  5. notificationService.SendAsync (sms surface) with rawOtp
  6. Return verificationId only (no JWT yet)

VerifyPhoneRegistrationCommand(CustomerId, VerificationId, RawOtp, Password)
  1. Load Verification, TryConsumeWithOtp(rawOtp) → Failure if invalid
  2. Load Customer, customer.MarkPhoneVerified()
  3. Account.CreateCredentialForCustomer(customer.Id, phone, hash(password))
  4. SaveChangesAsync
  5. Return JWT

LoginWithEmailCommand(Email, Password, SessionType, DeviceId?, IpAddress?, UserAgent?)
  1. Find Account (providerId="credential", accountId=email, actorType=customer)
  2. account.VerifyPassword(passwordService, password) → Failure("Invalid credentials")
  3. Load Customer → Failure("Account is banned") if banned
  4. If SessionType=Mobile: sessionStore.RemoveAllForUserAsync + delete mobile sessions from DB
  5. (session, rawToken) = CustomerSession.Create(...)
  6. Write session to Postgres AND Redis (sessionStore.SetAsync)
  7. Return (jwt, rawToken, CustomerProfileDto)

LoginWithPhoneCommand(Phone)
  1. Find Customer by phone → Failure if not found
  2. Verification.Create(customer.Id.ToString(), phone, PhoneLogin, Sms)
  3. Send OTP via notificationService
  4. Return verificationId

VerifyOtpLoginCommand(VerificationId, RawOtp, SessionType, DeviceId?, IpAddress?, UserAgent?)
  1. Load Verification, TryConsumeWithOtp
  2. Load Customer by identifier (parsed from Verification.Identifier)
  3. Create session (same as LoginWithEmail step 5-7)
  4. Return (jwt, rawToken, CustomerProfileDto)

AdminLoginCommand(Email, Password, IpAddress?, UserAgent?)
  1. Find Account (credential, email, actorType=admin)
  2. VerifyPassword → Failure if wrong
  3. Load AdminUser → Failure if banned
  4. (session, rawToken) = AdminSession.Create(...)
  5. Write to Postgres + Redis
  6. Return (jwt, rawToken)

LogoutCommand(RawToken)
  1. Find CustomerSession by token in DB, delete
  2. sessionStore.RemoveAsync(rawToken)

LogoutAllCommand()
  1. Delete all CustomerSessions for current user from DB
  2. sessionStore.RemoveAllForUserAsync(userId)

RefreshTokenCommand(RawToken)
  1. Check Redis first: sessionStore.GetAsync(rawToken)
  2. If not in Redis, check Postgres CustomerSession by token
  3. If expired or not found → Failure("Session expired")
  4. Load Customer, generate new JWT (session unchanged, just JWT refreshed)
  5. Return new JWT

SendPasswordResetCommand(Email)
  1. Find Account by email — do NOT reveal if not found
  2. If found: Verification.Create(account.CustomerId.ToString(), email, PasswordReset, Email)
  3. Send email with magic link containing rawToken
  4. Return Ok("If the email exists, a reset link has been sent")

ResetPasswordCommand(RawToken, NewPassword)
  1. SHA-256 hash rawToken, find Verification by tokenHash + purpose=PasswordReset
  2. TryConsumeWithToken(rawToken)
  3. Load Account, account.UpdatePassword(hash(NewPassword))
  4. Invalidate all sessions: LogoutAll for this user
  5. SaveChangesAsync

VerifyEmailCommand(RawToken)
  1. SHA-256 hash rawToken, find Verification by tokenHash + purpose=EmailVerification
  2. TryConsumeWithToken
  3. Load Customer, customer.MarkEmailVerified()
  4. SaveChangesAsync

RegisterDeviceCommand(DeviceId, DeviceType, DeviceName?, FcmToken?)
  Find UserDevice by customerId+deviceId:
    If exists: UpdateFcmToken, RecordActivity → SaveChanges
    If not: UserDevice.Register → Add → SaveChanges

AdminCreateCustomerCommand(Name, Email, Password) [AdminOnly]
  Same as RegisterWithEmail but skip verification,
  call customer.MarkEmailVerified() immediately, log audit

AdminBanCustomerCommand(CustomerId, Reason) [AdminOnly]
  1. Load Customer, customer.Ban(reason)
  2. LogoutAll for customer (delete sessions from DB + Redis)
  3. SaveChangesAsync
  4. auditLogger.LogAsync(...)
```

## Queries
```
GetCurrentCustomerQuery → CustomerProfileDto
GetMySessionsQuery → List<SessionDto> (from Postgres)
GetMyDevicesQuery → List<DeviceDto>
GetCustomerByIdQuery(id) [AdminOnly] → CustomerDetailDto
GetAllCustomersQuery(QueryParams) [AdminOnly] → CursorResult<CustomerSummaryDto>
```

## IdentityModule.cs
```csharp
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddValidatorsFromAssemblyContaining<IdentityModule>();
        return services;
    }
}
```

---

# PHASE 4 — Subscriptions + Payments Module

[Content identical to Phase 4 in the previous prompt version — entities, commands,
queries, gateways, jobs. Key differences from previous version:]

1. Payment table uses int (serial) PK — do not change to Guid
2. Cache UserAccess results in Redis via ICacheService:
   - Key: CacheKeys.UserAccess(userId, moduleId)
   - TTL: config["Cache:UserAccessTtlSeconds"] (default 300s = 5 minutes)
   - Invalidate on: subscription activated, cancelled, expired, revoked
3. Invalidate CacheKeys.SubscriptionPlans(moduleSlug) when tier prices are updated
4. Log audit events via IAuditLogger for all admin actions
   (AdminGrantAccess, AdminExtend, AdminBan equivalents)

---

# PHASE 5 — Market Module

[Content identical to previous version. Key additions:]

1. Cache market data in Redis:
   - StockQuote: CacheKeys.StockQuote(symbol), TTL = config["Cache:StockQuoteTtlSeconds"]
   - MarketSummary: CacheKeys.MarketSummary(), TTL = config["Cache:MarketDataTtlSeconds"]
   - TopGainers/Losers: CacheKeys.TopGainers(), TTL = same
   - Invalidate all on each successful poll cycle

2. IMarketPriceService implementation reads from Redis cache first,
   falls back to Postgres if cache miss

3. MarketModule.cs registers IMarketPriceService as scoped:
   services.AddScoped<IMarketPriceService, MarketPriceService>();

---

# PHASE 6 — Portfolio Module

[Identical to previous version. No changes.]

---

# PHASE 7 — Courses Module

[Identical to previous version. No changes.]

---

# PHASE 8 — Content Module

[Identical to previous version. Chat room access check queries user_access
directly — no import from Subscriptions module.]

---

# PHASE 9 — Final wiring

1. Uncomment all module registrations in Program.cs
2. Add hub mappings:
   ```csharp
   app.MapHub<MarketHub>("/hubs/market");
   app.MapHub<ChatHub>("/hubs/chat");
   ```
3. Register Hangfire recurring jobs:
   ```csharp
   RecurringJob.AddOrUpdate<ExpireSubscriptionsJob>(
       "expire-subs", j => j.ExecuteAsync(CancellationToken.None), "0 * * * *");
   RecurringJob.AddOrUpdate<ExpiryReminderJob>(
       "expiry-reminders", j => j.ExecuteAsync(CancellationToken.None), "0 9 * * *");
   RecurringJob.AddOrUpdate<CancelAtPeriodEndJob>(
       "cancel-period-end", j => j.ExecuteAsync(CancellationToken.None), "0 * * * *");
   ```
4. EF Core migration:
   ```
   dotnet ef migrations add InitialCreate --project TradingAnalytics.API
   dotnet ef database update --project TradingAnalytics.API
   ```
5. Smoke tests:
   - App starts, Swagger loads at /swagger
   - POST /api/v1/auth/register/email → 201
   - POST /api/v1/auth/login/email → 200 with JWT
   - GET /api/v1/market/summary → 200
   - GET /api/v1/portfolios → 200 (empty list)

---

# Reference: Standard patterns

## Command handler
```csharp
/// <summary>Standard application command handler abstraction.</summary>
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : IRequest<Result<TResponse>>;

public record CreateItemCommand(string Name) : IRequest<Result<ItemDto>>;

/// <summary>Base type for command handlers with shared dependencies.</summary>
public abstract class CommandHandler<TCommand, TResponse>(
    AppDbContext db,
    ICurrentUserService currentUser,
    ICacheService cache)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : IRequest<Result<TResponse>>
{
    protected AppDbContext Db { get; } = db ?? throw new ArgumentNullException(nameof(db));
    protected ICurrentUserService CurrentUser { get; } = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    protected ICacheService Cache { get; } = cache ?? throw new ArgumentNullException(nameof(cache));

    public abstract Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct);
}

/// <summary>Creates an item for the current user.</summary>
public sealed class CreateItemHandler(
    AppDbContext db,
    ICurrentUserService currentUser,
    ICacheService cache)
    : CommandHandler<CreateItemCommand, ItemDto>(db, currentUser, cache)
{
    public override async Task<Result<ItemDto>> Handle(CreateItemCommand cmd, CancellationToken ct)
    {
        // business failures → Result.Failure, never throw
        // infrastructure exceptions (DB down) → let propagate to GlobalExceptionMiddleware
        var item = Item.Create(cmd.Name);
        await Db.Items.AddAsync(item, ct);
        await Db.SaveChangesAsync(ct);
        return Result<ItemDto>.Success(new ItemDto { Id = item.Id, Name = item.Name });
    }
}
```

## Query handler (cursor paginated)
```csharp
public record GetItemsQuery(QueryParams Params) : IRequest<Result<CursorResult<ItemDto>>>;

public class GetItemsHandler : IRequestHandler<GetItemsQuery, Result<CursorResult<ItemDto>>>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly ICacheService _cache;

    public async Task<Result<CursorResult<ItemDto>>> Handle(
        GetItemsQuery request, CancellationToken ct)
    {
        var userId = _user.UserId!.Value;

        // Try cache for frequently accessed data
        // var cached = await _cache.GetAsync<List<ItemDto>>(CacheKeys.Something(), ct);

        var query = _db.Items
            .Where(i => i.UserId == userId && !i.IsDeleted)
            .OrderBy(i => i.Id);  // UUIDv7 = chronological

        if (!string.IsNullOrWhiteSpace(request.Params.Search))
            query = (IOrderedQueryable<Item>)query
                .Where(i => EF.Functions.ILike(i.Name, $"%{request.Params.Search}%"));

        var cursorResult = await query.ToCursorResultAsync(
            request.Params.After, request.Params.Limit, ct);

        var dtos = cursorResult.Items
            .Select(i => new ItemDto { Id = i.Id, Name = i.Name, CreatedAt = i.CreatedAt })
            .ToList();

        return Result<CursorResult<ItemDto>>.Success(
            new CursorResult<ItemDto>(dtos, cursorResult.NextCursor));
    }
}
```

## Controller
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ItemsController : AppControllerBase
{
    private readonly ISender _sender;
    public ItemsController(ISender sender) => _sender = sender;

    [HttpGet]
    [Authorize(Policy = Policies.CustomerOnly)]
    public async Task<IActionResult> GetItems([FromQuery] QueryParams q, CancellationToken ct)
        => CursoredResult(await _sender.Send(new GetItemsQuery(q), ct), "Items retrieved");

    [HttpPost]
    [Authorize(Policy = Policies.CustomerOnly)]
    public async Task<IActionResult> Create(CreateItemCommand cmd, CancellationToken ct)
        => CreatedResult(await _sender.Send(cmd, ct), "Item created");

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CustomerOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteItemCommand(id), ct);
        if (result.IsFailure) return BadRequest(ApiResponse<object?>.Ok(result.Error!));
        return OkMessage("Item deleted");
    }
}
```

## EF Core configuration
```csharp
public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        builder.Property(i => i.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at");
        builder.HasQueryFilter(i => !i.IsDeleted);

        // Enum → snake_case string
        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion(
                v => v.ToString().ToSnakeCase(),
                v => Enum.Parse<ItemStatus>(v.ToPascalCase(), true));
    }
}
```

## FluentValidation
```csharp
public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
    }
}
```
