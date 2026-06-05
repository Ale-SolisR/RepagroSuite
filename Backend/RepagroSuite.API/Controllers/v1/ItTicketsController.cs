using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Application.Features.ITAssets.Services;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ti/tickets")]
[Authorize]
public class ItTicketsController : ControllerBase
{
    private readonly IItTicketService _tickets;
    private readonly ICurrentUserService _currentUser;

    public ItTicketsController(IItTicketService tickets, ICurrentUserService currentUser)
    {
        _tickets = tickets;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<PagedResult<ItTicketListDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ItTicketType? type = null, [FromQuery] ItTicketStatus? status = null,
        [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await _tickets.GetPagedAsync(page, pageSize, type, status, search, ct);
        return Ok(ApiResponse<PagedResult<ItTicketListDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<ItTicketDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _tickets.GetByIdAsync(id, ct);
        return Ok(ApiResponse<ItTicketDto>.Ok(result));
    }

    [HttpGet("{id:guid}/pdf")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken ct)
    {
        var (bytes, fileName) = await _tickets.GetPdfAsync(id, ct);
        return File(bytes, "application/pdf", fileName);
    }

    [HttpPost("{id:guid}/regenerate-pdf")]
    [Authorize(Policy = "Ti.Ticket.Create")]
    public async Task<ActionResult<ApiResponse<ItTicketDto>>> RegeneratePdf(Guid id, CancellationToken ct)
    {
        var result = await _tickets.RegeneratePdfAsync(id, ct);
        return Ok(ApiResponse<ItTicketDto>.Ok(result, "PDF regenerado con el formato actual."));
    }

    [HttpPost("assignments")]
    [Authorize(Policy = "Ti.Assign")]
    public async Task<ActionResult<ApiResponse<ItTicketDto>>> CreateAssignment([FromBody] CreateAssignmentDto dto, CancellationToken ct)
    {
        var result = await _tickets.CreateAssignmentAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItTicketDto>.Ok(result, "Boleta de entrega emitida."));
    }

    [HttpPost("returns")]
    [Authorize(Policy = "Ti.Return")]
    public async Task<ActionResult<ApiResponse<ItTicketDto>>> CreateReturn([FromBody] CreateReturnDto dto, CancellationToken ct)
    {
        var result = await _tickets.CreateReturnAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItTicketDto>.Ok(result, "Boleta de devolución emitida."));
    }

    [HttpPost("deassignments")]
    [Authorize(Policy = "Ti.Return")]
    public async Task<ActionResult<ApiResponse<ItTicketDto>>> CreateDeassignment([FromBody] CreateDeassignmentDto dto, CancellationToken ct)
    {
        var result = await _tickets.CreateDeassignmentAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItTicketDto>.Ok(result, "Boleta de desasignación emitida."));
    }

    [HttpPost("incidents")]
    [Authorize(Policy = "Ti.Ticket.Create")]
    public async Task<ActionResult<ApiResponse<ItTicketDto>>> CreateIncident([FromBody] CreateIncidentDto dto, CancellationToken ct)
    {
        var result = await _tickets.CreateIncidentAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItTicketDto>.Ok(result, "Boleta de incidente emitida."));
    }

    [HttpPost]
    [Authorize(Policy = "Ti.Ticket.Create")]
    public async Task<ActionResult<ApiResponse<ItTicketDto>>> CreateGeneric([FromBody] CreateGenericTicketDto dto, CancellationToken ct)
    {
        var result = await _tickets.CreateGenericTicketAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItTicketDto>.Ok(result, "Boleta emitida."));
    }

    [HttpPost("{id:guid}/void")]
    [Authorize(Policy = "Ti.Ticket.Void")]
    public async Task<ActionResult<ApiResponse<ItTicketDto>>> Void(Guid id, [FromBody] VoidTicketDto dto, CancellationToken ct)
    {
        var result = await _tickets.VoidAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItTicketDto>.Ok(result, "Boleta anulada."));
    }
}
