using Learna.Core.Entities;
using Learna.Core.Interfaces;

namespace Learna.Api.DTOs;

public record LessonDto(
    int Id,
    int SubjectGroupId,
    string SubjectGroupName,
    string SubjectNameEnglish,
    string? SubjectNameThai,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? RoomId,
    string? RoomName,
    int? TeacherId,
    string? TeacherName,
    LessonStatus Status,
    string? Note,
    int? SourceRuleId,
    bool IsModified,
    bool AttendanceRegistered,
    bool CanManageAttendance
);

public record CreateLessonDto(
    int SubjectGroupId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? RoomId,
    int? TeacherId,
    string? Note,
    bool Force = false
);

public record UpdateLessonDto(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? RoomId,
    int? TeacherId,
    string? Note,
    LessonStatus Status,
    bool Force = false
);

public record LessonConflictDto(
    ConflictType Type,
    int LessonId,
    int SubjectGroupId,
    string SubjectGroupName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Detail
);

public record ConflictResponseDto(IEnumerable<LessonConflictDto> Conflicts);
