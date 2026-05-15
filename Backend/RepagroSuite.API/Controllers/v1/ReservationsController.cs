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
        CancellationToken ct = default)
    {
        var result = await _reservationService.GetPagedAsync(page, pageSize, userId, roomId, status, from, to, ct);
        return Ok(ApiResponse<PagedResult<ReservationDto>>.Ok(result));
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationDto>>>> GetMy(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ReservationStatus? status = null, CancellationToken ct = default)
    {
        var result = await _reservationService.GetPagedAsync(page, pageSize, userId: _currentUser.UserId, status: status, cancellationToken: ct);
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
        var result = await _reservationService.CreateAsync(_currentUser.UserId!.Value, dto, ct);
        return Created(string.Empty, ApiResponse<ReservationDto>.Ok(result, "Solicitud de reserva enviada. Pendiente de aprobación."));
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
        var result = await _reservationService.CancelAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ReservationDto>.Ok(result, "Reserva cancelada."));
    }
}
