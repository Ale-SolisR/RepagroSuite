using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Reservations.DTOs;
using RepagroSuite.Application.Features.Reservations.Services;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ICurrentUserService _currentUser;

    public ReservationsController(IReservationService reservationService, ICurrentUserService currentUser)
    {
        _reservationService = reservationService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "Reservations.View")]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null, [FromQuery] Guid? roomId = null,
        [FromQuery] ReservationStatus? status = null,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        [FromQuery] bool sortDescending = true,
        CancellationToken ct = default)
    {
        var result = await _reservationService.GetPagedAsync(page, pageSize, userId, roomId, status, from, to, sortDescending, ct);
        return Ok(ApiResponse<PagedResult<ReservationDto>>.Ok(result));
    }

    // Auditoría agrupada: cada entrada es una reserva individual o el resumen de una serie periódica.
    [HttpGet("audit")]
    [Authorize(Policy = "Reservations.View")]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationGroupDto>>>> GetAudit(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null, [FromQuery] Guid? roomId = null,
        [FromQuery] ReservationStatus? status = null,
        [FromQuery] bool sortDescending = true,
        CancellationToken ct = default)
    {
        var result = await _reservationService.GetAuditGroupsAsync(page, pageSize, userId, roomId, status, sortDescending, ct);
        return Ok(ApiResponse<PagedResult<ReservationGroupDto>>.Ok(result));
    }

    [HttpGet("recurring/{groupId:guid}/occurrences")]
    [Authorize(Policy = "Reservations.View")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReservationDto>>>> GetGroupOccurrences(Guid groupId, CancellationToken ct)
    {
        var result = await _reservationService.GetGroupOccurrencesAsync(groupId, ct);
        return Ok(ApiResponse<IEnumerable<ReservationDto>>.Ok(result));
    }

    [HttpPost("recurring/{groupId:guid}/approve")]
    [Authorize(Policy = "Reservations.Approve")]
    public async Task<ActionResult<ApiResponse<BulkActionResultDto>>> ApproveGroup(Guid groupId, CancellationToken ct)
    {
        var result = await _reservationService.ApproveGroupAsync(groupId, _currentUser.UserId!.Value, ct);
        var message = result.Skipped > 0
            ? $"{result.Affected} reserva(s) aprobada(s), {result.Skipped} omitida(s) por conflicto."
            : $"{result.Affected} reserva(s) aprobada(s).";
        return Ok(ApiResponse<BulkActionResultDto>.Ok(result, message));
    }

    [HttpPost("recurring/{groupId:guid}/reject")]
    [Authorize(Policy = "Reservations.Reject")]
    public async Task<ActionResult<ApiResponse<BulkActionResultDto>>> RejectGroup(Guid groupId, [FromBody] RejectReservationDto dto, CancellationToken ct)
    {
        var result = await _reservationService.RejectGroupAsync(groupId, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<BulkActionResultDto>.Ok(result, $"{result.Affected} reserva(s) rechazada(s)."));
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationDto>>>> GetMy(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ReservationStatus? status = null,
        [FromQuery] bool sortDescending = true, CancellationToken ct = default)
    {
        var result = await _reservationService.GetPagedAsync(page, pageSize, userId: _currentUser.UserId, status: status, sortDescending: sortDescending, cancellationToken: ct);
        return Ok(ApiResponse<PagedResult<ReservationDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _reservationService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<ReservationDto>.Ok(result));
    }

    [HttpGet("calendar")]
    [Authorize(Policy = "Reservations.View")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CalendarEventDto>>>> GetCalendar(
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] Guid? roomId = null, CancellationToken ct = default)
    {
        var result = await _reservationService.GetCalendarEventsAsync(from, to, roomId, ct);
        return Ok(ApiResponse<IEnumerable<CalendarEventDto>>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Reservations.Create")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> Create(
        [FromBody] CreateReservationDto dto, CancellationToken ct)
    {
        var callerCanApprove = _currentUser.Permissions.Contains("Reservations.Approve");
        var result = await _reservationService.CreateAsync(_currentUser.UserId!.Value, dto, callerCanApprove, ct);
        var message = result.Status == ReservationStatus.Approved
            ? "Reserva creada y aprobada."
            : "Solicitud de reserva enviada. Pendiente de aprobación.";
        return Created(string.Empty, ApiResponse<ReservationDto>.Ok(result, message));
    }

    [HttpPost("recurring")]
    [Authorize(Policy = "Reservations.Create")]
    public async Task<ActionResult<ApiResponse<RecurringReservationResultDto>>> CreateRecurring(
        [FromBody] CreateRecurringReservationDto dto, CancellationToken ct)
    {
        var callerCanApprove = _currentUser.Permissions.Contains("Reservations.Approve");
        var result = await _reservationService.CreateRecurringAsync(_currentUser.UserId!.Value, dto, callerCanApprove, ct);
        var message = result.CreatedCount > 0
            ? $"{result.CreatedCount} de {result.TotalOccurrences} reservas recurrentes enviadas. Pendientes de aprobación."
            : "No se pudo crear ninguna ocurrencia. Revise los conflictos de horario.";
        return Created(string.Empty, ApiResponse<RecurringReservationResultDto>.Ok(result, message));
    }

    [HttpPost("direct")]
    [Authorize(Policy = "Reservations.DirectCreate")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> DirectCreate(
        [FromBody] AdminDirectReservationDto dto, CancellationToken ct)
    {
        var result = await _reservationService.AdminDirectCreateAsync(_currentUser.UserId!.Value, dto, ct);
        return Created(string.Empty, ApiResponse<ReservationDto>.Ok(result, "Sala reservada directamente."));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "Reservations.Approve")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> Approve(
        Guid id, [FromBody] ApproveReservationDto dto, CancellationToken ct)
    {
        var result = await _reservationService.ApproveAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ReservationDto>.Ok(result, "Reserva aprobada."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "Reservations.Reject")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> Reject(
        Guid id, [FromBody] RejectReservationDto dto, CancellationToken ct)
    {
        var result = await _reservationService.RejectAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ReservationDto>.Ok(result, "Reserva rechazada."));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Reservations.Cancel")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> Cancel(
        Guid id, [FromBody] CancelReservationDto dto, CancellationToken ct)
    {
        var canManageAny = _currentUser.Permissions.Contains("Reservations.Approve");
        var result = await _reservationService.CancelAsync(id, dto, _currentUser.UserId!.Value, canManageAny, ct);
        return Ok(ApiResponse<ReservationDto>.Ok(result, "Reserva cancelada."));
    }
}
