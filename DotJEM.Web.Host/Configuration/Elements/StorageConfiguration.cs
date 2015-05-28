using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public class StorageConfiguration : ConfigurationElement
    {
        [ConfigurationProperty("connectionString", IsRequired = true)]
        public string ConnectionString
        {
            get { return this["connectionString"] as string; }
        }
    }
}