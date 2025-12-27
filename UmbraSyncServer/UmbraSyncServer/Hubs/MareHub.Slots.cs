using UmbraSync.API.Dto.Slot;
using UmbraSync.API.Dto.Group;
using MareSynchronosServer.Utils;
using MareSynchronosShared.Models;
using MareSynchronosShared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace MareSynchronosServer.Hubs;

public partial class MareHub
{
    [Authorize(Policy = "Identified")]
    public async Task<SlotInfoResponseDto?> SlotGetInfo(SlotLocationDto location)
    {
        _logger.LogCallInfo(MareHubLogger.Args(location));

        var slot = await DbContext.Slots
            .Include(s => s.Group)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ServerId == location.ServerId 
                                   && s.TerritoryId == location.TerritoryId 
                                   && s.WardId == location.WardId 
                                   && s.PlotId == location.PlotId)
            .ConfigureAwait(false);

        return slot?.ToSlotInfoDto();
    }

    [Authorize(Policy = "Identified")]
    public async Task<SlotInfoResponseDto?> SlotGetNearby(uint serverId, uint territoryId, float x, float y, float z)
    {
        _logger.LogCallInfo(MareHubLogger.Args(serverId, territoryId, x, y, z));

        // On récupère tous les slots du territoire pour filtrer par distance
        // En pratique il n'y en a pas des milliers par territoire
        var slots = await DbContext.Slots
            .Include(s => s.Group)
            .AsNoTracking()
            .Where(s => s.ServerId == serverId && s.TerritoryId == territoryId)
            .ToListAsync()
            .ConfigureAwait(false);

        var playerPos = new Vector3(x, y, z);
        
        // On cherche le slot le plus proche dont le rayon englobe le joueur
        var nearbySlot = slots
            .Where(s => Vector3.Distance(new Vector3(s.X, s.Y, s.Z), playerPos) <= s.Radius)
            .OrderBy(s => Vector3.Distance(new Vector3(s.X, s.Y, s.Z), playerPos))
            .FirstOrDefault();

        return nearbySlot?.ToSlotInfoDto();
    }

    [Authorize(Policy = "Identified")]
    public async Task<List<SlotInfoResponseDto>> SlotGetInfoForGroup(GroupDto groupDto)
    {
        _logger.LogCallInfo(MareHubLogger.Args(groupDto));

        var (hasRights, _) = await TryValidateGroupModeratorOrOwner(groupDto.Group.GID).ConfigureAwait(false);
        if (!hasRights) return new();

        var slots = await DbContext.Slots
            .Include(s => s.Group)
            .AsNoTracking()
            .Where(s => s.GroupGID == groupDto.Group.GID)
            .ToListAsync()
            .ConfigureAwait(false);

        return slots.Select(s => s.ToSlotInfoDto()).ToList();
    }

    [Authorize(Policy = "Identified")]
    public async Task<bool> SlotUpdate(SlotUpdateRequestDto request)
    {
        _logger.LogCallInfo(MareHubLogger.Args(request));

        var (hasRights, group) = await TryValidateGroupModeratorOrOwner(request.Group.GID).ConfigureAwait(false);
        if (!hasRights) return false;

        if (request.IsDelete)
        {
            var slotToDelete = await DbContext.Slots.FirstOrDefaultAsync(s => s.SlotId == request.SlotId && s.GroupGID == request.Group.GID).ConfigureAwait(false);
            if (slotToDelete != null)
            {
                DbContext.Slots.Remove(slotToDelete);
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogCallInfo(MareHubLogger.Args(request, "Deleted"));
                return true;
            }
            return false;
        }

        Slot? slot;
        if (request.SlotId == Guid.Empty)
        {
            slot = new Slot
            {
                GroupGID = request.Group.GID
            };
            DbContext.Slots.Add(slot);
        }
        else
        {
            slot = await DbContext.Slots.FirstOrDefaultAsync(s => s.SlotId == request.SlotId && s.GroupGID == request.Group.GID).ConfigureAwait(false);
            if (slot == null) return false;
        }

        slot.SlotName = request.SlotName;
        slot.SlotDescription = request.SlotDescription;
        if (request.Location != null)
        {
            slot.ServerId = request.Location.ServerId;
            slot.TerritoryId = request.Location.TerritoryId;
            slot.WardId = request.Location.WardId;
            slot.PlotId = request.Location.PlotId;
            slot.X = request.Location.X;
            slot.Y = request.Location.Y;
            slot.Z = request.Location.Z;
            slot.Radius = request.Location.Radius;
        }

        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        _logger.LogCallInfo(MareHubLogger.Args(request, "Success"));
        return true;
    }
}
