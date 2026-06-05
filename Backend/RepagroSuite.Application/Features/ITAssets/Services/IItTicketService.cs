using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public interface IItTicketService
{
    Task<PagedResult<ItTicketListDto>> GetPagedAsync(int page, int pageSize, ItTicketType? type,
        ItTicketStatus? status, string? search, CancellationToken cancellationToken = default);
    Task<ItTicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<byte[]> GetPdfAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ItTicketDto> CreateAssignmentAsync(CreateAssignmentDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> CreateReturnAsync(CreateReturnDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> CreateDeassignmentAsync(CreateDeassignmentDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> CreateIncidentAsync(CreateIncidentDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> CreateGenericTicketAsync(CreateGenericTicketDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> VoidAsync(Guid id, VoidTicketDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
}
