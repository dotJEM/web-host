using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DotJEM.Web.Host.Configuration.Elements;

public interface IWebHostConfiguration
{
    string KillSignalFile { get; }

    StorageConfiguration Storage { get; }
    IndexConfiguration Index { get; }
    TelemetryConfiguration Telemetry { get; }
    DataCleanupElementCollection Cleanup { get; }
}

public class WebHostConfiguration : ConfigurationSection, IWebHostConfiguration
{

    [ConfigurationProperty("storageConfiguration", IsRequired = true)]
    public StorageConfiguration Storage => this["storageConfiguration"] as StorageConfiguration;

    [ConfigurationProperty("indexConfiguration", IsRequired = true)]
    public IndexConfiguration Index => this["indexConfiguration"] as IndexConfiguration;

    [ConfigurationProperty("kill-signal-file", IsRequired = false, DefaultValue = null)]
    public string KillSignalFile => (string)this["kill-signal-file"];

    [ConfigurationProperty("diagnostics", IsRequired = false, DefaultValue = null)]
    public TelemetryConfiguration Telemetry => this["diagnostics"] as TelemetryConfiguration;

    [ConfigurationProperty("cleanup", IsRequired = false, DefaultValue = null)]
    public DataCleanupElementCollection Cleanup => this["cleanup"] as DataCleanupElementCollection;

}

[ConfigurationCollection(typeof(CleanQueryConfigurationElement))]
public class DataCleanupElementCollection : ConfigurationElementCollection
{
    [ConfigurationProperty("interval", IsRequired = true)]
    public string Interval => (string)this["interval"];

    public IEnumerable<CleanQueryConfigurationElement> Items => this.OfType<CleanQueryConfigurationElement>();

    protected override ConfigurationElement CreateNewElement()
    {
        return new CleanQueryConfigurationElement();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
        return ((CleanQueryConfigurationElement)(element)).Query;
    }
}

public class CleanQueryConfigurationElement : ConfigurationElement
{
    [ConfigurationProperty("query", IsRequired = true)]
    public string Query => this["query"] as string;

    [ConfigurationProperty("interval", IsRequired = false, DefaultValue = null)]
    public string Interval => (string)this["interval"];
}