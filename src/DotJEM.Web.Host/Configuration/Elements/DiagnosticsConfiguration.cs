using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements;

public class TelemetryConfiguration : ConfigurationElement
{
    [ConfigurationProperty("performance", IsRequired = false, DefaultValue = null)]
    public TraceLoggerConfiguration TraceLogger => this["performance"] as TraceLoggerConfiguration;
}