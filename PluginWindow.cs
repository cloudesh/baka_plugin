using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace BakaPlugin;

public sealed class PluginWindow : Window
{
    private static readonly Vector4 Paper = new(0.94f, 0.90f, 0.81f, 1);
    private static readonly Vector4 Card = new(0.98f, 0.95f, 0.88f, 1);
    private static readonly Vector4 Ink = new(0.20f, 0.22f, 0.21f, 1);
    private static readonly Vector4 Muted = new(0.43f, 0.41f, 0.35f, 1);
    private static readonly Vector4 Gold = new(0.59f, 0.44f, 0.23f, 1);
    private static readonly Vector4 Line = new(0.72f, 0.65f, 0.51f, 0.45f);
    private static readonly Vector4 Green = new(0.22f, 0.43f, 0.34f, 1);
    private readonly RosterService roster;
    private string search = "";
    private int selectedFilter;
    private static float Scale => ImGuiHelpers.GlobalScale;

    public PluginWindow(RosterService roster) : base("Baka Free Company###BakaRoster",
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.roster = roster;
        Size = new Vector2(940, 710);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(780, 680), MaximumSize = new Vector2(1600, 1200) };
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(28, 24) * Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 12 * Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1 * Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6 * Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 8) * Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10) * Scale);
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(14, 12) * Scale);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Paper);
        ImGui.PushStyleColor(ImGuiCol.Text, Ink);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, Muted);
        ImGui.PushStyleColor(ImGuiCol.Border, Line);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Card);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(1, 0.97f, 0.89f, 1));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Card);
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.87f, 0.82f, 0.71f, 1));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.82f, 0.75f, 0.61f, 1));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.76f, 0.68f, 0.53f, 1));
        ImGui.PushStyleColor(ImGuiCol.Separator, Line);
        ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, Paper);
        ImGui.PushStyleColor(ImGuiCol.TableRowBg, Card);
        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.955f, 0.92f, 0.84f, 1));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, Paper);
        ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0.72f, 0.66f, 0.54f, 1));
        ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(0.70f, 0.62f, 0.40f, 0.35f));
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(17);
        ImGui.PopStyleVar(7);
    }

    public override void Draw()
    {
        var snapshot = roster.Snapshot;
        DrawHeader();
        ImGui.Dummy(new Vector2(0, 10 * Scale));
        DrawStats(snapshot);
        ImGui.Dummy(new Vector2(0, 6 * Scale));
        DrawFilters();

        ActivityKind? kind = selectedFilter switch
        {
            1 => ActivityKind.Adventure,
            2 => ActivityKind.Away,
            3 => ActivityKind.Social,
            _ => null,
        };
        var members = RosterModel.Filter(snapshot.Members, search, kind).ToArray();
        var height = Math.Max(100 * Scale, ImGui.GetContentRegionAvail().Y - 125 * Scale);
        DrawRoster(snapshot, members, height);
        ImGui.Spacing();
        DrawFooter(snapshot, members.Length);
    }

    private void DrawHeader()
    {
        var draw = ImGui.GetWindowDrawList();
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        // A restrained seal and fine gold rules, rather than a heavy fantasy frame.
        draw.AddRectFilled(start, start + new Vector2(54, 54) * Scale, U(Ink), 10 * Scale);
        draw.AddRect(start + new Vector2(5, 5) * Scale, start + new Vector2(49, 49) * Scale, U(Gold), 7 * Scale);
        draw.AddText(ImGui.GetFont(), 29 * Scale, start + new Vector2(17, 9) * Scale, U(Paper), "B");
        draw.AddText(start + new Vector2(72, 0) * Scale, U(Gold), "S H E L T E R   /   R A I D E N");
        draw.AddText(ImGui.GetFont(), 29 * Scale, start + new Vector2(70, 23) * Scale, U(Ink), "Baka Free Company");
        ImGui.SetCursorScreenPos(start + new Vector2(width - 34 * Scale, 0));
        if (ImGui.Button("x##close", new Vector2(34, 32) * Scale))
            IsOpen = false;
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Close · reopen with /baka");
        ImGui.SetCursorScreenPos(start + new Vector2(0, 72) * Scale);
        ImGui.TextColored(Muted, "A little closer, wherever adventure takes us.");
        ImGui.Spacing();
        ImGui.Separator();
    }

    private void DrawStats(RosterSnapshot snapshot)
    {
        if (!ImGui.BeginTable("summary", 3, ImGuiTableFlags.SizingStretchSame)) return;
        var online = snapshot.OnlineCount?.ToString() ?? "—";
        var total = snapshot.TotalCount is { } n ? $"of {n} company members" : "waiting for the company roster";
        Stat("ONLINE NOW", online, total, Green);
        Stat("ON AN ADVENTURE", snapshot.ReadAt == null ? "—" : snapshot.Members.Count(m => m.Activity.Kind == ActivityKind.Adventure).ToString(), "from the loaded roster", Gold);
        Stat("TAKING A MOMENT", snapshot.ReadAt == null ? "—" : snapshot.Members.Count(m => m.Activity.Kind == ActivityKind.Away).ToString(), "away or busy", Muted);
        ImGui.EndTable();
    }

    private static void Stat(string label, string value, string hint, Vector4 accent)
    {
        ImGui.TableNextColumn();
        var pos = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var draw = ImGui.GetWindowDrawList();
        draw.AddRectFilled(pos, pos + new Vector2(width, 90 * Scale), U(Card), 8 * Scale);
        draw.AddRectFilled(pos + new Vector2(0, 15) * Scale, pos + new Vector2(3, 75) * Scale, U(accent), 2 * Scale);
        draw.AddText(pos + new Vector2(16, 12) * Scale, U(Muted), label);
        draw.AddText(ImGui.GetFont(), 28 * Scale, pos + new Vector2(14, 32) * Scale, U(accent), value);
        draw.AddText(pos + new Vector2(16, 66) * Scale, U(Muted), hint);
        ImGui.Dummy(new Vector2(width, 90 * Scale));
    }

    private void DrawFilters()
    {
        ImGui.TextColored(Gold, "THE COMPANY ROSTER");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##search", "Search members, jobs, locations or activity…", ref search, 128);
        string[] labels = ["All online", "Adventuring", "Away / busy", "Social"];
        for (var i = 0; i < labels.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            var selected = selectedFilter == i;
            if (selected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Ink);
                ImGui.PushStyleColor(ImGuiCol.Text, Paper);
            }
            if (ImGui.Button(labels[i])) selectedFilter = i;
            if (selected) ImGui.PopStyleColor(2);
        }
        ImGui.Spacing();
    }

    private static void DrawRoster(RosterSnapshot snapshot, Member[] members, float height)
    {
        if (members.Length == 0)
        {
            ImGui.BeginChild("empty-roster", new Vector2(0, height));
            ImGui.Dummy(new Vector2(0, 24 * Scale));
            ImGui.TextColored(Gold, snapshot.ReadAt == null ? "Your company, gathered here." : "No members to show.");
            ImGui.Spacing();
            ImGui.PushTextWrapPos();
            ImGui.TextUnformatted(snapshot.ReadAt == null ? snapshot.Notice :
                snapshot.Members.Length > 0 ? "No online members match these filters. Try another search or select All online." :
                snapshot.OnlineCount == 0 ? "No members are online in the loaded roster." : snapshot.Notice);
            ImGui.PopTextWrapPos();
            ImGui.EndChild();
            return;
        }

        if (!ImGui.BeginTable("members", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoBordersInBody, new Vector2(0, height))) return;
        ImGui.TableSetupColumn("MEMBER", ImGuiTableColumnFlags.WidthStretch, 1.1f);
        ImGui.TableSetupColumn("WHAT THEY'RE UP TO", ImGuiTableColumnFlags.WidthStretch, 1.05f);
        ImGui.TableSetupColumn("WHEREABOUTS", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();
        foreach (var member in members)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.None, 62 * Scale);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(member.Name);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(member.Name);
            ImGui.TextColored(Muted, member.Job);
            ImGui.TableNextColumn();
            var (label, kind) = member.Activity;
            var color = kind switch
            {
                ActivityKind.Adventure => Gold,
                ActivityKind.Away => Muted,
                ActivityKind.Social => new Vector4(0.40f, 0.38f, 0.52f, 1),
                _ => Green,
            };
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddCircleFilled(pos + new Vector2(4, 9) * Scale, 3 * Scale, U(color));
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 15 * Scale);
            ImGui.TextColored(color, label);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(label + " · reported by the game");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(member.Location);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(member.Location);
            ImGui.TextColored(Muted, member.World);
        }
        ImGui.EndTable();
    }

    private void DrawFooter(RosterSnapshot snapshot, int shown)
    {
        ImGui.Separator();
        ImGui.TextColored(Muted, snapshot.ReadAt == null ? "Waiting for the in-game roster" :
            $"{shown} shown · {snapshot.Members.Length} online members loaded" + (snapshot.IsPartial ? " · partial roster" : ""));
        if (ImGui.Button("Open Free Company")) roster.OpenCompany();
        ImGui.SameLine();
        var cooldown = roster.RefreshCooldown;
        ImGui.BeginDisabled(!snapshot.CanRefresh || cooldown > 0);
        if (ImGui.Button(cooldown > 0 ? $"Refresh ({cooldown}s)" : "Refresh roster")) roster.RequestRefresh();
        ImGui.EndDisabled();
        ImGui.PushTextWrapPos();
        ImGui.TextColored(Muted, snapshot.Notice);
        ImGui.PopTextWrapPos();
    }

    private static uint U(Vector4 color) => ImGui.ColorConvertFloat4ToU32(color);
}
