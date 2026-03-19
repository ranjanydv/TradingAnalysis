using MongoDB.Driver;
using TradingAnalytics.Shared.Infrastructure;
using TradingAnalytics.Shared.Infrastructure.Auth;
using TradingAnalytics.Shared.Infrastructure.Http;
using TradingAnalytics.Shared.Infrastructure.MongoDB;

var builder = WebApplication.CreateBuilder(args);

OtpHasher.Configure(
    builder.Configuration["Auth:OtpHmacSecret"]
    ?? throw new InvalidOperationException("Auth:OtpHmacSecret is not configured."));

builder.Services
    .AddSharedInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddSwaggerWithJwt();

builder.Services.AddControllers();

builder.Services.AddSignalR()
    .AddStackExchangeRedis(
        builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured."));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    await MongoIndexInitializer.EnsureIndexesAsync(mongoDatabase);
}

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok("TradingAnalytics API"));

app.Run();
