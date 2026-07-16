using System.Security.Claims;
using FluentAssertions;
using Learna.Api.Controllers;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Infrastructure.Data;
using Learna.Infrastructure.Repositories;
using Learna.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace Learna.Tests;

public class MessagingTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static TokenService Tokens() => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:SecretKey"] = "a-development-test-secret-that-is-at-least-32-characters",
        ["Jwt:Issuer"] = "LearnaApi",
        ["Jwt:Audience"] = "LearnaApp"
    }).Build());

    [Fact]
    public void Model_EnforcesParticipantUniquePairAndPolicyUniqueIndex()
    {
        using var db = Context(); var model = db.GetService<IDesignTimeModel>().Model;
        model.FindEntityType(typeof(ConversationParticipant))!.FindPrimaryKey()!.Properties.Select(p => p.Name).Should().Equal("ConversationId", "UserId");
        model.FindEntityType(typeof(MessagingPolicyRule))!.GetIndexes().Single(i => i.IsUnique).Properties.Select(p => p.Name).Should().Equal("RoleA", "RoleB");
    }

    [Fact]
    public void DirectoryDto_ExposesNoEmailOrPii()
    {
        typeof(DirectoryPersonDto).GetProperties().Select(p => p.Name).Should().NotContain(["Email", "PhoneNumber"]);
    }

    [Fact]
    public async Task DirectConversation_DedupesAndPolicyGatesCreateAndSend()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));

        var studentA = Controller(db, seed.StudentAUser, clock, "Student");
        var created = Value(await studentA.Create(new CreateConversationDto(ConversationType.Direct, seed.TeacherUser.Id, null, null)));

        var teacher = Controller(db, seed.TeacherUser, clock, "Teacher");
        var reopened = Value(await teacher.Create(new CreateConversationDto(ConversationType.Direct, seed.StudentAUser.Id, null, null)));
        reopened.Id.Should().Be(created.Id);

        await SetPolicy(db, "Student", "Teacher", false, clock.GetUtcNow().UtcDateTime);

        var studentB = Controller(db, seed.StudentBUser, clock, "Student");
        AssertPolicyBlocked((await studentB.Create(new CreateConversationDto(ConversationType.Direct, seed.TeacherUser.Id, null, null))).Result!);

        // Re-initiating an already-existing thread still dedupes even while the pair is blocked for NEW conversations.
        var stillDeduped = Value(await studentA.Create(new CreateConversationDto(ConversationType.Direct, seed.TeacherUser.Id, null, null)));
        stillDeduped.Id.Should().Be(created.Id);

        AssertPolicyBlocked((await studentA.SendMessage(created.Id, new SendMessageDto("สวัสดีครับ"))).Result!);

        await SetPolicy(db, "Student", "Teacher", true, clock.GetUtcNow().UtcDateTime);
        var sent = Value(await studentA.SendMessage(created.Id, new SendMessageDto("สวัสดีครับ")));
        sent.Body.Should().Be("สวัสดีครับ");
    }

    [Fact]
    public async Task MultiRoleUser_AnyAllowedPair_EscapesBlockedPolicy()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        await SetPolicy(db, "Student", "Student", false, clock.GetUtcNow().UtcDateTime);

        var studentA = Controller(db, seed.StudentAUser, clock, "Student");
        AssertPolicyBlocked((await studentA.Create(new CreateConversationDto(ConversationType.Direct, seed.StudentBUser.Id, null, null))).Result!);

        var adminRole = await db.Roles.SingleAsync(r => r.Name == "Admin");
        db.UserRoles.Add(new UserRole { UserId = seed.StudentAUser.Id, RoleId = adminRole.Id, AssignedAt = clock.GetUtcNow().UtcDateTime });
        await db.SaveChangesAsync();

        var created = Value(await studentA.Create(new CreateConversationDto(ConversationType.Direct, seed.StudentBUser.Id, null, null)));
        created.Should().NotBeNull();
    }

    [Fact]
    public async Task GroupMembership_EnforcesAllPairwiseCombinations_AtCreateAndAdd_ButNotAtSend()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        await SetPolicy(db, "Student", "Student", false, clock.GetUtcNow().UtcDateTime);

        var teacher = Controller(db, seed.TeacherUser, clock, "Teacher");
        AssertPolicyBlocked((await teacher.Create(new CreateConversationDto(ConversationType.Group, null, "กลุ่มโปรเจกต์วิทยาศาสตร์", [seed.StudentAUser.Id, seed.StudentBUser.Id]))).Result!);

        var group = Value(await teacher.Create(new CreateConversationDto(ConversationType.Group, null, "กลุ่มโปรเจกต์วิทยาศาสตร์", [seed.StudentAUser.Id])));
        group.DisplayTitle.Should().Be("กลุ่มโปรเจกต์วิทยาศาสตร์");

        AssertPolicyBlocked((await teacher.AddParticipants(group.Id, new AddParticipantsDto([seed.StudentBUser.Id]))).Result!);

        await SetPolicy(db, "Student", "Student", true, clock.GetUtcNow().UtcDateTime);
        var afterAdd = Value(await teacher.AddParticipants(group.Id, new AddParticipantsDto([seed.StudentBUser.Id])));
        afterAdd.Participants.Select(p => p.UserId).Should().Contain([seed.StudentAUser.Id, seed.StudentBUser.Id]);

        // Group membership is not policy-rechecked on send, even once a previously-fine pair is blocked again.
        await SetPolicy(db, "Student", "Student", false, clock.GetUtcNow().UtcDateTime);
        var studentA = Controller(db, seed.StudentAUser, clock, "Student");
        var sentMessage = Value(await studentA.SendMessage(group.Id, new SendMessageDto("hello")));
        sentMessage.Body.Should().Be("hello");
    }

    [Fact]
    public async Task GroupLifecycle_LeaveLosesAccess_GroupKeepsFunctioning_CreatorReAdds()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        var teacher = Controller(db, seed.TeacherUser, clock, "Teacher");
        var group = Value(await teacher.Create(new CreateConversationDto(ConversationType.Group, null, "กลุ่มโปรเจกต์วิทยาศาสตร์", [seed.StudentAUser.Id, seed.StudentBUser.Id])));

        var studentB = Controller(db, seed.StudentBUser, clock, "Student");
        Value(await studentB.SendMessage(group.Id, new SendMessageDto("ก่อนออก")));

        (await studentB.RemoveParticipant(group.Id, seed.StudentBUser.Id)).Should().BeOfType<NoContentResult>();

        (await studentB.Get(group.Id)).Result.Should().BeOfType<ForbidResult>();
        (await studentB.SendMessage(group.Id, new SendMessageDto("หลังออก"))).Result.Should().BeOfType<ForbidResult>();

        var studentA = Controller(db, seed.StudentAUser, clock, "Student");
        Value(await studentA.SendMessage(group.Id, new SendMessageDto("กลุ่มยังทำงานได้")));

        (await teacher.AddParticipants(group.Id, new AddParticipantsDto([seed.StudentBUser.Id]))).Result.Should().BeOfType<OkObjectResult>();
        var history = Value(await studentB.Messages(group.Id, null, 10));
        history.Messages.Select(m => m.Body).Should().Contain(["ก่อนออก", "กลุ่มยังทำงานได้"]);
    }

    [Fact]
    public async Task AdminCanReadNotSend_NonParticipantForbidden()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        var teacher = Controller(db, seed.TeacherUser, clock, "Teacher");
        var conversation = Value(await teacher.Create(new CreateConversationDto(ConversationType.Direct, seed.StudentAUser.Id, null, null)));
        Value(await teacher.SendMessage(conversation.Id, new SendMessageDto("hi")));

        var admin = Controller(db, seed.AdminUser, clock, "Admin");
        (await admin.Get(conversation.Id)).Result.Should().BeOfType<OkObjectResult>();
        (await admin.Messages(conversation.Id, null, 10)).Result.Should().BeOfType<OkObjectResult>();
        (await admin.SendMessage(conversation.Id, new SendMessageDto("intercepted"))).Result.Should().BeOfType<ForbidResult>();

        var outsider = Controller(db, seed.GuardianUser, clock, "Parent");
        (await outsider.Get(conversation.Id)).Result.Should().BeOfType<ForbidResult>();
        (await outsider.Messages(conversation.Id, null, 10)).Result.Should().BeOfType<ForbidResult>();
        (await outsider.SendMessage(conversation.Id, new SendMessageDto("x"))).Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UnreadCounts_TrackLastReadAt_OwnMessagesNeverUnread_TotalBadgeSums()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        var teacher = Controller(db, seed.TeacherUser, clock, "Teacher");
        var conversation = Value(await teacher.Create(new CreateConversationDto(ConversationType.Direct, seed.StudentAUser.Id, null, null)));
        Value(await teacher.SendMessage(conversation.Id, new SendMessageDto("one")));
        clock.Set(clock.GetUtcNow().AddMinutes(1));
        Value(await teacher.SendMessage(conversation.Id, new SendMessageDto("two")));

        var studentA = Controller(db, seed.StudentAUser, clock, "Student");
        var feed = Value(await studentA.My());
        feed.Conversations.Single().UnreadCount.Should().Be(2);
        feed.TotalUnread.Should().Be(2);

        (await studentA.MarkRead(conversation.Id)).Should().BeOfType<NoContentResult>();
        Value(await studentA.My()).Conversations.Single().UnreadCount.Should().Be(0);

        clock.Set(clock.GetUtcNow().AddMinutes(1));
        Value(await studentA.SendMessage(conversation.Id, new SendMessageDto("three")));
        Value(await studentA.My()).Conversations.Single().UnreadCount.Should().Be(0);

        Value(await teacher.My()).Conversations.Single().UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task MessagePaging_NewestFirstPage_ChronologicalWithinPage_HasMoreFlagCorrect()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        var teacher = Controller(db, seed.TeacherUser, clock, "Teacher");
        var conversation = Value(await teacher.Create(new CreateConversationDto(ConversationType.Direct, seed.StudentAUser.Id, null, null)));
        for (var i = 1; i <= 5; i++)
        {
            Value(await teacher.SendMessage(conversation.Id, new SendMessageDto($"msg{i}")));
            clock.Set(clock.GetUtcNow().AddMinutes(1));
        }

        var page1 = Value(await teacher.Messages(conversation.Id, null, 2));
        page1.Messages.Select(m => m.Body).Should().Equal("msg4", "msg5");
        page1.HasMore.Should().BeTrue();

        var page2 = Value(await teacher.Messages(conversation.Id, page1.Messages[0].Id, 2));
        page2.Messages.Select(m => m.Body).Should().Equal("msg2", "msg3");
        page2.HasMore.Should().BeTrue();

        var page3 = Value(await teacher.Messages(conversation.Id, page2.Messages[0].Id, 2));
        page3.Messages.Select(m => m.Body).Should().Equal("msg1");
        page3.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Directory_ExcludesSelfAndBlockedRoles_SupportsThaiNicknameSearch()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        await SetPolicy(db, "Student", "Student", false, clock.GetUtcNow().UtcDateTime);

        var directory = new MessagingDirectoryController(new UserRepository(db), new MessagingPolicyRepository(db));
        SetUser(directory, seed.StudentAUser, ["Student"]);

        var results = Value(await directory.Search(null));
        results.Select(p => p.UserId).Should().NotContain(seed.StudentAUser.Id).And.NotContain(seed.StudentBUser.Id);
        results.Select(p => p.UserId).Should().Contain(seed.TeacherUser.Id);

        var byNickname = Value(await directory.Search("ครูดา"));
        byNickname.Should().ContainSingle(p => p.UserId == seed.TeacherUser.Id);
    }

    [Fact]
    public async Task GreetingName_PrefersNicknameThenFirstName_FallsBackToEmailForUnlinkedAdmin()
    {
        await using var db = Context();
        var student = new Student { Name = new PersonName { FirstName = "Somsri", LastName = "Dee", Nickname = "หนูดี" }, Email = "s@test", StudentId = "S1", IdCardNumber = "ID1", DateOfBirth = DateTime.Today, EnrollmentDate = DateTime.Today, PhoneNumber = "0", Address = "x" };
        var teacher = new Teacher { Name = new PersonName { FirstName = "Anong", LastName = "Chai" }, Email = "t@test" };
        db.AddRange(student, teacher);
        await db.SaveChangesAsync();
        var studentUser = new User { Email = "student-login@test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"), StudentId = student.Id };
        var teacherUser = new User { Email = "teacher-login@test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"), TeacherId = teacher.Id };
        var adminUser = new User { Email = "pure-admin@test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!") };
        db.AddRange(studentUser, teacherUser, adminUser);
        await db.SaveChangesAsync();
        var auth = new AuthService(new UserRepository(db), new RefreshTokenRepository(db), Tokens());

        (await auth.LoginAsync(studentUser.Email, "Password1!")).User!.GreetingName.Should().Be("หนูดี");
        (await auth.LoginAsync(teacherUser.Email, "Password1!")).User!.GreetingName.Should().Be("Anong");
        (await auth.LoginAsync(adminUser.Email, "Password1!")).User!.GreetingName.Should().Be("pure-admin@test");
    }

    private static void AssertPolicyBlocked(IActionResult result)
    {
        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(403);
        obj.Value.Should().BeEquivalentTo(new ConversationErrorDto("POLICY_BLOCKED"));
    }

    private static async Task SetPolicy(ApplicationDbContext db, string roleA, string roleB, bool allowed, DateTime now) =>
        await new MessagingPolicyRepository(db).ReplaceAllAsync([(roleA, roleB, allowed)], now);

    private static ConversationsController Controller(ApplicationDbContext db, User user, TimeProvider clock, params string[] roles)
    {
        var c = new ConversationsController(new ConversationRepository(db), new UserRepository(db), new MessagingPolicyRepository(db), clock);
        SetUser(c, user, roles); return c;
    }

    private static void SetUser(ControllerBase c, User user, IEnumerable<string> roles) { var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()) }; claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r))); c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) } }; }
    private static T Value<T>(ActionResult<T> result) => result.Value ?? (T)((ObjectResult)result.Result!).Value!;

    private sealed record SeedData(User AdminUser, User TeacherUser, User StudentAUser, User StudentBUser, User GuardianUser);

    private static async Task<SeedData> Seed(ApplicationDbContext db)
    {
        db.Roles.AddRange(
            new Role { Id = 1, Name = "Admin", Description = "Admin" },
            new Role { Id = 2, Name = "Teacher", Description = "Teacher" },
            new Role { Id = 3, Name = "Student", Description = "Student" },
            new Role { Id = 4, Name = "Parent", Description = "Parent" });
        await db.SaveChangesAsync();

        var teacher = new Teacher { Name = new PersonName { FirstName = "Somchai", LastName = "Srisuk", Nickname = "ครูดา" }, Email = "teacher@test" };
        var studentA = Student("A"); var studentB = Student("B");
        var guardian = new Guardian { Name = new PersonName { FirstName = "P", LastName = "One" } };
        db.AddRange(teacher, studentA, studentB, guardian);
        await db.SaveChangesAsync();

        var adminUser = new User { Email = "admin@test", PasswordHash = "x" };
        var teacherUser = new User { Email = "teacher-login@test", PasswordHash = "x", TeacherId = teacher.Id };
        var studentAUser = new User { Email = "a@test", PasswordHash = "x", StudentId = studentA.Id };
        var studentBUser = new User { Email = "b@test", PasswordHash = "x", StudentId = studentB.Id };
        var guardianUser = new User { Email = "guardian@test", PasswordHash = "x", GuardianId = guardian.Id };
        db.AddRange(adminUser, teacherUser, studentAUser, studentBUser, guardianUser);
        await db.SaveChangesAsync();

        var adminRole = await db.Roles.SingleAsync(r => r.Name == "Admin");
        var teacherRole = await db.Roles.SingleAsync(r => r.Name == "Teacher");
        var studentRole = await db.Roles.SingleAsync(r => r.Name == "Student");
        var parentRole = await db.Roles.SingleAsync(r => r.Name == "Parent");
        var now = DateTime.UtcNow;
        db.UserRoles.AddRange(
            new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id, AssignedAt = now },
            new UserRole { UserId = teacherUser.Id, RoleId = teacherRole.Id, AssignedAt = now },
            new UserRole { UserId = studentAUser.Id, RoleId = studentRole.Id, AssignedAt = now },
            new UserRole { UserId = studentBUser.Id, RoleId = studentRole.Id, AssignedAt = now },
            new UserRole { UserId = guardianUser.Id, RoleId = parentRole.Id, AssignedAt = now });
        await db.SaveChangesAsync();

        return new SeedData(adminUser, teacherUser, studentAUser, studentBUser, guardianUser);
    }

    private static Student Student(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Student" }, Email = $"{id}@test", StudentId = id, IdCardNumber = $"ID{id}", DateOfBirth = DateTime.Today, EnrollmentDate = DateTime.Today, PhoneNumber = "0", Address = "x" };

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider { private DateTimeOffset value = now; public override DateTimeOffset GetUtcNow() => value; public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc; public void Set(DateTimeOffset next) => value = next; }
}
