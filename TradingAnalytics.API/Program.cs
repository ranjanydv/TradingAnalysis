using MongoDB.Driver;
using TradingAnalytics.Modules.Identity;
using TradingAnalytics.Shared.Infrastructure;
using TradingAnalytics.Shared.Infrastructure.Auth;
using TradingAnalytics.Shared.Infrastructure.Http;
using TradingAnalytics.Shared.Infrastructure.MongoDB;
using TradingAnalytics.Shared.Kernel.Auth;

var builder = WebApplication.CreateBuilder(args);

OtpHasher.Configure(
    builder.Configuration["Auth:OtpHmacSecret"]
    ?? throw new InvalidOperationException("Auth:OtpHmacSecret is not configured."));

builder.Services
    .AddSharedInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddSwaggerWithJwt();

builder.Services.AddIdentityModule(builder.Configuration);
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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/swagger/{SwaggerGroups.Customer}/swagger.json", "Customer API v1");
        options.RoutePrefix = "swagger/customer";
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/swagger/{SwaggerGroups.Admin}/swagger.json", "Admin API v1");
        options.RoutePrefix = "swagger/admin";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok("TradingAnalytics API"));

app.Run();
