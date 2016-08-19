using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Diagnostics.Performance.Correlations
{
    public interface ICorrelation
    {
        Guid Uid { get; }
        string Hash { get; }
        string FullHash { get; }
    }

    public class Correlation : ICorrelation
    {
        private readonly ICorrelationScope scope;

        public Guid Uid => scope.Uid;
        public string Hash => scope.Hash;
        public string FullHash => scope.FullHash;

        public Correlation(ICorrelationScope scope)
        {
            this.scope = scope;
        }
    }

    public interface ICorrelationScope : IDisposable
    {
        Guid Uid { get; }
        string Hash { get; }
        string FullHash { get; }
    }

    public sealed class CorrelationScope : ICorrelationScope
    {
        private const string KEY = "CORRELATION_SCOPE_KEY_4D50C2D";

        public static ICorrelation Current => new Correlation(CallContext.LogicalGetData(KEY) as ICorrelationScope);

        public Guid Uid { get; }
        public string Hash { get; }
        public string FullHash { get; }

        internal CorrelationScope(Guid id)
        {
            Uid = id;
            FullHash = ComputeHash(id.ToByteArray());
            Hash = FullHash.Substring(0, 7);

            CallContext.LogicalSetData(KEY, this);
        }

        public void Dispose()
        {
            CallContext.LogicalSetData(KEY, null);
        }

        private static string ComputeHash(byte[] bytes)
        {
            using (SHA1 hasher = SHA1.Create())
            {
                byte[] hash = hasher.ComputeHash(bytes);
                return string.Join(string.Empty, hash.Select(b => b.ToString("x2")));
            }
        }
    }

    //TODO: Injectable service.
    //public static class Correlator
    //{
    //    private const string CORRELATION_KEY = "CORRELATION_KEY";
    //    private const string EMPTY = "00000000";

    //    public static void Set(Guid id)
    //    {
    //        CallContext.LogicalSetData(CORRELATION_KEY, Hash(id.ToByteArray(), 5));
    //    }

    //    public static string Get()
    //    {
    //        string ctx = (string)CallContext.LogicalGetData(CORRELATION_KEY);
    //        return ctx ?? EMPTY;
    //    }

    //    private static string Hash(byte[] bytes, int size)
    //    {
    //        using (SHA1 hasher = SHA1.Create())
    //        {
    //            byte[] hash = hasher.ComputeHash(bytes);
    //            return string.Join(string.Empty, hash.Take(size).Select(b => b.ToString("x2")));
    //        }
    //    }
    //}
}
