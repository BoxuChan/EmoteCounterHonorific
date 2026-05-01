using Dalamud.Plugin;
using System;

namespace EmoteCounterHonorific.Configs;

public class ConfigMigrator(IDalamudPluginInterface pluginInterface)
{
    public void MaybeMigrate(Config config)
    {
        if (config.Version == Config.LATEST) return;

        if (config.Version < 4)
        {
            try
            {
#pragma warning disable CS0618
                config.AutoClearDelayMs = Convert.ToUInt16(config.AutoClearTitleInterval * 1000);
#pragma warning restore CS0618
            } 
            catch (OverflowException)
            {
                config.AutoClearDelayMs = ushort.MaxValue;
            }

            config.EmoteConfigs.ForEach(ec =>
            {
                ec.TitleTemplate = ec.TitleTemplate.Replace("{0}", "{{ total_count }}");
                ec.TitleDataConfig = new()
                {
                    IsPrefix = ec.IsPrefix,
                    Color = ec.Color,
                    Glow = ec.Glow
                };
            });
        }

        config.Version = Config.LATEST;
        pluginInterface.SavePluginConfig(config);   
    }
}
