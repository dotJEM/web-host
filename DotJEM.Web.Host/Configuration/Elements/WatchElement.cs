using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public class WatchElement : ConfigurationElement
    {
        [ConfigurationProperty("area", IsRequired = true)]
        public string Area => this["area"] as string;

        [ConfigurationProperty("batch-size", IsRequired = false, DefaultValue = 5000)]
        public int BatchSize => (int)this["batch-size"];
    }
}