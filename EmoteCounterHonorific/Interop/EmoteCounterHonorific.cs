using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using EmoteCounterHonorific.Emotes;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EmoteCounterHonorific.Interop;

public class EmoteCounterConfig(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog)
{
    private static readonly Dictionary<string, HashSet<ushort>> EMOTE_NAME_TO_IDS = new()
    {
        { "Pet", [105] },
        { "Dote", [146, 147] },
        { "Hug", [112, 113] },
        { "Heart", [274] },
        { "Petals", [211] }
    };

    public class JsonConfig
    {
        [JsonProperty(Required = Required.Always)]
        public EmoteDataConfig[] EmoteData { get; set; } = [];
    }

    public class EmoteDataConfig
    {
        [JsonProperty(Required = Required.Always)]
        public ulong CID { get; set; }

        [JsonProperty(Required = Required.Always)]
        public CounterConfig[] Counters { get; set; } = [];

        public override string ToString()
        {
            return $"{nameof(EmoteDataConfig)} {{ {nameof(CID)} = {CID}, {nameof(Counters)} = [\n\t{string.Join(",\n\t", [..Counters])}\n] }}";
        }
    }

    public record CounterConfig
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; } = string.Empty;

        [JsonProperty(Required = Required.Always)]
        public uint Value { get; set; }
    }

    private IDalamudPluginInterface PluginInterface { get; init; } = pluginInterface;
    private IPluginLog PluginLog { get; init; } = pluginLog;

    public bool TryParse(out JsonConfig parsed)
    {
        var pluginConfigsDirectory = Path.GetFullPath(Path.Combine(PluginInterface.GetPluginConfigDirectory(), ".."));
        // %appdata%\xivlauncher\pluginConfigs\EmoteCounter.json
        var EmoteCounterConfigPath = Path.Combine(pluginConfigsDirectory, "EmoteCounter.json");
        if (!Path.Exists(EmoteCounterConfigPath))
        {
            PluginLog.Error($"EmoteCounter config not found at {EmoteCounterConfigPath}");
            parsed = null!;
            return false;
        }

        using StreamReader EmoteCounterConfigFile = new(EmoteCounterConfigPath);
        var EmoteCounterConfigJson = EmoteCounterConfigFile.ReadToEnd();
        parsed = JsonConvert.DeserializeObject<JsonConfig>(EmoteCounterConfigJson)!;

        if (parsed == null)
        {
            PluginLog.Error($"Failed to parse EmoteCounter config at {EmoteCounterConfigPath}");
            return false;
        }

        return true;
    }

    public bool TrySync(Config config)
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

                    // Update the count of the first emote only since patme doesn't differentiate
                    if (i == 0)
                    {
                        PluginLog.Verbose($"Using temporary total count {internalCounter} for {key}");

                        var value = counter.Value - totalCounter;
                        if (config.Counters.TryAdd(key, value))
                        {
                            PluginLog.Debug($"Set new {key} to value {value}");
                        } 
                        else
                        {
                            config.Counters[key] += value;
                            PluginLog.Debug($"Added {value} to existing {key} now has value {config.Counters[key]}");
                        }
                    }
                }
            }
        }

        return true;
    }
}
