using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DotJEM.Web.Host.Configuration.Elements
{
    [ConfigurationCollection(typeof(IndexShardConfiguration), AddItemName = "shard")]

    public class IndexConfiguration : ConfigurationElementCollection
    {
        public IEnumerable<IndexShardConfiguration> Items => this.OfType<IndexShardConfiguration>();

        protected override ConfigurationElement CreateNewElement() => new IndexShardConfiguration();

        protected override object GetElementKey(ConfigurationElement element) => ((IndexShardConfiguration)element).Name;
    }

    public class IndexShardConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name => this["name"] as string;

        [ConfigurationProperty("debug", IsRequired = false)]
        public IndexDebuggingConfiguration Debugging => this["debug"] as IndexDebuggingConfiguration;

        [ConfigurationProperty("storage", IsRequired = false)]
        public IndexStorageConfiguration Storage => this["storage"] as IndexStorageConfiguration;

        [ConfigurationProperty("watch", IsRequired = true)]
        public WatchElementCollection Watch => this["watch"] as WatchElementCollection;

        [ConfigurationProperty("snapshots", IsRequired = false, DefaultValue = null)]
        public IndexSnapshotsConfiguration Snapshots => this["snapshots"] as IndexSnapshotsConfiguration;
    }

    public class IndexDebuggingConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("enabled", IsRequired = false, DefaultValue = false)]
        public bool Enabled => (bool)this["enabled"];

        [ConfigurationProperty("index-writer-info-stream", IsRequired = false)]
        public InfoStreamConfiguration IndexWriterInfoStream => this["index-writer-info-stream"] as InfoStreamConfiguration;
    }

    public class InfoStreamConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path => this["path"] as string;

        [ConfigurationProperty("max-size", IsRequired = false, DefaultValue = "10MB")]
        public string MaxSize => this["max-size"] as string;

        [ConfigurationProperty("max-files", IsRequired = false, DefaultValue = 10)]
        public int MaxFiles => (int)this["max-files"];

        [ConfigurationProperty("zip", IsRequired = false, DefaultValue = false)]
        public bool Zip => (bool)this["zip"];
    }

    public class IndexStorageConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("path", IsRequired = false)]
        public string Path => this["path"] as string;

        [ConfigurationProperty("type", IsRequired = true)]
        public IndexStorageType Type => (IndexStorageType)this["type"];
    }

    public class IndexSnapshotsConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("path", IsRequired = false)]
        public string Path => this["path"] as string;

        [ConfigurationProperty("max-snapshots", IsRequired = false, DefaultValue = 0)]
        public int MaxSnapshots => (int)this["max-snapshots"];

        [ConfigurationProperty("cron-time", IsRequired = false, DefaultValue = "")]
        public string CronTime => (string)this["cron-time"];
    }

    public enum IndexStorageType { Memory, File, CachedMemory }
}