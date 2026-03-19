using System.Reflection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Shared.Kernel.Entities;

namespace TradingAnalytics.Shared.Infrastructure.Persistence;

/// <summary>
/// Represents the primary PostgreSQL database context for the application.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator) : DbContext(options)
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    /// <summary>
    /// Gets the customers set.
    /// </summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>
    /// Gets the admin users set.
    /// </summary>
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    /// <summary>
    /// Gets the accounts set.
    /// </summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>
    /// Gets the customer sessions set.
    /// </summary>
    public DbSet<CustomerSession> CustomerSessions => Set<CustomerSession>();

    /// <summary>
    /// Gets the admin sessions set.
    /// </summary>
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();

    /// <summary>
    /// Gets the user devices set.
    /// </summary>
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    /// <summary>
    /// Gets the verifications set.
    /// </summary>
    public DbSet<Verification> Verifications => Set<Verification>();

    /// <summary>
    /// Gets the roles set.
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>
    /// Gets the permissions set.
    /// </summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>
    /// Gets the role permissions set.
    /// </summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>
    /// Configures the model for all infrastructure assemblies.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var idProperty = entityType.FindProperty(nameof(BaseEntity.Id));
            if (idProperty?.ClrType == typeof(Guid))
            {
                idProperty.SetValueGeneratorFactory(static (_, _) => new UuidV7ValueGenerator());
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Saves changes and dispatches collected domain events.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>().Where(static x => x.State == EntityState.Modified))
        {
            entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
        }

        var result = await base.SaveChangesAsync(ct);
        await DispatchDomainEventsAsync(ct);
        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Select(static x => x.Entity)
            .Where(static x => x.DomainEvents.Count != 0)
            .ToList();

        var events = aggregates.SelectMany(static x => x.DomainEvents).ToList();
        aggregates.ForEach(static x => x.ClearDomainEvents());

        foreach (var domainEvent in events)
        {
            await _mediator.Publish(domainEvent, ct);
        }
    }
}
