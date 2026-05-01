using Newtonsoft.Json;

namespace EmoteCounterHonorific.Configs.EmoteCounter;

public class EmoteCounterConfig
{
    [JsonProperty(Required = Required.Always)]
    public EmoteDataConfig[] EmoteData { get; set; } = [];
}
