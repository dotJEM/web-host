using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public interface IWebHostConfiguration
    {
        StorageConfiguration Storage { get; }
        IndexConfiguration Index { get; }
    }

    public class WebHostConfiguration : ConfigurationSection, IWebHostConfiguration
    {
        [ConfigurationProperty("storageConfiguration", IsRequired = true)]
        public StorageConfiguration Storage
        {
            get { return this["storageConfiguration"] as StorageConfiguration; }
        }

        [ConfigurationProperty("indexConfiguration", IsRequired = true)]
        public IndexConfiguration Index
        {
            get { return this["indexConfiguration"] as IndexConfiguration; }
        }
    }

    public class StorageConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("connectionString", IsRequired = true)]
        public string ConnectionString
        {
            get { return this["connectionString"] as string; }
        }
    }

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


    [ConfigurationCollection(typeof(WatchElement))]
    public class WatchElementCollection : ConfigurationElementCollection
    {
        public IEnumerable<WatchElement> Items
        {
            get { return this.OfType<WatchElement>(); }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new WatchElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((WatchElement)(element)).Area;
        }

        [ConfigurationProperty("interval", IsRequired = true)]
        public long Interval
        {
            get { return (long)this["interval"]; }
        }
    }

    public class WatchElement : ConfigurationElement
    {
        [ConfigurationProperty("area", IsRequired = true)]
        public string Area
        {
            get { return this["area"] as string; }
        }
    }
}