namespace BakaPlugin;

// Mirrors InfoProxyCommonList.CharacterData.OnlineStatus (API 15).
[Flags]
public enum MemberStatus : ulong
{
    Offline = 0,
    Disconnected = 1ul << 5,
    NotFound = 1ul << 9,
    OfflineExd = 1ul << 10,
    Busy = 1ul << 12,
    PvP = 1ul << 13,
    PlayingTripleTriad = 1ul << 14,
    ViewingCutscene = 1ul << 15,
    AwayFromKeyboard = 1ul << 17,
    CameraMode = 1ul << 18,
    RolePlaying = 1ul << 22,
    LookingForParty = 1ul << 23,
    WaitingForDutyFinder = 1ul << 25,
    RecruitingPartyMembers = 1ul << 26,
    InDuty = 1ul << 43,
    Online = 1ul << 47,
}

public enum ActivityKind { Available, Adventure, Away, Social }

public sealed record Member(ulong ContentId, string Name, string Job, string Location, string World, MemberStatus Status)
{
    public bool IsOnline => Status != MemberStatus.Offline &&
        (Status & (MemberStatus.OfflineExd | MemberStatus.Disconnected | MemberStatus.NotFound)) == 0;

    public (string Label, ActivityKind Kind) Activity => Status switch
    {
        var s when s.HasFlag(MemberStatus.AwayFromKeyboard) => ("Away from keyboard", ActivityKind.Away),
        var s when s.HasFlag(MemberStatus.ViewingCutscene) => ("Watching a cutscene", ActivityKind.Adventure),
        var s when s.HasFlag(MemberStatus.PvP) => ("In PvP", ActivityKind.Adventure),
        var s when s.HasFlag(MemberStatus.InDuty) => ("In a duty", ActivityKind.Adventure),
        var s when s.HasFlag(MemberStatus.WaitingForDutyFinder) => ("Queued for a duty", ActivityKind.Adventure),
        var s when s.HasFlag(MemberStatus.PlayingTripleTriad) => ("Playing Triple Triad", ActivityKind.Social),
        var s when s.HasFlag(MemberStatus.CameraMode) => ("In camera mode", ActivityKind.Social),
        var s when s.HasFlag(MemberStatus.RolePlaying) => ("Role-playing", ActivityKind.Social),
        var s when s.HasFlag(MemberStatus.RecruitingPartyMembers) => ("Recruiting a party", ActivityKind.Social),
        var s when s.HasFlag(MemberStatus.LookingForParty) => ("Looking for a party", ActivityKind.Social),
        var s when s.HasFlag(MemberStatus.Busy) => ("Busy", ActivityKind.Away),
        _ => ("Online", ActivityKind.Available),
    };
}

public sealed record RosterSnapshot(string CompanyName, Member[] Members, int? OnlineCount, int? TotalCount,
    DateTimeOffset? ReadAt, string Notice, bool CanRefresh)
{
    public static RosterSnapshot Empty(string notice) => new("", [], null, null, null, notice, false);
    public bool IsPartial => OnlineCount is { } count && Members.Length < count;
}

public static class RosterModel
{
    public static bool IsTargetCompany(string name, string world) =>
        name.Equals("Shelter", StringComparison.OrdinalIgnoreCase) && world.Equals("Raiden", StringComparison.OrdinalIgnoreCase);

    public static Member[] OnlineMembers(IEnumerable<Member> members) => members
        .Where(member => member.ContentId != 0 && !string.IsNullOrWhiteSpace(member.Name))
        .GroupBy(member => member.ContentId)
        .Select(group => group.Last())
        .Where(member => member.IsOnline)
        .OrderBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public static IEnumerable<Member> Filter(IEnumerable<Member> members, string query, ActivityKind? kind)
    {
        query = query.Trim();
        return members.Where(member => (kind == null || member.Activity.Kind == kind) &&
            (query.Length == 0 || new[] { member.Name, member.Job, member.Location, member.Activity.Label }
                .Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase))));
    }
}
