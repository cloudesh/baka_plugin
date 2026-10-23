using Xunit;

namespace BakaPlugin.Tests;

public class RosterTests
{
    private static Member Member(ulong id = 1, string name = "Baka Member", MemberStatus status = MemberStatus.Online) =>
        new(id, name, "WHM", "Old Sharlayan", "Raiden", status);

    [Theory]
    [InlineData("Shelter", "Raiden", true)]
    [InlineData("SHELTER", "raiden", true)]
    [InlineData("Shelter", "Odin", false)]
    [InlineData("Another Company", "Raiden", false)]
    [InlineData("", "", false)]
    public void OnlyShelterOnRaidenMatches(string name, string world, bool expected) =>
        Assert.Equal(expected, RosterModel.IsTargetCompany(name, world));

    [Theory]
    [InlineData(MemberStatus.Offline)]
    [InlineData(MemberStatus.OfflineExd)]
    [InlineData(MemberStatus.Disconnected | MemberStatus.Online)]
    [InlineData(MemberStatus.NotFound | MemberStatus.Online)]
    public void OfflineAndDisconnectedMembersAreExcluded(MemberStatus status) =>
        Assert.Empty(RosterModel.OnlineMembers([Member(status: status)]));

    [Theory]
    [InlineData(MemberStatus.Online, "Online", ActivityKind.Available)]
    [InlineData(MemberStatus.InDuty | MemberStatus.Online, "In a duty", ActivityKind.Adventure)]
    [InlineData(MemberStatus.ViewingCutscene | MemberStatus.InDuty, "Watching a cutscene", ActivityKind.Adventure)]
    [InlineData(MemberStatus.AwayFromKeyboard | MemberStatus.InDuty, "Away from keyboard", ActivityKind.Away)]
    [InlineData(MemberStatus.WaitingForDutyFinder, "Queued for a duty", ActivityKind.Adventure)]
    [InlineData(MemberStatus.PvP | MemberStatus.InDuty, "In PvP", ActivityKind.Adventure)]
    [InlineData(MemberStatus.PlayingTripleTriad, "Playing Triple Triad", ActivityKind.Social)]
    [InlineData(MemberStatus.CameraMode, "In camera mode", ActivityKind.Social)]
    [InlineData(MemberStatus.RolePlaying, "Role-playing", ActivityKind.Social)]
    [InlineData(MemberStatus.RecruitingPartyMembers, "Recruiting a party", ActivityKind.Social)]
    [InlineData(MemberStatus.LookingForParty, "Looking for a party", ActivityKind.Social)]
    [InlineData(MemberStatus.Busy, "Busy", ActivityKind.Away)]
    public void ActivityReflectsReportedFlags(MemberStatus status, string label, ActivityKind kind) =>
        Assert.Equal((label, kind), Member(status: status).Activity);

    [Fact]
    public void CraftingJobDoesNotInventActivity()
    {
        var member = Member() with { Job = "CRP" };
        Assert.Equal("Online", member.Activity.Label);
    }

    [Fact]
    public void LatestDuplicateWinsIncludingLogout()
    {
        Member[] input = [Member(), Member(status: MemberStatus.Offline)];
        Assert.Empty(RosterModel.OnlineMembers(input));
    }

    [Fact]
    public void InvalidRowsAreExcludedAndNamesSorted()
    {
        Member[] input = [Member(4, "Zelda"), Member(3, "alpha"), Member(0), Member(2, " "), Member(4, "Zelda Updated")];
        Assert.Equal(new[] { "alpha", "Zelda Updated" }, RosterModel.OnlineMembers(input).Select(m => m.Name));
    }

    [Theory]
    [InlineData("  BAKA  ")]
    [InlineData("whm")]
    [InlineData("sharlayan")]
    [InlineData("onLiNe")]
    public void SearchMatchesNameJobLocationOrActivity(string query) =>
        Assert.Single(RosterModel.Filter([Member()], query, null));

    [Fact]
    public void ActivityAndSearchFiltersCombine()
    {
        Member[] input = [Member(), Member(2, "Other Member", MemberStatus.InDuty)];
        Assert.Single(RosterModel.Filter(input, "other", ActivityKind.Adventure));
        Assert.Empty(RosterModel.Filter(input, "baka", ActivityKind.Adventure));
    }

    [Fact]
    public void PartialRosterIsNotPresentedAsComplete()
    {
        var snapshot = new RosterSnapshot("Shelter", [Member()], 8, 100, DateTimeOffset.UtcNow, "", true);
        Assert.True(snapshot.IsPartial);
        Assert.False((snapshot with { OnlineCount = 1 }).IsPartial);
    }

    [Fact]
    public void UnavailableRosterIsDifferentFromNobodyOnline()
    {
        var snapshot = RosterSnapshot.Empty("Log in");
        Assert.Null(snapshot.OnlineCount);
        Assert.Null(snapshot.ReadAt);
        Assert.False(snapshot.CanRefresh);
        Assert.Empty(snapshot.Members);
    }
}
