using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;

namespace Learna.Infrastructure.Services;

public class LessonSchedulingService : ILessonSchedulingService
{
    private readonly ApplicationDbContext _context;
    private readonly ISubjectGroupRepository _subjectGroupRepository;
    private readonly ITermRepository _termRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ILessonRuleRepository _lessonRuleRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public LessonSchedulingService(
        ApplicationDbContext context,
        ISubjectGroupRepository subjectGroupRepository,
        ITermRepository termRepository,
        IRoomRepository roomRepository,
        ITeacherRepository teacherRepository,
        ILessonRuleRepository lessonRuleRepository,
        ILessonRepository lessonRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _context = context;
        _subjectGroupRepository = subjectGroupRepository;
        _termRepository = termRepository;
        _roomRepository = roomRepository;
        _teacherRepository = teacherRepository;
        _lessonRuleRepository = lessonRuleRepository;
        _lessonRepository = lessonRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<RuleSaveResult> CreateRuleAsync(
        int subjectGroupId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime,
        int? roomId, DateOnly? startDate, DateOnly? endDate, bool force)
    {
        var group = await _subjectGroupRepository.GetByIdAsync(subjectGroupId);
        if (group == null)
        {
            return new RuleSaveResult { ValidationError = "Subject group not found." };
        }

        var term = await _termRepository.GetByIdAsync(group.TermId);
        if (term == null)
        {
            return new RuleSaveResult { ValidationError = "Term not found." };
        }

        var termStart = DateOnly.FromDateTime(term.StartDate);
        var termEnd = DateOnly.FromDateTime(term.EndDate);
        var effectiveStart = startDate ?? termStart;
        var effectiveEnd = endDate ?? termEnd;

        var validationError = ValidateRuleInput(startTime, endTime, effectiveStart, effectiveEnd, termStart, termEnd);
        if (validationError != null)
        {
            return new RuleSaveResult { ValidationError = validationError };
        }

        if (roomId.HasValue && await _roomRepository.GetByIdAsync(roomId.Value) == null)
        {
            return new RuleSaveResult { ValidationError = "Room not found." };
        }

        var occurrences = GenerateOccurrences(effectiveStart, effectiveEnd, dayOfWeek).ToList();

        var candidates = occurrences.Select(date => new ConflictCandidate
        {
            SubjectGroupId = subjectGroupId,
            TeacherId = group.TeacherId,
            RoomId = roomId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime
        }).ToList();

        var conflicts = await FindConflictsAsync(candidates, new HashSet<int>());
        if (conflicts.Count > 0 && !force)
        {
            return new RuleSaveResult { Conflicts = conflicts };
        }

        var rule = new LessonRule
        {
            SubjectGroupId = subjectGroupId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            RoomId = roomId,
            StartDate = effectiveStart,
            EndDate = effectiveEnd
        };

        var transaction = await BeginTransactionIfSupportedAsync();
        try
        {
            await _lessonRuleRepository.AddAsync(rule);

            var lessons = occurrences.Select(date => new Lesson
            {
                SubjectGroupId = subjectGroupId,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                RoomId = roomId,
                TeacherId = group.TeacherId,
                Status = LessonStatus.Scheduled,
                SourceRuleId = rule.Id,
                IsModified = false
            }).ToList();

            await _lessonRepository.AddRangeAsync(lessons);

            if (transaction != null) await transaction.CommitAsync();
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }

        return new RuleSaveResult { Rule = rule, Conflicts = conflicts };
    }

    public async Task<RuleSaveResult> UpdateRuleAsync(
        int ruleId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime,
        int? roomId, DateOnly startDate, DateOnly endDate, bool force)
    {
        var rule = await _lessonRuleRepository.GetByIdWithLessonsAsync(ruleId);
        if (rule == null)
        {
            return new RuleSaveResult { NotFound = true };
        }

        var group = await _subjectGroupRepository.GetByIdAsync(rule.SubjectGroupId);
        if (group == null)
        {
            return new RuleSaveResult { ValidationError = "Subject group not found." };
        }

        var term = await _termRepository.GetByIdAsync(group.TermId);
        if (term == null)
        {
            return new RuleSaveResult { ValidationError = "Term not found." };
        }

        var termStart = DateOnly.FromDateTime(term.StartDate);
        var termEnd = DateOnly.FromDateTime(term.EndDate);

        var validationError = ValidateRuleInput(startTime, endTime, startDate, endDate, termStart, termEnd);
        if (validationError != null)
        {
            return new RuleSaveResult { ValidationError = validationError };
        }

        if (roomId.HasValue && await _roomRepository.GetByIdAsync(roomId.Value) == null)
        {
            return new RuleSaveResult { ValidationError = "Room not found." };
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var futureUnmodified = rule.Lessons.Where(l => !l.IsModified && l.Date >= today).ToList();
        // Dates already covered by a surviving (modified) lesson for this rule must not get a duplicate.
        var preservedDates = rule.Lessons.Where(l => l.IsModified && l.Date >= today).Select(l => l.Date).ToHashSet();

        var occurrences = GenerateOccurrences(startDate, endDate, dayOfWeek)
            .Where(d => d >= today && !preservedDates.Contains(d))
            .ToList();

        var candidates = occurrences.Select(date => new ConflictCandidate
        {
            SubjectGroupId = rule.SubjectGroupId,
            TeacherId = group.TeacherId,
            RoomId = roomId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime
        }).ToList();

        var excludeIds = futureUnmodified.Select(l => l.Id).ToHashSet();
        var conflicts = await FindConflictsAsync(candidates, excludeIds);
        if (conflicts.Count > 0 && !force)
        {
            return new RuleSaveResult { Conflicts = conflicts };
        }

        var transaction = await BeginTransactionIfSupportedAsync();
        try
        {
            await _lessonRepository.RemoveRangeAsync(futureUnmodified);

            rule.DayOfWeek = dayOfWeek;
            rule.StartTime = startTime;
            rule.EndTime = endTime;
            rule.RoomId = roomId;
            rule.StartDate = startDate;
            rule.EndDate = endDate;
            await _lessonRuleRepository.UpdateAsync(rule);

            var newLessons = occurrences.Select(date => new Lesson
            {
                SubjectGroupId = rule.SubjectGroupId,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                RoomId = roomId,
                TeacherId = group.TeacherId,
                Status = LessonStatus.Scheduled,
                SourceRuleId = rule.Id,
                IsModified = false
            }).ToList();

            await _lessonRepository.AddRangeAsync(newLessons);

            if (transaction != null) await transaction.CommitAsync();
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }

        return new RuleSaveResult { Rule = rule, Conflicts = conflicts };
    }

    public async Task<bool> DeleteRuleAsync(int ruleId)
    {
        var rule = await _lessonRuleRepository.GetByIdWithLessonsAsync(ruleId);
        if (rule == null) return false;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var toDelete = rule.Lessons.Where(l => !l.IsModified && l.Date >= today).ToList();

        var transaction = await BeginTransactionIfSupportedAsync();
        try
        {
            await _lessonRepository.RemoveRangeAsync(toDelete);
            // Remaining (past/modified) lessons are tracked via the Include above; EF sets
            // their SourceRuleId to null on save because that relationship is DeleteBehavior.SetNull.
            await _lessonRuleRepository.DeleteAsync(rule);

            if (transaction != null) await transaction.CommitAsync();
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }

        return true;
    }

    public async Task<LessonSaveResult> CreateOneOffLessonAsync(
        int subjectGroupId, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        int? roomId, int? teacherId, string? note, bool force)
    {
        var group = await _subjectGroupRepository.GetByIdAsync(subjectGroupId);
        if (group == null)
        {
            return new LessonSaveResult { ValidationError = "Subject group not found." };
        }

        if (endTime <= startTime)
        {
            return new LessonSaveResult { ValidationError = "End time must be after start time." };
        }

        if (roomId.HasValue && await _roomRepository.GetByIdAsync(roomId.Value) == null)
        {
            return new LessonSaveResult { ValidationError = "Room not found." };
        }

        var effectiveTeacherId = teacherId ?? group.TeacherId;
        if (effectiveTeacherId.HasValue && await _teacherRepository.GetByIdAsync(effectiveTeacherId.Value) == null)
        {
            return new LessonSaveResult { ValidationError = "Teacher not found." };
        }

        var candidate = new ConflictCandidate
        {
            SubjectGroupId = subjectGroupId,
            TeacherId = effectiveTeacherId,
            RoomId = roomId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime
        };

        var conflicts = await FindConflictsAsync(new List<ConflictCandidate> { candidate }, new HashSet<int>());
        if (conflicts.Count > 0 && !force)
        {
            return new LessonSaveResult { Conflicts = conflicts };
        }

        var lesson = new Lesson
        {
            SubjectGroupId = subjectGroupId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            RoomId = roomId,
            TeacherId = effectiveTeacherId,
            Note = note,
            Status = LessonStatus.Scheduled,
            SourceRuleId = null,
            IsModified = false
        };

        await _lessonRepository.AddAsync(lesson);

        return new LessonSaveResult { Lesson = lesson, Conflicts = conflicts };
    }

    public async Task<LessonSaveResult> UpdateLessonAsync(
        int lessonId, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        int? roomId, int? teacherId, string? note, LessonStatus status, bool force)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        if (lesson == null)
        {
            return new LessonSaveResult { NotFound = true };
        }

        if (endTime <= startTime)
        {
            return new LessonSaveResult { ValidationError = "End time must be after start time." };
        }

        if (roomId.HasValue && await _roomRepository.GetByIdAsync(roomId.Value) == null)
        {
            return new LessonSaveResult { ValidationError = "Room not found." };
        }

        if (teacherId.HasValue && await _teacherRepository.GetByIdAsync(teacherId.Value) == null)
        {
            return new LessonSaveResult { ValidationError = "Teacher not found." };
        }

        var conflicts = new List<LessonConflict>();
        if (status != LessonStatus.Cancelled)
        {
            var candidate = new ConflictCandidate
            {
                SubjectGroupId = lesson.SubjectGroupId,
                TeacherId = teacherId,
                RoomId = roomId,
                Date = date,
                StartTime = startTime,
                EndTime = endTime
            };

            conflicts = await FindConflictsAsync(new List<ConflictCandidate> { candidate }, new HashSet<int> { lessonId });
            if (conflicts.Count > 0 && !force)
            {
                return new LessonSaveResult { Conflicts = conflicts };
            }
        }

        lesson.Date = date;
        lesson.StartTime = startTime;
        lesson.EndTime = endTime;
        lesson.RoomId = roomId;
        lesson.TeacherId = teacherId;
        lesson.Note = note;
        lesson.Status = status;
        lesson.IsModified = true;

        await _lessonRepository.UpdateAsync(lesson);

        return new LessonSaveResult { Lesson = lesson, Conflicts = conflicts };
    }

    private static string? ValidateRuleInput(
        TimeOnly startTime, TimeOnly endTime, DateOnly startDate, DateOnly endDate,
        DateOnly termStart, DateOnly termEnd)
    {
        if (endTime <= startTime)
        {
            return "End time must be after start time.";
        }
        if (endDate < startDate)
        {
            return "End date must not be before start date.";
        }
        if (startDate < termStart || endDate > termEnd)
        {
            return "Rule dates must lie within the subject group's term.";
        }
        return null;
    }

    private static IEnumerable<DateOnly> GenerateOccurrences(DateOnly start, DateOnly end, DayOfWeek dayOfWeek)
    {
        if (end < start) yield break;

        var offset = ((int)dayOfWeek - (int)start.DayOfWeek + 7) % 7;
        for (var date = start.AddDays(offset); date <= end; date = date.AddDays(7))
        {
            yield return date;
        }
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync()
    {
        if (!_context.Database.IsRelational())
        {
            return null;
        }
        return await _context.Database.BeginTransactionAsync();
    }

    private async Task<List<LessonConflict>> FindConflictsAsync(IReadOnlyList<ConflictCandidate> candidates, ISet<int> excludeLessonIds)
    {
        var conflicts = new List<LessonConflict>();
        if (candidates.Count == 0) return conflicts;

        var minDate = candidates.Min(c => c.Date);
        var maxDate = candidates.Max(c => c.Date);

        var existingLessons = (await _lessonRepository.GetAllAsync(minDate, maxDate, null, null, null))
            .Where(l => l.Status != LessonStatus.Cancelled && !excludeLessonIds.Contains(l.Id))
            .ToList();

        var groupIds = candidates.Select(c => c.SubjectGroupId)
            .Concat(existingLessons.Select(l => l.SubjectGroupId))
            .Distinct()
            .ToList();

        var studentsByGroup = new Dictionary<int, Dictionary<int, string>>();
        foreach (var groupId in groupIds)
        {
            var enrollments = await _enrollmentRepository.GetBySubjectGroupIdAsync(groupId, includeHistorical: false);
            studentsByGroup[groupId] = enrollments.ToDictionary(e => e.StudentId, e => ComposeName(e.Student.Name));
        }

        foreach (var candidate in candidates)
        {
            var overlapping = existingLessons.Where(l =>
                l.Date == candidate.Date &&
                TimesOverlap(l.StartTime, l.EndTime, candidate.StartTime, candidate.EndTime));

            foreach (var existing in overlapping)
            {
                if (candidate.TeacherId.HasValue && existing.TeacherId == candidate.TeacherId.Value)
                {
                    conflicts.Add(new LessonConflict
                    {
                        Type = ConflictType.Teacher,
                        LessonId = existing.Id,
                        SubjectGroupId = existing.SubjectGroupId,
                        SubjectGroupName = existing.SubjectGroup.Name,
                        Date = existing.Date,
                        StartTime = existing.StartTime,
                        EndTime = existing.EndTime,
                        Detail = existing.Teacher != null ? ComposeName(existing.Teacher.Name) : null
                    });
                }

                if (candidate.RoomId.HasValue && existing.RoomId == candidate.RoomId.Value)
                {
                    conflicts.Add(new LessonConflict
                    {
                        Type = ConflictType.Room,
                        LessonId = existing.Id,
                        SubjectGroupId = existing.SubjectGroupId,
                        SubjectGroupName = existing.SubjectGroup.Name,
                        Date = existing.Date,
                        StartTime = existing.StartTime,
                        EndTime = existing.EndTime,
                        Detail = existing.Room?.Name
                    });
                }

                if (existing.SubjectGroupId != candidate.SubjectGroupId &&
                    studentsByGroup.TryGetValue(candidate.SubjectGroupId, out var candidateStudents) &&
                    studentsByGroup.TryGetValue(existing.SubjectGroupId, out var existingStudents))
                {
                    var overlappingNames = candidateStudents.Keys.Intersect(existingStudents.Keys)
                        .Select(id => existingStudents[id])
                        .ToList();

                    if (overlappingNames.Count > 0)
                    {
                        conflicts.Add(new LessonConflict
                        {
                            Type = ConflictType.Student,
                            LessonId = existing.Id,
                            SubjectGroupId = existing.SubjectGroupId,
                            SubjectGroupName = existing.SubjectGroup.Name,
                            Date = existing.Date,
                            StartTime = existing.StartTime,
                            EndTime = existing.EndTime,
                            Detail = string.Join(", ", overlappingNames)
                        });
                    }
                }
            }
        }

        return conflicts;
    }

    private static bool TimesOverlap(TimeOnly aStart, TimeOnly aEnd, TimeOnly bStart, TimeOnly bEnd)
        => aStart < bEnd && bStart < aEnd;

    private static string ComposeName(PersonName name)
        => string.IsNullOrWhiteSpace(name.Nickname) ? $"{name.FirstName} {name.LastName}" : $"{name.FirstName} {name.LastName} ({name.Nickname})";

    private class ConflictCandidate
    {
        public required int SubjectGroupId { get; init; }
        public int? TeacherId { get; init; }
        public int? RoomId { get; init; }
        public required DateOnly Date { get; init; }
        public required TimeOnly StartTime { get; init; }
        public required TimeOnly EndTime { get; init; }
    }
}
