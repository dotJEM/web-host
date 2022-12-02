using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements;

public class PerformanceConfiguration : ConfigurationElement
{
    [ConfigurationProperty("path", IsRequired = true)]
    public string Path
    {
        get { return this["path"] as string; }
    }

    [ConfigurationProperty("max-size", IsRequired = false, DefaultValue = "10MB")]
    public string MaxSize
    {
        get { return this["max-size"] as string; }
    }

    [ConfigurationProperty("max-files", IsRequired = false, DefaultValue = 10)]
    public int MaxFiles
    {
        get { return (int)this["max-files"]; }
    }

    [ConfigurationProperty("zip", IsRequired = false, DefaultValue = false)]
    public bool Zip
    {
        get { return (bool)this["zip"]; }
    }
}