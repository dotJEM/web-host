using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public class WatchElement : ConfigurationElement
    {
        [ConfigurationProperty("area", IsRequired = true)]
        public string Area => this["area"] as string;

        [ConfigurationProperty("index", IsRequired = true)]
        public string Index => this["index"] as string;
    }
}