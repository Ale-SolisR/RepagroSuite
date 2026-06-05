using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public interface IItTicketService
{
    Task<PagedResult<ItTicketListDto>> GetPagedAsync(int page, int pageSize, ItTicketType? type,
        ItTicketStatus? status, string? search, CancellationToken cancellationToken = default);
    Task<ItTicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Devuelve el PDF de la boleta junto con un nombre de archivo identificativo
    /// (número de boleta + tipo + colaborador), seguro para descarga.</summary>
    Task<(byte[] Bytes, string FileName)> GetPdfAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Vuelve a generar el PDF de una boleta con el formato actual (cédulas, condición
    /// en español, etc.). Útil para boletas antiguas cuyo PDF se guardó con un formato previo.</summary>
    Task<ItTicketDto> RegeneratePdfAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ItTicketDto> CreateAssignmentAsync(CreateAssignmentDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> CreateReturnAsync(CreateReturnDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> CreateDeassignmentAsync(CreateDeassignmentDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> CreateIncidentAsync(CreateIncidentDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> CreateGenericTicketAsync(CreateGenericTicketDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ItTicketDto> VoidAsync(Guid id, VoidTicketDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
}
