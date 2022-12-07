using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements;

public interface IWebHostConfiguration
{
    string KillSignalFile { get; }
    StorageConfiguration Storage { get; }
    IndexConfiguration Index { get; }
    DiagnosticsConfiguration Diagnostics { get; }
}

public class WebHostConfiguration : ConfigurationSection, IWebHostConfiguration
{
    [ConfigurationProperty("kill-signal-file", IsRequired = false, DefaultValue = null)]
    public string KillSignalFile => (string)this["kill-signal-file"];

    [ConfigurationProperty("storageConfiguration", IsRequired = true)]
    public StorageConfiguration Storage => this["storageConfiguration"] as StorageConfiguration;

    [ConfigurationProperty("indexConfiguration", IsRequired = true)]
    public IndexConfiguration Index => this["indexConfiguration"] as IndexConfiguration;

    [ConfigurationProperty("diagnostics", IsRequired = false)]
    public DiagnosticsConfiguration Diagnostics => this["diagnostics"] as DiagnosticsConfiguration;
}