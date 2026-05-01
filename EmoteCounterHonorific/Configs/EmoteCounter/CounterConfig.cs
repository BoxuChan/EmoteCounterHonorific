using Newtonsoft.Json;

namespace EmoteCounterHonorific.Configs.EmoteCounter;

public class CounterConfig
{
    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; } = string.Empty;

    [JsonProperty(Required = Required.Always)]
    public uint Value { get; set; }
}
