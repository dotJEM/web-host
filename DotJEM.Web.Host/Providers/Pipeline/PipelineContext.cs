using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Query.Dynamic;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public interface IPipelineContext : IDisposable
    {
        object this[string key] { get; set; }
        int Count { get; }
        ICollection<string> Keys { get; }

        void Add(string key, object value);
        bool TryGetValue(string key, out object value);
        bool ContainsKey(string key);
    }

    public class PipelineContext : DynamicObject, IPipelineContext
    {
        private readonly IDictionary<string, object> inner = new Dictionary<string, object>();

        public int Count => inner.Count;

        public ICollection<string> Keys => inner.Keys;
        public ICollection<object> Values => inner.Values;

        public bool TryGetValue(string key, out object value) => inner.TryGetValue(key, out value);
        public void Add(string key, object value) => inner.Add(key, value);
        public bool ContainsKey(string key) => inner.ContainsKey(key);

        public object GetOrAdd(string key, Func<string, object> factory)
        {
            object result;
            if (inner.TryGetValue(key, out result))
                return result;

            result = factory(key);
            inner.Add(key, result);
            return result;
        }

        public object this[string key]
        {
            get
            {
                object result;
                if(inner.TryGetValue(key, out result))
                    return result;
                return null;
            }
            set { inner[key] = value; }
        }

        #region Dynamic Members
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            inner.TryGetValue(binder.Name, out result);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value;
            return true;
        }
        #endregion

        #region Dispose pattern
        private volatile bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            disposed = true;
        }

        ~PipelineContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
