using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements;

public class DiagnosticsConfiguration : ConfigurationElement
{
    [ConfigurationProperty("performance", IsRequired = false, DefaultValue = null)]
    public PerformanceConfiguration Performance => this["performance"] as PerformanceConfiguration;
}