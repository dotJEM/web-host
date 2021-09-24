using System;
using System.Collections.Generic;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{
    public interface IPipelineContext
    {
        object this[string key] { get; set; }

        bool TryGetValue(string key, out object value);

        object GetParameter(string key);

        IPipelineContext Replace(params (string key, object value)[] values);

        IPipelineContext Add(string key, object value);
        IPipelineContext Set(string key, object value);
    }

    public class PipelineContext : IPipelineContext
    {
        private readonly Dictionary<string, object> parameters = new();

        public object this[string key]
        {
            get => parameters[key];
            set => parameters[key] = value;
        }

        public virtual bool TryGetValue(string key, out object value) => parameters.TryGetValue(key, out value);

        public virtual object GetParameter(string key)
        {
            return parameters.TryGetValue(key, out object value) ? value : null;
        }

        public virtual IPipelineContext Replace(params (string key, object value)[] values)
        {
            foreach ((string key, object value) in values)
            {
                if (!parameters.ContainsKey(key))
                    throw new MissingMemberException("");
                parameters[key] = value;
            }
            return this;
        }

        public IPipelineContext Add(string key, object value)
        {
            parameters.Add(key, value);
            return this;
        }

        public IPipelineContext Set(string key, object value)
        {
            parameters[key] = value;
            return this;
        }
    }
}