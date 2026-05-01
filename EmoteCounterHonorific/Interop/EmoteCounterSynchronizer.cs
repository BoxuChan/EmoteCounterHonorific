using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using EmoteCounterHonorific.Configs;
using EmoteCounterHonorific.Configs.EmoteCounter;
using EmoteCounterHonorific.Emotes;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace EmoteCounterHonorific.Interop;

public class EmoteCounterSynchronizer(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog)
{
    private static readonly Dictionary<string, HashSet<ushort>> EMOTE_NAME_TO_IDS = new()
    {
        { "Pet", [105] },
        { "Dote", [146, 147] },
        { "Hug", [112, 113] },
        { "Love Heart", [274] },
        { "Flower Shower", [211] }
    };

    private IDalamudPluginInterface PluginInterface { get; init; } = pluginInterface;
    private IPluginLog PluginLog { get; init; } = pluginLog;

    private bool TryParse([NotNullWhen(true)] out EmoteCounterConfig? parsed)
    {
        parsed = null;

        var pluginConfigsDirectory = Path.GetFullPath(Path.Combine(PluginInterface.GetPluginConfigDirectory(), ".."));

        // %appdata%\xivlauncher\pluginConfigs\EmoteCounter.json
        var emoteCounterConfigPath = Path.Combine(pluginConfigsDirectory, "EmoteCounter.json");

        if (!Path.Exists(emoteCounterConfigPath))
        {
            PluginLog.Error($"EmoteCounter config not found at {emoteCounterConfigPath}");
            return false;
        }

        var emoteCounterConfigJson = File.ReadAllText(emoteCounterConfigPath);
        parsed = JsonConvert.DeserializeObject<EmoteCounterConfig>(emoteCounterConfigJson);
        if (parsed != null) return true;

        PluginLog.Error($"Failed to parse EmoteCounter config at {emoteCounterConfigPath}");
        return false;
    }

    public bool TryUpdate(Config config)
    {
        if (!TryParse(out var parsed))
        {
            PluginLog.Error("Failed to sync since parsing failed");
            return false;
        }

        foreach (var emoteData in parsed.EmoteData)
        {
            PluginLog.Verbose($"Parsed {emoteData}");

            var characterId = emoteData.CID;
            foreach (var counter in emoteData.Counters)
            {
                var emoteIds = EMOTE_NAME_TO_IDS[counter.Name];
                var totalCounter = 0u;
                for (var i = emoteIds.Count - 1; i >= 0; i--)
                {
                    var emoteId = emoteIds.ElementAt(i);
                    var key = new EmoteCounterKey() { CharacterId = characterId, EmoteId = emoteId, Direction = EmoteDirection.Receiving };

                    if (config.Counters.TryGetValue(key, out var internalCounter))
                    {
                        PluginLog.Verbose($"Added {internalCounter} to temporary total count using {key}");
                        totalCounter += internalCounter;
                    }

                    // Update the count of the first emote only since emotecounter doesn't differentiate
                    if (i > 0) continue;

                    PluginLog.Verbose($"Using temporary total count {internalCounter} for {key}");

                    var value = counter.Value - totalCounter;
                    if (config.Counters.TryAdd(key, value))
                    {
                        PluginLog.Debug($"Set new {key} to value {value}");
                        continue;
                    }

                    config.Counters[key] += value;
                    PluginLog.Debug($"Added {value} to existing {key} now has value {config.Counters[key]}");
                }
            }
        }

        return true;
    }
}
