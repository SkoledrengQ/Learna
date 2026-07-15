using Learna.Core.Entities;
using Learna.Core.Interfaces;

namespace Learna.Api.Controllers;

internal static class AnnouncementAccess
{
    public static bool IsActive(Announcement announcement, DateTime now) => announcement.PublishAt <= now && (!announcement.ExpiresAt.HasValue || announcement.ExpiresAt > now);

    public static async Task<bool> CanSeeAsync(Announcement announcement, User user, bool isAdmin, IAnnouncementRepository announcements, DateTime now)
    {
        if (isAdmin || announcement.CreatedByUserId == user.Id) return true;
        if (!IsActive(announcement, now)) return false;
        return await MatchesAudienceAsync(announcement, user, announcements);
    }

    public static async Task<bool> MatchesAudienceAsync(Announcement announcement, User user, IAnnouncementRepository announcements)
    {
        if (announcement.AudienceType == AnnouncementAudienceType.School) return true;
        if (!announcement.TargetId.HasValue) return false;
        if (user.StudentId is int studentId)
        {
            var ids = announcement.AudienceType == AnnouncementAudienceType.SchoolClass
                ? await announcements.ActiveClassIdsForStudentAsync(studentId)
                : await announcements.ActiveGroupIdsForStudentAsync(studentId);
            return ids.Contains(announcement.TargetId.Value);
        }
        if (user.TeacherId is int teacherId)
        {
            if (announcement.AudienceType == AnnouncementAudienceType.SchoolClass)
                return (await announcements.GetClassAsync(announcement.TargetId.Value))?.HomeroomTeacherId == teacherId;
            return (await announcements.GetSubjectGroupAsync(announcement.TargetId.Value))?.TeacherId == teacherId;
        }
        if (user.GuardianId is int guardianId)
        {
            var ids = announcement.AudienceType == AnnouncementAudienceType.SchoolClass
                ? await announcements.GuardianActiveClassIdsAsync(guardianId)
                : await announcements.GuardianActiveGroupIdsAsync(guardianId);
            return ids.Contains(announcement.TargetId.Value);
        }
        return false;
    }

    public static async Task<bool> CanTargetAsync(AnnouncementAudienceType audienceType, int? targetId, User user, bool isAdmin, IAnnouncementRepository announcements)
    {
        if (audienceType == AnnouncementAudienceType.School) return isAdmin && targetId == null;
        if (!targetId.HasValue) return false;
        if (isAdmin)
        {
            return audienceType == AnnouncementAudienceType.SchoolClass
                ? await announcements.GetClassAsync(targetId.Value) != null
                : await announcements.GetSubjectGroupAsync(targetId.Value) != null;
        }
        if (user.TeacherId is not int teacherId) return false;
        if (audienceType == AnnouncementAudienceType.SchoolClass)
            return (await announcements.GetClassAsync(targetId.Value))?.HomeroomTeacherId == teacherId;
        return (await announcements.GetSubjectGroupAsync(targetId.Value))?.TeacherId == teacherId;
    }
}
