using System.Configuration;

namespace DotJEM.Web.Host.Configuration.Elements
{
    public interface IWebHostConfiguration
    {
        StorageConfiguration Storage { get; }
        IndexConfiguration Index { get; }
        DiagnosticsConfiguration Diagnostics { get; }
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

        [ConfigurationProperty("diagnostics", IsRequired = false)]
        public DiagnosticsConfiguration Diagnostics
        {
            get { return this["diagnostics"] as DiagnosticsConfiguration; }
        }
    }
}