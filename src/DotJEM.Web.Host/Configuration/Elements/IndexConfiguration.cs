﻿using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public class IndexConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("debug", IsRequired = false)]
        public IndexDebuggingConfiguration Debugging => this["debug"] as IndexDebuggingConfiguration;

        [ConfigurationProperty("storage", IsRequired = false)]
        public IndexStorageConfiguration Storage => this["storage"] as IndexStorageConfiguration;

        [ConfigurationProperty("watch", IsRequired = true)]
        public WatchElementCollection Watch => this["watch"] as WatchElementCollection;
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

    public enum IndexStorageType { Memory, File, CachedMemory }
}