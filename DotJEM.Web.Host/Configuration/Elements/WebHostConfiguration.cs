using System.Configuration;

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
}