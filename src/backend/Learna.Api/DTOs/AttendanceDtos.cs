using Learna.Core.Entities;

namespace Learna.Api.DTOs;

public record AttendanceRecordDto(AttendanceStatus Status, string? Note, int RecordedByUserId, string RecordedByEmail, DateTime RecordedAt, DateTime UpdatedAt);
public record AttendanceRosterStudentDto(int StudentId, string FirstName, string LastName, string? Nickname, AttendanceRecordDto? Record);
public record LessonAttendanceDto(int LessonId, DateOnly LessonDate, bool CanEdit, DateOnly EditDeadline, IEnumerable<AttendanceRosterStudentDto> Students);
public record AttendanceUpsertItemDto(int StudentId, AttendanceStatus Status, string? Note);
public record BulkAttendanceDto(IEnumerable<AttendanceUpsertItemDto> Records);
public record AttendanceStatusTotalsDto(int Present, int Absent, int Late, int ExcusedAbsence, int Sick, int ApprovedLeave);
public record AttendanceGroupSummaryDto(int SubjectGroupId, string SubjectGroupName, string SubjectNameEnglish, string? SubjectNameThai, int RecordedLessons, decimal PresencePercentage, int ExcusedAbsences, int UnexcusedAbsences);
public record RecentAttendanceDto(int LessonId, DateOnly Date, TimeOnly StartTime, int SubjectGroupId, string SubjectGroupName, string SubjectNameEnglish, string? SubjectNameThai, AttendanceStatus Status, string? Note);
public record AttendanceSummaryDto(int RecordedLessons, AttendanceStatusTotalsDto Totals, decimal PresencePercentage, int ExcusedAbsences, int UnexcusedAbsences, IEnumerable<AttendanceGroupSummaryDto> SubjectGroups, IEnumerable<RecentAttendanceDto> RecentRecords);
