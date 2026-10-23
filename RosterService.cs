using System.Threading;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.Sheets;

namespace BakaPlugin;

/// <summary>Copies native FC data on the framework thread; native pointers never reach the window.</summary>
public sealed class RosterService : IDisposable
{
    private readonly IClientState client;
    private readonly IFramework framework;
    private readonly IDataManager data;
    private readonly IPluginLog log;
    private RosterSnapshot snapshot = RosterSnapshot.Empty("Log in to see Shelter's Free Company roster.");
    private DateTimeOffset nextRead;
    private DateTimeOffset nextRequest;
    private int requestRefresh;
    private int openCompany;
    private bool failed;

    public bool IsVisible { get; set; }
    public RosterSnapshot Snapshot => Volatile.Read(ref snapshot);
    public int RefreshCooldown => Math.Max(0, (int)Math.Ceiling((nextRequest - DateTimeOffset.UtcNow).TotalSeconds));

    public RosterService(IClientState client, IFramework framework, IDataManager data, IPluginLog log)
    {
        this.client = client;
        this.framework = framework;
        this.data = data;
        this.log = log;
        framework.Update += Update;
        client.Logout += OnLogout;
    }

    public void RequestRefresh() => Interlocked.Exchange(ref requestRefresh, 1);
    public void OpenCompany() => Interlocked.Exchange(ref openCompany, 1);

    private void OnLogout(int type, int code)
    {
        Volatile.Write(ref snapshot, RosterSnapshot.Empty("Log in to see Shelter's Free Company roster."));
        nextRead = nextRequest = default;
        Interlocked.Exchange(ref requestRefresh, 0);
        Interlocked.Exchange(ref openCompany, 0);
    }

    private unsafe void Update(IFramework _)
    {
        if (!client.IsLoggedIn || !IsVisible)
            return;

        var now = DateTimeOffset.UtcNow;
        if (now < nextRead)
            return;
        nextRead = now.AddSeconds(1);

        try
        {
            if (Interlocked.Exchange(ref openCompany, 0) != 0)
            {
                var agent = AgentFreeCompany.Instance();
                if (agent != null)
                    agent->Show();
            }

            var company = InfoProxyFreeCompany.Instance();
            var roster = InfoProxyFreeCompanyMember.Instance();
            if (company == null || roster == null || company->Id == 0)
            {
                Volatile.Write(ref snapshot, RosterSnapshot.Empty("Open Free Company > Members to load Shelter's roster."));
                return;
            }

            var homeWorld = data.GetExcelSheet<World>().GetRowOrDefault(company->HomeWorldId)?.Name.ToString() ?? "";
            if (!RosterModel.IsTargetCompany(company->NameString, homeWorld))
            {
                Volatile.Write(ref snapshot, RosterSnapshot.Empty("This panel is for Shelter on Raiden. Open Shelter's Free Company Members tab."));
                return;
            }

            // Another company's profile can temporarily occupy the FC proxy. Never mix two rosters.
            if (roster->FreeCompanyId == 0 || roster->FreeCompanyId != company->Id)
            {
                Volatile.Write(ref snapshot, RosterSnapshot.Empty("Open Shelter's Members tab to load a matching roster."));
                return;
            }

            var refresh = Interlocked.Exchange(ref requestRefresh, 0) != 0;
            if (now >= nextRequest && (refresh || IsVisible))
            {
                // The game's request handles its own pagination. Throttle even failed requests.
                nextRequest = now.AddSeconds(30);
                roster->RequestData();
            }

            var count = roster->EntryCount;
            if (roster->CharData == null || count == 0 || count > roster->DataSize || count > 512)
            {
                Volatile.Write(ref snapshot, new RosterSnapshot(company->NameString, [], company->OnlineMembers,
                    company->TotalMembers, null, "Waiting for member data. Open Free Company > Members if this persists.", true));
                return;
            }

            var members = new List<Member>((int)count);
            // Bound reads to the game's allocated capacity. Copy strings immediately.
            for (var index = 0; index < count; index++)
            {
                var entry = roster->CharData[index];
                var status = (MemberStatus)entry.State;
                var name = entry.NameString;
                var job = data.GetExcelSheet<ClassJob>().GetRowOrDefault(entry.Job)?.Abbreviation.ToString() ?? "—";
                var location = entry.Location == 0 ? "Location unavailable" :
                    data.GetExcelSheet<TerritoryType>().GetRowOrDefault(entry.Location)?.PlaceName.Value.Name.ToString();
                var world = data.GetExcelSheet<World>().GetRowOrDefault(entry.CurrentWorld)?.Name.ToString() ?? "";
                members.Add(new Member(entry.ContentId, name, job,
                    string.IsNullOrWhiteSpace(location) ? "Location unavailable" : location, world, status));
            }

            var online = RosterModel.OnlineMembers(members);
            var notice = online.Length < company->OnlineMembers
                ? "Some online members have not loaded. Open the Members tab and refresh."
                : "Activity and locations reflect the game's most recently loaded FC roster.";
            Volatile.Write(ref snapshot, new RosterSnapshot(company->NameString, online,
                company->OnlineMembers, company->TotalMembers, now, notice, true));
            failed = false;
        }
        catch (Exception exception)
        {
            if (!failed)
                log.Error(exception, "Unable to read the Free Company roster.");
            failed = true;
            Volatile.Write(ref snapshot, RosterSnapshot.Empty("The roster is unavailable. Reopen the Members tab and try again."));
        }
    }

    public void Dispose()
    {
        framework.Update -= Update;
        client.Logout -= OnLogout;
    }
}
