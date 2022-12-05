using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DotJEM.Web.Host.Configuration.Elements;

[ConfigurationCollection(typeof(WatchElement))]
public class WatchElementCollection : ConfigurationElementCollection
{
    public IEnumerable<WatchElement> Items => this.OfType<WatchElement>();

    protected override ConfigurationElement CreateNewElement()
    {
        return new WatchElement();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
        return ((WatchElement)(element)).Area;
    }

    [ConfigurationProperty("interval", IsRequired = true)]
    public long Interval => (long)this["interval"];

    [ConfigurationProperty("rambuffer", IsRequired = false, DefaultValue = "512mb")]
    public string RamBuffer => (string)this["rambuffer"];

    [ConfigurationProperty("batch-size", IsRequired = false, DefaultValue = 5000)]
    public int BatchSize => (int)this["batch-size"];
}