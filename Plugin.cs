using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace BakaPlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDataManager Data { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly WindowSystem windows = new("BakaPlugin");
    private readonly RosterService roster;
    private readonly PluginWindow window;

    public Plugin()
    {
        roster = new RosterService(ClientState, Framework, Data, Log);
        window = new PluginWindow(roster) { IsOpen = true };
        windows.AddWindow(window);
        Commands.AddHandler("/baka", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Baka Free Company roster. /baka refresh requests a roster update.",
        });
        PluginInterface.UiBuilder.Draw += Draw;
        PluginInterface.UiBuilder.OpenMainUi += Open;
    }

    private void Draw()
    {
        roster.IsVisible = window.IsOpen;
        windows.Draw();
    }

    private void Open() => window.IsOpen = true;

    private void OnCommand(string command, string args)
    {
        if (args.Trim().Equals("refresh", StringComparison.OrdinalIgnoreCase))
        {
            Open();
            roster.RequestRefresh();
        }
        else
        {
            window.Toggle();
        }
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= Draw;
        PluginInterface.UiBuilder.OpenMainUi -= Open;
        Commands.RemoveHandler("/baka");
        roster.Dispose();
        windows.RemoveAllWindows();
    }
}
