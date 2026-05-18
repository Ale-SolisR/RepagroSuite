using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Rooms.DTOs;
using RepagroSuite.Application.Features.Rooms.Services;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ICurrentUserService _currentUser;

    public RoomsController(IRoomService roomService, ICurrentUserService currentUser)
    {
        _roomService = roomService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "Rooms.View")]
    public async Task<ActionResult<ApiResponse<PagedResult<RoomDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] RoomStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _roomService.GetPagedAsync(page, pageSize, search, status, ct);
        return Ok(ApiResponse<PagedResult<RoomDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Rooms.View")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _roomService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<RoomDto>.Ok(result));
    }

    [HttpGet("available")]
    [Authorize(Policy = "Rooms.View")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoomDto>>>> GetAvailable(
        [FromQuery] DateTime start, [FromQuery] DateTime end,
        [FromQuery] int? minCapacity = null, CancellationToken ct = default)
    {
        var result = await _roomService.GetAvailableAsync(start, end, minCapacity, ct);
        return Ok(ApiResponse<IEnumerable<RoomDto>>.Ok(result));
    }

    [HttpGet("features")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FeatureDto>>>> GetFeatures(CancellationToken ct)
    {
        var result = await _roomService.GetActiveFeaturesAsync(ct);
        return Ok(ApiResponse<IEnumerable<FeatureDto>>.Ok(result));
    }

    [HttpGet("{id:guid}/slots")]
    [Authorize(Policy = "Rooms.View")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AvailableSlotDto>>>> GetSlots(
        Guid id, [FromQuery] DateTime date, CancellationToken ct)
    {
        var result = await _roomService.GetAvailableSlotsAsync(id, date, ct);
        return Ok(ApiResponse<IEnumerable<AvailableSlotDto>>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Rooms.Create")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> Create(
        [FromBody] CreateRoomDto dto, CancellationToken ct)
    {
        var result = await _roomService.CreateAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<RoomDto>.Ok(result, "Sala creada correctamente."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Rooms.Update")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> Update(
        Guid id, [FromBody] UpdateRoomDto dto, CancellationToken ct)
    {
        var result = await _roomService.UpdateAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<RoomDto>.Ok(result, "Sala actualizada correctamente."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "Rooms.Update")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> ChangeStatus(
        Guid id, [FromBody] ChangeRoomStatusDto dto, CancellationToken ct)
    {
        var result = await _roomService.ChangeStatusAsync(id, dto.Status, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<RoomDto>.Ok(result, "Estado de sala actualizado."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Rooms.Delete")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _roomService.DeleteAsync(id, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Sala eliminada."));
    }

    [HttpGet("{id:guid}/availability")]
    [Authorize(Policy = "Rooms.View")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoomAvailabilityDto>>>> GetAvailability(Guid id, CancellationToken ct)
    {
        var result = await _roomService.GetAvailabilitiesAsync(id, ct);
        return Ok(ApiResponse<IEnumerable<RoomAvailabilityDto>>.Ok(result));
    }

    [HttpPut("{id:guid}/availability")]
    [Authorize(Policy = "Rooms.Availability.Manage")]
    public async Task<ActionResult<ApiResponse<object>>> UpsertAvailability(
        Guid id, [FromBody] List<UpsertRoomAvailabilityDto> dtos, CancellationToken ct)
    {
        await _roomService.UpsertAvailabilityAsync(id, dtos, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Horarios de disponibilidad actualizados."));
    }

    [HttpPost("{id:guid}/blocks")]
    [Authorize(Policy = "Rooms.Availability.Manage")]
    public async Task<ActionResult<ApiResponse<RoomBlockDto>>> CreateBlock(
        Guid id, [FromBody] CreateRoomBlockDto dto, CancellationToken ct)
    {
        dto.RoomId = id;
        var result = await _roomService.CreateBlockAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<RoomBlockDto>.Ok(result, "Bloqueo creado."));
    }

    [HttpDelete("blocks/{blockId:guid}")]
    [Authorize(Policy = "Rooms.Availability.Manage")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBlock(Guid blockId, CancellationToken ct)
    {
        await _roomService.DeleteBlockAsync(blockId, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Bloqueo eliminado."));
    }
}

public class ChangeRoomStatusDto
{
    public RoomStatus Status { get; set; }
}
