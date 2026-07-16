using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learna.Api.Controllers;

/// Admin oversight list. Opening a specific conversation for read-only viewing reuses
/// GET /api/conversations/{id} and GET /api/conversations/{id}/messages, which already
/// grant Admins read access to any conversation regardless of participation.
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/conversations")]
public sealed class AdminConversationsController(IConversationRepository conversations) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminConversationSummaryDto>>> Search([FromQuery] string? search)
    {
        var list = await conversations.SearchAllAsync(search);
        var lastMessages = await conversations.GetLastMessagesAsync(list.Select(c => c.Id));
        var rows = new List<AdminConversationSummaryDto>();
        foreach (var c in list)
        {
            lastMessages.TryGetValue(c.Id, out var last);
            var messageCount = await conversations.GetMessageCountAsync(c.Id);
            rows.Add(ToDto(c, last, messageCount));
        }
        return Ok(rows);
    }

    private static AdminConversationSummaryDto ToDto(Conversation c, Message? last, int messageCount)
    {
        var participantNames = c.Participants.Where(p => p.LeftAt == null).Select(p => PersonDisplay.ForUser(p.User)).ToList();
        var title = c.Type == ConversationType.Group ? c.Title ?? "" : string.Join(", ", participantNames);
        return new AdminConversationSummaryDto(c.Id, c.Type, title, participantNames, messageCount, last?.SentAt, c.CreatedAt);
    }
}
