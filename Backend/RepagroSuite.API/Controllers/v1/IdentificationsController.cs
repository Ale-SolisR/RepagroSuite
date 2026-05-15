using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Identifications.DTOs;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class IdentificationsController : ControllerBase
{
    private readonly IIdentificationLookupService _lookupService;

    public IdentificationsController(IIdentificationLookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet("lookup")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IdentificationLookupResultDto>>> Lookup(
        [FromQuery] string identificationNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identificationNumber))
            return BadRequest(ApiResponse<IdentificationLookupResultDto>.Fail("El número de cédula es obligatorio.", "VALIDATION_ERROR"));

        var normalized = new string(identificationNumber.Where(char.IsDigit).ToArray());

        if (normalized.Length < 9 || normalized.Length > 10)
            return BadRequest(ApiResponse<IdentificationLookupResultDto>.Fail("Formato de cédula inválido.", "VALIDATION_ERROR"));

        var result = await _lookupService.LookupAsync(normalized, ct);

        if (result == null)
            return Ok(ApiResponse<IdentificationLookupResultDto>.Ok(new IdentificationLookupResultDto { Found = false },
                "No se encontró información para la cédula ingresada."));

        return Ok(ApiResponse<IdentificationLookupResultDto>.Ok(result, "Cédula validada correctamente."));
    }
}
