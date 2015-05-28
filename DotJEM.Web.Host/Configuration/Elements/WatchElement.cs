using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public class WatchElement : ConfigurationElement
    {
        [ConfigurationProperty("area", IsRequired = true)]
        public string Area
        {
            get { return this["area"] as string; }
        }
    }
}