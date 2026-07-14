using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomRepository _repository;

    public RoomsController(IRoomRepository repository)
    {
        _repository = repository;
    }

    private static RoomDto ToDto(Room room) => new(
        room.Id,
        room.Name,
        room.Building,
        room.Description,
        room.Capacity
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAll()
    {
        var rooms = await _repository.GetAllAsync();
        return Ok(rooms.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoomDto>> GetById(int id)
    {
        var room = await _repository.GetByIdAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        return Ok(ToDto(room));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomDto createDto)
    {
        var room = new Room
        {
            Name = createDto.Name,
            Building = createDto.Building,
            Description = createDto.Description,
            Capacity = createDto.Capacity
        };

        var created = await _repository.CreateAsync(room);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> Update(int id, UpdateRoomDto updateDto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = updateDto.Name;
        existing.Building = updateDto.Building;
        existing.Description = updateDto.Description;
        existing.Capacity = updateDto.Capacity;

        var updated = await _repository.UpdateAsync(existing);

        return Ok(ToDto(updated));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _repository.DeleteAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
