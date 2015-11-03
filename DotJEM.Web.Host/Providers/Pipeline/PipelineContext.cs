using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public class PipelineContext
    {
        private readonly IDictionary<string, object> inner = new Dictionary<string, object>();

        public void Add(string key, object value)
        {
            inner.Add(key, value);
        }

        public object this[string key]
        {
            get { return inner[key]; }
            set { inner[key] = value; }
        }

        public ICollection<string> Keys => inner.Keys;
    }
}
