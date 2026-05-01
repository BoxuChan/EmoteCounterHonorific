using Dalamud.Configuration;
using EmoteCounterHonorific.Configs;
using EmoteCounterHonorific.Emotes;
using System;
using System.Collections.Generic;

namespace EmoteCounterHonorific;

[Serializable]
public class Config : IPluginConfiguration
{
    public static readonly int LATEST = 4;

    public int Version { get; set; } = LATEST;

    public bool Enabled { get; set; } = true;

    public List<EmoteConfig> EmoteConfigs { get; init; } = [];

    public EmoteCounters<uint> Counters { get; set; } = [];

    [Obsolete("Changed to AutoClearDelayMs in version 4")]
    public int AutoClearTitleInterval { get; set; } = 5; // seconds

    public ushort AutoClearDelayMs { get; set; } = 5000;

    public bool IsHonorificSupporter { get; set; } = false;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
