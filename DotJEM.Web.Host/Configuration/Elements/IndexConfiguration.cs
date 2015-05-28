using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public class IndexConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("storage", IsRequired = true)]
        public IndexStorageConfiguration Storage
        {
            get { return this["storage"] as IndexStorageConfiguration; }
        }

        [ConfigurationProperty("watch", IsRequired = true)]
        public WatchElementCollection Watch
        {
            get { return this["watch"] as WatchElementCollection; }
        }
    }
    public class IndexStorageConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return this["type"] as string; }
        }

        [ConfigurationProperty("path", IsRequired = false)]
        public string Path
        {
            get { return this["path"] as string; }
        }

    }
}