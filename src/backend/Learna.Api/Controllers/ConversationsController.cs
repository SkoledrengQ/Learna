using System.Security.Claims;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/conversations")]
public sealed class ConversationsController(IConversationRepository conversations, IUserRepository users, IMessagingPolicyRepository policy, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ConversationListDto>> My()
    {
        var user = await CurrentUserAsync();
        var list = await conversations.GetActiveForUserAsync(user.Id);
        var unreadCounts = await conversations.GetUnreadCountsAsync(user.Id);
        var lastMessages = await conversations.GetLastMessagesAsync(list.Select(c => c.Id));
        var rows = list.Select(c => ToSummaryDto(c, user.Id, unreadCounts, lastMessages))
            .OrderByDescending(r => r.LastMessageAt ?? DateTime.MinValue)
            .ToList();
        return Ok(new ConversationListDto(rows, unreadCounts.Values.Sum()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConversationDetailDto>> Get(int id)
    {
        var conversation = await conversations.GetByIdAsync(id);
        if (conversation == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!CanRead(conversation, user.Id, User.IsInRole("Admin"))) return Forbid();
        return Ok(ToDetailDto(conversation, user.Id));
    }

    [HttpPost]
    public async Task<ActionResult<ConversationSummaryDto>> Create(CreateConversationDto dto)
    {
        var user = await CurrentUserAsync();
        var userRoles = MessagingAccess.RolesOf(user);

        if (dto.Type == ConversationType.Direct)
        {
            if (dto.UserId is not int targetId || targetId == user.Id) return BadRequest(new ConversationErrorDto("INVALID_CONVERSATION"));
            var target = await users.GetByIdAsync(targetId);
            if (target == null) return BadRequest(new ConversationErrorDto("INVALID_CONVERSATION"));

            var existing = await conversations.FindDirectAsync(user.Id, targetId);
            if (existing != null) return Ok(await ToSummaryAsync(existing, user.Id));

            if (!await MessagingAccess.IsPairAllowedAsync(userRoles, MessagingAccess.RolesOf(target), policy))
                return StatusCode(StatusCodes.Status403Forbidden, new ConversationErrorDto("POLICY_BLOCKED"));

            var now = Now;
            var conversation = new Conversation { Type = ConversationType.Direct, CreatedByUserId = user.Id, CreatedByUser = user, CreatedAt = now };
            var created = await conversations.AddAsync(conversation);
            await conversations.AddParticipantAsync(created.Id, user.Id, now);
            await conversations.AddParticipantAsync(created.Id, targetId, now);
            var reloaded = (await conversations.GetByIdAsync(created.Id))!;
            return CreatedAtAction(nameof(Get), new { id = created.Id }, await ToSummaryAsync(reloaded, user.Id));
        }

        if (string.IsNullOrWhiteSpace(dto.Title) || dto.UserIds == null || dto.UserIds.Count == 0)
            return BadRequest(new ConversationErrorDto("INVALID_CONVERSATION"));

        var memberIds = dto.UserIds.Where(id => id != user.Id).Distinct().ToList();
        var members = new List<User>();
        foreach (var id in memberIds)
        {
            var member = await users.GetByIdAsync(id);
            if (member == null) return BadRequest(new ConversationErrorDto("INVALID_CONVERSATION"));
            members.Add(member);
        }

        var roleSets = new List<IReadOnlyList<string>> { userRoles };
        roleSets.AddRange(members.Select(MessagingAccess.RolesOf));
        if (!await MessagingAccess.AllPairsAllowedAsync(roleSets, policy))
            return StatusCode(StatusCodes.Status403Forbidden, new ConversationErrorDto("POLICY_BLOCKED"));

        var groupNow = Now;
        var group = new Conversation { Type = ConversationType.Group, Title = dto.Title!.Trim(), CreatedByUserId = user.Id, CreatedByUser = user, CreatedAt = groupNow };
        var createdGroup = await conversations.AddAsync(group);
        await conversations.AddParticipantAsync(createdGroup.Id, user.Id, groupNow);
        foreach (var id in memberIds) await conversations.AddParticipantAsync(createdGroup.Id, id, groupNow);
        var reloadedGroup = (await conversations.GetByIdAsync(createdGroup.Id))!;
        return CreatedAtAction(nameof(Get), new { id = createdGroup.Id }, await ToSummaryAsync(reloadedGroup, user.Id));
    }

    [HttpGet("{id:int}/messages")]
    public async Task<ActionResult<MessagePageDto>> Messages(int id, [FromQuery] int? before, [FromQuery] int limit = 30)
    {
        var conversation = await conversations.GetByIdAsync(id);
        if (conversation == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!CanRead(conversation, user.Id, User.IsInRole("Admin"))) return Forbid();

        var take = Math.Clamp(limit, 1, 100);
        var rows = await conversations.GetMessagesAsync(id, before, take + 1);
        var hasMore = rows.Count > take;
        var page = rows.Take(take).Reverse().Select(m => ToMessageDto(m, user.Id)).ToList();
        return Ok(new MessagePageDto(page, hasMore));
    }

    [HttpPost("{id:int}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(int id, SendMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Body)) return BadRequest(new ConversationErrorDto("INVALID_MESSAGE"));
        var conversation = await conversations.GetByIdAsync(id);
        if (conversation == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!CanSend(conversation, user.Id)) return Forbid();

        if (conversation.Type == ConversationType.Direct)
        {
            var other = conversation.Participants.FirstOrDefault(p => p.UserId != user.Id);
            if (other != null && !await MessagingAccess.IsPairAllowedAsync(MessagingAccess.RolesOf(user), MessagingAccess.RolesOf(other.User), policy))
                return StatusCode(StatusCodes.Status403Forbidden, new ConversationErrorDto("POLICY_BLOCKED"));
        }

        var now = Now;
        var message = new Message { ConversationId = id, SenderUserId = user.Id, SenderUser = user, Body = dto.Body.Trim(), SentAt = now };
        await conversations.AddMessageAsync(message);
        await conversations.MarkReadAsync(id, user.Id, now);
        return CreatedAtAction(nameof(Messages), new { id }, ToMessageDto(message, user.Id));
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var conversation = await conversations.GetByIdAsync(id);
        if (conversation == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!CanSend(conversation, user.Id)) return Forbid();
        await conversations.MarkReadAsync(id, user.Id, Now);
        return NoContent();
    }

    [HttpPost("{id:int}/participants")]
    public async Task<ActionResult<ConversationDetailDto>> AddParticipants(int id, AddParticipantsDto dto)
    {
        var conversation = await conversations.GetByIdAsync(id);
        if (conversation == null) return NotFound();
        var user = await CurrentUserAsync();
        if (conversation.Type != ConversationType.Group) return BadRequest(new ConversationErrorDto("INVALID_CONVERSATION"));
        if (conversation.CreatedByUserId != user.Id) return Forbid();
        if (dto.UserIds == null || dto.UserIds.Count == 0) return BadRequest(new ConversationErrorDto("INVALID_CONVERSATION"));

        var activeParticipants = conversation.Participants.Where(p => p.LeftAt == null).ToList();
        var newUsers = new List<User>();
        foreach (var candidateId in dto.UserIds.Distinct())
        {
            if (activeParticipants.Any(p => p.UserId == candidateId)) continue;
            var candidate = await users.GetByIdAsync(candidateId);
            if (candidate == null) return BadRequest(new ConversationErrorDto("INVALID_CONVERSATION"));
            newUsers.Add(candidate);
        }
        if (newUsers.Count == 0) return Ok(ToDetailDto(conversation, user.Id));

        var roleSets = activeParticipants.Select(p => MessagingAccess.RolesOf(p.User)).Concat(newUsers.Select(MessagingAccess.RolesOf)).ToList();
        if (!await MessagingAccess.AllPairsAllowedAsync(roleSets, policy))
            return StatusCode(StatusCodes.Status403Forbidden, new ConversationErrorDto("POLICY_BLOCKED"));

        var now = Now;
        foreach (var newUser in newUsers) await conversations.AddParticipantAsync(id, newUser.Id, now);
        var reloaded = (await conversations.GetByIdAsync(id))!;
        return Ok(ToDetailDto(reloaded, user.Id));
    }

    [HttpDelete("{id:int}/participants/{userId:int}")]
    public async Task<IActionResult> RemoveParticipant(int id, int userId)
    {
        var conversation = await conversations.GetByIdAsync(id);
        if (conversation == null) return NotFound();
        var user = await CurrentUserAsync();
        if (conversation.Type != ConversationType.Group) return BadRequest(new ConversationErrorDto("INVALID_CONVERSATION"));
        var isSelf = userId == user.Id;
        if (!isSelf && conversation.CreatedByUserId != user.Id) return Forbid();

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId && p.LeftAt == null);
        if (participant == null) return NotFound();
        await conversations.RemoveParticipantAsync(participant, Now);
        return NoContent();
    }

    private static bool CanRead(Conversation c, int userId, bool isAdmin) => isAdmin || CanSend(c, userId);
    private static bool CanSend(Conversation c, int userId) => c.Participants.Any(p => p.UserId == userId && p.LeftAt == null);

    private async Task<ConversationSummaryDto> ToSummaryAsync(Conversation c, int callerId)
    {
        var lastMessages = await conversations.GetLastMessagesAsync(new[] { c.Id });
        var unreadCounts = await conversations.GetUnreadCountsAsync(callerId);
        return ToSummaryDto(c, callerId, unreadCounts, lastMessages);
    }

    private static ConversationSummaryDto ToSummaryDto(Conversation c, int callerId, IReadOnlyDictionary<int, int> unreadCounts, IReadOnlyDictionary<int, Message> lastMessages)
    {
        var participantNames = c.Participants.Where(p => p.LeftAt == null && p.UserId != callerId).Select(p => PersonDisplay.ForUser(p.User)).ToList();
        lastMessages.TryGetValue(c.Id, out var last);
        unreadCounts.TryGetValue(c.Id, out var unread);
        var preview = last?.Body != null && last.Body.Length > 140 ? last.Body[..140] + "…" : last?.Body;
        return new ConversationSummaryDto(c.Id, c.Type, DisplayTitle(c, callerId), participantNames, preview, last?.SentAt, unread, c.CreatedByUserId == callerId);
    }

    private static ConversationDetailDto ToDetailDto(Conversation c, int callerId)
    {
        var activeParticipants = c.Participants.Where(p => p.LeftAt == null).ToList();
        var participantDtos = activeParticipants
            .Select(p => new ParticipantDto(p.UserId, PersonDisplay.ForUser(p.User), MessagingAccess.RolesOf(p.User), p.JoinedAt, p.LeftAt, p.UserId == c.CreatedByUserId))
            .ToList();
        return new ConversationDetailDto(c.Id, c.Type, DisplayTitle(c, callerId), c.CreatedByUserId, c.CreatedByUserId == callerId, participantDtos);
    }

    private static string DisplayTitle(Conversation c, int callerId)
    {
        if (c.Type == ConversationType.Group) return c.Title ?? "";
        var other = c.Participants.FirstOrDefault(p => p.UserId != callerId) ?? c.Participants.FirstOrDefault();
        return other != null ? PersonDisplay.ForUser(other.User) : "";
    }

    private static MessageDto ToMessageDto(Message m, int callerId) => new(m.Id, m.SenderUserId, PersonDisplay.ForUser(m.SenderUser), m.Body, m.SentAt, m.SenderUserId == callerId);

    private async Task<User> CurrentUserAsync() => (await users.GetByIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))!;
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
}
