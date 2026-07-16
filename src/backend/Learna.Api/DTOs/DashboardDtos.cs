namespace Learna.Api.DTOs;

public record TeacherRecentSubmissionDto(
    int SubmissionId,
    int StudentId,
    string StudentName,
    int SubjectGroupId,
    string SubjectGroupName,
    string AssignmentTitle,
    DateTime SubmittedAt,
    bool IsLate
);

public record TeacherDashboardDto(
    IReadOnlyList<LessonDto> TodayLessons,
    int MissingAttendanceCount,
    IReadOnlyList<LessonDto> MissingAttendanceLessons,
    int AwaitingGradingCount,
    IReadOnlyList<TeacherRecentSubmissionDto> RecentSubmissions
);

public record AdminDashboardDto(
    int StudentCount,
    int TeacherCount,
    int ClassCount,
    int SubjectGroupCount,
    int TodayLessonCount,
    int MissingAttendanceCount
);
