using System.Collections.Generic;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{
    public interface IPipelineContext
    {
        bool TryGetValue(string key, out object value);

        object GetParameter(string key);

        IPipelineContext Replace(params (string key, object value)[] values);
    }

    public class PipelineContext : IPipelineContext
    {
        private readonly Dictionary<string, object> parameters = new();

        public virtual bool TryGetValue(string key, out object value) => parameters.TryGetValue(key, out value);

        public virtual object GetParameter(string key)
        {
            return parameters.TryGetValue(key, out object value) ? value : null;
        }

        public virtual IPipelineContext Replace(params (string key, object value)[] values)
        {
            foreach ((string key, object value) in values)
                parameters[key] = value;
            return this;
        }

        public PipelineContext Set(string key, object value)
        {
            parameters[key] = value;
            return this;
        }
    }
}