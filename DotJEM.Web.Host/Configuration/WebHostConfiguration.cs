using System.Configuration;

namespace DotJEM.Web.Host.Configuration
{
    public interface IWebHostConfiguration
    {
        StorageConfiguration Storage { get; }
    }

    public class WebHostConfiguration : ConfigurationSection, IWebHostConfiguration
    {
        [ConfigurationProperty("storageConfiguration", IsRequired = true)]
        public StorageConfiguration Storage
        {
            get { return this["storageConfiguration"] as StorageConfiguration; }
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
}