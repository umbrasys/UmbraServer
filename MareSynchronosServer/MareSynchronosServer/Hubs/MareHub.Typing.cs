using System;
using System.Linq;
using System.Threading.Tasks;
using MareSynchronos.API.Data.Enum;
using MareSynchronos.API.Data.Extensions;
using MareSynchronos.API.Dto.User;
using MareSynchronosServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MareSynchronosServer.Hubs;

public partial class MareHub
{
    [Authorize(Policy = "Identified")]
    public async Task UserSetTypingState(bool isTyping)
    {
        _logger.LogCallInfo(MareHubLogger.Args(isTyping));

        var pairedEntries = await GetAllPairedClientsWithPauseState().ConfigureAwait(false);

        var sender = await DbContext.Users.AsNoTracking()
            .SingleAsync(u => u.UID == UserUID)
            .ConfigureAwait(false);

        var typingDto = new TypingStateDto(sender.ToUserData(), isTyping, TypingScope.Proximity);

        await Clients.Caller.Client_UserTypingState(typingDto).ConfigureAwait(false);

        var recipients = pairedEntries
            .Where(p => !p.IsPaused)
            .Select(p => p.UID)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (recipients.Count == 0)
            return;

        await Clients.Users(recipients).Client_UserTypingState(typingDto).ConfigureAwait(false);
    }

    [Authorize(Policy = "Identified")]
    public async Task UserSetTypingState(bool isTyping, TypingScope scope)
    {
        _logger.LogCallInfo(MareHubLogger.Args(isTyping, scope));

        var sender = await DbContext.Users.AsNoTracking()
            .SingleAsync(u => u.UID == UserUID)
            .ConfigureAwait(false);

        var typingDto = new TypingStateDto(sender.ToUserData(), isTyping, scope);

        await Clients.Caller.Client_UserTypingState(typingDto).ConfigureAwait(false);

        if (scope == TypingScope.Party || scope == TypingScope.CrossParty)
        {
            _logger.LogCallInfo(MareHubLogger.Args("Typing scope is party-based; server-side party routing not yet implemented, not broadcasting to non-caller."));
            return;
        }

        var pairedEntries = await GetAllPairedClientsWithPauseState().ConfigureAwait(false);
        var recipients = pairedEntries
            .Where(p => !p.IsPaused)
            .Select(p => p.UID)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (recipients.Count == 0) return;

        await Clients.Users(recipients).Client_UserTypingState(typingDto).ConfigureAwait(false);
    }
}
