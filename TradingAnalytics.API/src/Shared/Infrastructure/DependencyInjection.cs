using System.Reflection;
using System.Text;
using FirebaseAdmin;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using StackExchange.Redis;
using TradingAnalytics.Shared.Infrastructure.Auth;
using TradingAnalytics.Shared.Infrastructure.Behaviours;
using TradingAnalytics.Shared.Infrastructure.Cache;
using TradingAnalytics.Shared.Infrastructure.Firebase;
using TradingAnalytics.Shared.Infrastructure.MongoDB;
using TradingAnalytics.Shared.Infrastructure.Http;
using TradingAnalytics.Shared.Infrastructure.Persistence;
using TradingAnalytics.Shared.Infrastructure.Session;
using TradingAnalytics.Shared.Kernel;
using TradingAnalytics.Shared.Kernel.Auth;
using TradingAnalytics.Shared.Kernel.Interfaces;

namespace TradingAnalytics.Shared.Infrastructure;

/// <summary>
/// Registers shared infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the shared infrastructure services.
    /// </summary>
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ISessionStore, RedisSessionStore>();

        var mongoConnection = configuration["MongoDB:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDB:ConnectionString is not configured.");
        var mongoDatabase = configuration["MongoDB:Database"]
            ?? throw new InvalidOperationException("MongoDB:Database is not configured.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));
        services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

        services.Configure<FirebaseConfig>(configuration.GetSection("Firebase"));
        services.AddSingleton(static sp =>
        {
            var options = sp.GetRequiredService<IOptions<FirebaseConfig>>().Value;
            if (FirebaseApp.DefaultInstance is not null)
            {
                return FirebaseApp.DefaultInstance;
            }

            return FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(options.CredentialFilePath)
            });
        });
        services.AddScoped<IFirebaseMessagingService, FirebaseMessagingService>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPasswordService, PasswordService>();

        return services;
    }

    /// <summary>
    /// Adds JWT authentication and authorization policies.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JwtConfig>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateDataAnnotations()
            .Validate(static config => !string.IsNullOrWhiteSpace(config.Secret) && config.Secret.Length >= 32, "Jwt:Secret must be at least 32 characters long.")
            .ValidateOnStart();

        var jwt = configuration.GetSection("Jwt").Get<JwtConfig>()
            ?? throw new InvalidOperationException("Jwt configuration is missing.");
        var key = Encoding.UTF8.GetBytes(jwt.Secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    {
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AdminOnly, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim(Constants.ClaimTypes.ActorType, Constants.ActorTypes.Admin));

            options.AddPolicy(Policies.CustomerOnly, policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim(Constants.ClaimTypes.ActorType, Constants.ActorTypes.Customer));

            options.AddPolicy(Policies.AnyActor, policy => policy.RequireAuthenticatedUser());
        });

        services.AddScoped<IJwtService, JwtService>();
        return services;
    }

    /// <summary>
    /// Adds Swagger with JWT bearer configuration.
    /// </summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }
}

/// <summary>
/// Provides the current UTC time.
/// </summary>
public sealed class DateTimeService : IDateTimeService
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
