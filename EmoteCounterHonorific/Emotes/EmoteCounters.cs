using Newtonsoft.Json;
using System.Collections.Generic;

namespace EmoteCounterHonorific.Emotes;

[JsonArray]
public class EmoteCounters : Dictionary<EmoteCounterKey, uint>
{
}
