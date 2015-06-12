using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public class IndexConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("cache-location", IsRequired = false)]
        public string CacheLocation
        {
            get { return this["cache-location"] as string; }
        }

        [ConfigurationProperty("watch", IsRequired = true)]
        public WatchElementCollection Watch
        {
            get { return this["watch"] as WatchElementCollection; }
        }
    }
}