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
        ICorrelationBranch Branch();
    }

    public class Correlation : ICorrelation
    {
        private readonly ICorrelationScope scope;
        private ICorrelationBranch branch;
        private readonly Stack<ICorrelationBranch> branches = new Stack<ICorrelationBranch>();

        public Guid Uid => scope.Uid;
        public string Hash => scope.Hash;
        public string FullHash => scope.FullHash;
        
        public Correlation(ICorrelationScope scope)
        {
            this.scope = scope;
        }

        public ICorrelationBranch Branch()
        {
            return branch = new CorrelationBranch(this, branch);
        }

        public void Up(ICorrelationBranch parent)
        {
            branch = parent;
        }
    }

    public interface ICorrelationBranch 
    {
        string Hash { get; }
        void Close();
        void Capture(DateTime time, long elapsed, string type, string identity, string[] args);
    }

    public class CorrelationBranch : ICorrelationBranch
    {
        private readonly Correlation correlation;
        private readonly ICorrelationBranch parent;

        public string Hash => correlation.Hash;

        public CorrelationBranch(Correlation correlation, ICorrelationBranch parent)
        {
            this.correlation = correlation;
            this.parent = parent;
        }

        public void Capture(DateTime time, long elapsed, string type, string identity, string[] args)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            correlation.Up(parent);
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
            //TODO: 

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

}
