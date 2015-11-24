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

        void Add(string key, object value);
    }

    public class PipelineContext : DynamicObject, IPipelineContext
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

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return inner.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value;
            return true;
        }

        #region Dispose pattern
        protected volatile bool Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            Disposed = true;
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
