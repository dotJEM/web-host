using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements;

public class WatchElement : ConfigurationElement
{
    [ConfigurationProperty("area", IsRequired = true)]
    public string Area => this["area"] as string;

    [ConfigurationProperty("batch-size", IsRequired = false, DefaultValue = -1)]
    public int BatchSize => (int)this["batch-size"];

    [ConfigurationProperty("initial-generation", IsRequired = false, DefaultValue = 0L)]
    public long InitialGeneration  => (long)this["initial-generation"];
}