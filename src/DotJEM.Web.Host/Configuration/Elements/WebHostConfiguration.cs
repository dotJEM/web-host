using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DotJEM.Web.Host.Configuration.Elements;

public interface IWebHostConfiguration
{
    string KillSignalFile { get; }

    StorageConfiguration Storage { get; }
    IndexConfiguration Index { get; }
    DiagnosticsConfiguration Diagnostics { get; }
    DataCleanupElementCollection Cleanup { get; }
}

//public class OptionalConfigurationSection : ConfigurationElement
//{
//    public bool IsPresent { get; private set; }

//    protected override void DeserializeElement(System.Xml.XmlReader reader, bool serializeCollectionKey)
//    {
//        base.DeserializeElement(reader, serializeCollectionKey);
//        IsPresent = true;  // Mark this section as present if it is successfully deserialized
//    }
//}

public static class OptionalConfigurationSectionExtensions
{
    public static bool IsPresent(this ConfigurationElement self) => self.ElementInformation.IsPresent;
    public static T IfPresent<T>(this T self) where T : ConfigurationElement => self.ElementInformation.IsPresent ? self : null;
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
    public DiagnosticsConfiguration Diagnostics => this["diagnostics"] as DiagnosticsConfiguration;

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