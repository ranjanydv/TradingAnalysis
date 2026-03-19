using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingAnalytics.Modules.Identity.Application.Dtos;
using TradingAnalytics.Modules.Identity.Application.Mappings;
using TradingAnalytics.Shared.Infrastructure.Persistence;
using TradingAnalytics.Shared.Kernel.Http;
using TradingAnalytics.Shared.Kernel.Interfaces;
using TradingAnalytics.Shared.Kernel.Pagination;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Identity.Application.Queries;

/// <summary>
/// Gets the current authenticated customer profile.
/// </summary>
public sealed record GetCurrentCustomerQuery() : IRequest<Result<CustomerProfileDto>>;

/// <summary>
/// Gets all customer sessions for the current user.
/// </summary>
public sealed record GetMySessionsQuery() : IRequest<Result<List<SessionDto>>>;

/// <summary>
/// Gets all customer devices for the current user.
/// </summary>
public sealed record GetMyDevicesQuery() : IRequest<Result<List<DeviceDto>>>;

/// <summary>
/// Gets a customer by identifier.
/// </summary>
/// <param name="Id">The customer identifier.</param>
public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDetailDto>>;

/// <summary>
/// Gets customers using cursor pagination.
/// </summary>
/// <param name="Params">The query parameters.</param>
public sealed record GetAllCustomersQuery(QueryParams Params) : IRequest<Result<CursorResult<CustomerSummaryDto>>>;

internal sealed class GetCurrentCustomerHandler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<GetCurrentCustomerQuery, Result<CustomerProfileDto>>
{
    public async Task<Result<CustomerProfileDto>> Handle(GetCurrentCustomerQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result<CustomerProfileDto>.Failure("User is not authenticated.");
        }

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == currentUser.UserId.Value, ct);
        return customer is null
            ? Result<CustomerProfileDto>.Failure("Customer not found.")
            : Result<CustomerProfileDto>.Success(customer.ToProfileDto());
    }
}

internal sealed class GetMySessionsHandler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<GetMySessionsQuery, Result<List<SessionDto>>>
{
    public async Task<Result<List<SessionDto>>> Handle(GetMySessionsQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result<List<SessionDto>>.Failure("User is not authenticated.");
        }

        var sessions = await db.CustomerSessions.Where(x => x.CustomerId == currentUser.UserId.Value).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Result<List<SessionDto>>.Success(sessions.Select(x => x.ToDto()).ToList());
    }
}

internal sealed class GetMyDevicesHandler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<GetMyDevicesQuery, Result<List<DeviceDto>>>
{
    public async Task<Result<List<DeviceDto>>> Handle(GetMyDevicesQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Result<List<DeviceDto>>.Failure("User is not authenticated.");
        }

        var devices = await db.UserDevices.Where(x => x.CustomerId == currentUser.UserId.Value).OrderByDescending(x => x.LastActiveAt).ToListAsync(ct);
        return Result<List<DeviceDto>>.Success(devices.Select(x => x.ToDto()).ToList());
    }
}

internal sealed class GetCustomerByIdHandler(AppDbContext db) : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDetailDto>>
{
    public async Task<Result<CustomerDetailDto>> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return customer is null
            ? Result<CustomerDetailDto>.Failure("Customer not found.")
            : Result<CustomerDetailDto>.Success(customer.ToDetailDto());
    }
}

internal sealed class GetAllCustomersHandler(AppDbContext db) : IRequestHandler<GetAllCustomersQuery, Result<CursorResult<CustomerSummaryDto>>>
{
    public async Task<Result<CursorResult<CustomerSummaryDto>>> Handle(GetAllCustomersQuery request, CancellationToken ct)
    {
        var query = db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Params.Search))
        {
            var search = request.Params.Search.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, $"%{search}%")
                || (x.Email != null && EF.Functions.ILike(x.Email, $"%{search}%"))
                || (x.Phone != null && EF.Functions.ILike(x.Phone, $"%{search}%")));
        }

        var cursor = await query.ToCursorResultAsync(request.Params.After, request.Params.Limit, ct);
        return Result<CursorResult<CustomerSummaryDto>>.Success(new CursorResult<CustomerSummaryDto>(
            cursor.Items.Select(x => x.ToSummaryDto()).ToList(),
            cursor.NextCursor,
            cursor.PrevCursor));
    }
}
