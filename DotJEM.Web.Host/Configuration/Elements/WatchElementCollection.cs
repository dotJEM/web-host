using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DotJEM.Web.Host.Configuration.Elements
{
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
}