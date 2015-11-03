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
        private readonly Hashtable inner = new Hashtable();

        public void Add(object key, object value)
        {
            inner.Add(key, value);
        }

        public object this[object key]
        {
            get { return inner[key]; }
            set { inner[key] = value; }
        }

        public ICollection Keys => inner.Keys;
    }
}
