using System.Text;
using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Helpers;

public static class ShareTextComposer
{
    public static string ComposeWorkerText(
        string dayLabel,
        IReadOnlyList<TurnoverEvent> events,
        Func<Guid, string> houseNameLookup)
    {
        var sb = new StringBuilder();
        sb.Append(dayLabel);

        foreach (var e in events)
        {
            sb.AppendLine();
            sb.Append($"{houseNameLookup(e.HouseId)} → {e.DisplayLabel}");
        }

        return sb.ToString();
    }

    public static string ComposeOwnerText(
        string dayLabel,
        string ownerName,
        IReadOnlyList<TurnoverEvent> events,
        IReadOnlyList<House> houses,
        Guid ownerId)
    {
        var ownerHouseIds = new HashSet<Guid>(
            houses.Where(h => h.OwnerId == ownerId).Select(h => h.Id));

        var matchingEvents = events.Where(e => ownerHouseIds.Contains(e.HouseId)).ToList();

        if (matchingEvents.Count == 0)
            return string.Empty;

        var houseLookup = houses.ToDictionary(h => h.Id, h => h.Name);

        var sb = new StringBuilder();
        sb.Append(dayLabel);
        sb.AppendLine();
        sb.Append(ownerName);

        foreach (var e in matchingEvents)
        {
            sb.AppendLine();
            sb.Append($"{houseLookup[e.HouseId]} → {e.DisplayLabel}");
        }

        return sb.ToString();
    }
}
