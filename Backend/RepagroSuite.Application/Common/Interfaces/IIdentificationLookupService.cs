using RepagroSuite.Application.Features.Identifications.DTOs;

namespace RepagroSuite.Application.Common.Interfaces;

public interface IIdentificationLookupService
{
    Task<IdentificationLookupResultDto?> LookupAsync(string identificationNumber, CancellationToken cancellationToken = default);
    Task<IdentificationLookupResultDto?> LookupFromCacheAsync(string normalizedNumber, CancellationToken cancellationToken = default);
}
