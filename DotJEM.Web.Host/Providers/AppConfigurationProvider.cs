using System.Configuration;

namespace DotJEM.Web.Host.Providers
{
    public interface IAppConfigurationProvider
    {
        T Get<T>(string name = null) where T : ConfigurationSection, new();
    }

    internal class AppConfigurationProvider : IAppConfigurationProvider
    {
        public T Get<T>(string name = null) where T : ConfigurationSection, new()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = typeof(T).Name;
                name = char.ToLowerInvariant(name[0]) + name.Substring(1);
            }
            return ConfigurationManager.GetSection(name) as T ?? new T();
        }
    }
}