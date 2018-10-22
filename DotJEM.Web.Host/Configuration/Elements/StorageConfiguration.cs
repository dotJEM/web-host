using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DotJEM.Web.Host.Configuration.Elements
{
    [ConfigurationCollection(typeof(StorageAreaElement), AddItemName = "area")]
    public class StorageConfiguration : ConfigurationElementCollection
    {
        [ConfigurationProperty("interval", IsRequired = false, DefaultValue = "30m")]
        public string Interval => (string)this["interval"];

        [ConfigurationProperty("connectionString", IsRequired = true)]
        public string ConnectionString => this["connectionString"] as string;

        public IEnumerable<StorageAreaElement> Items => this.OfType<StorageAreaElement>();

        protected override ConfigurationElement CreateNewElement() => new WatchElement();

        protected override object GetElementKey(ConfigurationElement element) => ((WatchElement)element).Area;
    }

    public class StorageAreaElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name => this["name"] as string;

        [ConfigurationProperty("history", IsRequired = false, DefaultValue = false)]
        public bool History => (bool)this["history"];

        [ConfigurationProperty("historyAge", IsRequired = false, DefaultValue = null)]
        public string HistoryAge => (string)this["historyAge"];
    }
}