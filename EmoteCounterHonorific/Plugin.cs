using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using EmoteCounterHonorific.Windows;
using Dalamud.Plugin.Services;
using EmoteCounterHonorific.Interop;
using EmoteCounterHonorific.Emotes;
using Dalamud.Game.Command;
using Emote = Lumina.Excel.Sheets.Emote;
using System.Linq;
using Lumina.Excel;
using EmoteCounterHonorific.Configs;

namespace EmoteCounterHonorific;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;

    private static readonly string[] CommandNames = ["/emotecounterhonorific", "/ech", "/echonorific"];
    private const string CommandHelpMessage = $"Available subcommands for /echonorific are info, config, enable and disable";

    public Config Config { get; init; }

    public readonly WindowSystem WindowSystem = new("EmoteCounterHonorific");
    private ConfigWindow ConfigWindow { get; init; }

    private EmoteHook EmoteHook { get; init; }
    private Updater Updater { get; init; }
    private ExcelSheet<Emote> EmoteSheet { get; init; }

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Config ?? new Config()
        {
            EmoteConfigs = [
                new() { Name = "Receiving Pet", EmoteIds = [105], TitleTemplate = "Pet Counter {{ total_count }}" },
                new() { Name = "Receiving Dote", EmoteIds = [146, 147], TitleTemplate = "Dote Counter {{ total_count }}" },
                new() { Name = "Receiving Hug",  EmoteIds = [112, 113], TitleTemplate = "Hug Counter {{ total_count }}" },
                new() { Name = "Receiving Heart",  EmoteIds = [274], TitleTemplate = "Heart Counter {{ total_count }}" },
                new() { Name = "Receiving Petals",  EmoteIds = [211], TitleTemplate = "Petals Counter {{ total_count }}" }
            ]
        };

        #region Deprecated
        new ConfigMigrator(PluginInterface).MaybeMigrate(Config);
        #endregion

        var emoteCounterSynchronizer = new EmoteCounterSynchronizer(PluginInterface, PluginLog);

        var setCharacterTitle = PluginInterface.GetIpcSubscriber<int, string, object>("Honorific.SetCharacterTitle");
        var clearCharacterTitle = PluginInterface.GetIpcSubscriber<int, object>("Honorific.ClearCharacterTitle");

        EmoteSheet = DataManager.GetExcelSheet<Emote>()!;
        ConfigWindow = new(Config, EmoteSheet, emoteCounterSynchronizer, PlayerState, PluginInterface, PluginLog);
        EmoteHook = new(PluginLog, GameInteropProvider);

        Updater = new(clearCharacterTitle, Config, EmoteHook, Framework, ObjectTable, PlayerState, PluginLog, PluginInterface, setCharacterTitle);

        foreach (var command in CommandNames)
        {
            CommandManager.AddHandler(command, new CommandInfo(OnCommand)
            {
                HelpMessage = CommandHelpMessage
            });
        }

        WindowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;
    }

    private void OnCommand(string command, string args)
    {
        var subcommand = args.Split(" ", 2)[0];
        if (subcommand == "config")
        {
            ToggleConfigUI();
        }
        else if (subcommand == "enable")
        {
            Config.Enabled = true;
            SaveConfig();
        }
        else if (subcommand == "disable")
        {
            Config.Enabled = false;
            SaveConfig();
        }
        else if (subcommand == "info")
        {
            PrintCounters();
        }
        else
        {
            ChatGui.Print(CommandHelpMessage);
        }
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        EmoteHook.Dispose();
        Updater.Dispose();
    }

    private void PrintCounters()
    {
        ChatGui.Print("Counters:");
        foreach(var counter in Config.Counters.Where(c => c.Key.CharacterId == PlayerState.ContentId))
        {
            var key = counter.Key;
            ChatGui.Print($"     {EmoteSheet.GetRowAt(key.EmoteId).Name}: {counter.Value} ({key.Direction})");
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    private void SaveConfig() => PluginInterface.SavePluginConfig(Config);
}
