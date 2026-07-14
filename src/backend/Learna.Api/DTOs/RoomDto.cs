namespace Learna.Api.DTOs;

public record RoomDto(
    int Id,
    string Name,
    string? Building,
    string? Description,
    int? Capacity
);

public record CreateRoomDto(
    string Name,
    string? Building,
    string? Description,
    int? Capacity
);

public record UpdateRoomDto(
    string Name,
    string? Building,
    string? Description,
    int? Capacity
);
