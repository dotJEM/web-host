using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public class IndexConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("debug", IsRequired = false, DefaultValue = false)]
        public bool Debugging
        {
            get { return (bool)this["debug"]; }
        }

        [ConfigurationProperty("storage", IsRequired = false)]
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
        [ConfigurationProperty("path", IsRequired = false)]
        public string Path => this["path"] as string;

        [ConfigurationProperty("type", IsRequired = true)]
        public IndexStorageType Type => (IndexStorageType)this["type"];
    }

    public enum IndexStorageType { Memmory, File, CachedMemmory }
}