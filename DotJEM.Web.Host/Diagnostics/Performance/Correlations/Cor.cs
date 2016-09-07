using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;

namespace DotJEM.Web.Host.Diagnostics.Performance.Correlations
{
    public interface ICorrelation
    {
        Guid Uid { get; }
        string Hash { get; }
        string FullHash { get; }
        ICorrelationBranch Branch();
    }

    /// <summary>
    /// This is a proxy object, it's meant to remove Dispose from the interface returned by CorrelationScope.Current
    /// </summary>
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

        public ICorrelationBranch Branch() => scope.Branch();
    }

    public interface ICorrelationBranch 
    {
        string Hash { get; }
        ICorrelationBranch Root { get; }

        void Close();
        void Capture(DateTime time, long elapsed, string type, string identity, string[] args);
    }

    public class CorrelationBranch : ICorrelationBranch
    {
        private readonly CorrelationScope scope;
        private readonly ICorrelationBranch parent;

        public Guid Uid { get; } = Guid.NewGuid();

        public string Hash => scope.Hash;
        public ICorrelationBranch Root => parent ?? this;

        public CorrelationBranch(CorrelationScope correlation, ICorrelationBranch parent)
        {
            this.scope = correlation;
            this.parent = parent;


        }

        public void Capture(DateTime time, long elapsed, string type, string identity, string[] args)
        {
            throw new NotImplementedException();
        }


        public void Close()
        {
            scope.Up(parent);
        }
    }

    public interface ICorrelationScope : IDisposable
    {
        Guid Uid { get; }
        string Hash { get; }
        string FullHash { get; }
        ICorrelationBranch Branch();
    }

    public sealed class CorrelationScope : ICorrelationScope
    {
        private const string KEY = "CORRELATION_SCOPE_KEY_4D50C2D";

        public static ICorrelation Current => new Correlation(CallContext.LogicalGetData(KEY) as ICorrelationScope);

        private ICorrelationBranch currentBranch;
        private readonly List<ICorrelationBranch> branches = new List<ICorrelationBranch>();
        private readonly object padlock = new object();

        public Guid Uid { get; }
        public string Hash { get; }
        public string FullHash { get; }

        private readonly Action<PerformanceTrack> completed;

        internal CorrelationScope(Guid id, Action<PerformanceTrack> completed)
        {
            this.completed = completed;
            Uid = id;
            FullHash = ComputeHash(id.ToByteArray());
            Hash = FullHash.Substring(0, 7);

            CallContext.LogicalSetData(KEY, this);
        }

        public ICorrelationBranch Branch()
        {
            lock (padlock)
            {
                currentBranch = new CorrelationBranch(this, currentBranch);
                branches.Add(currentBranch);
                return currentBranch;
            }
        }

        public void Up(ICorrelationBranch parent)
        {
            lock (padlock)
            {
                currentBranch = parent;
            }
        }

        public void Dispose()
        {
            completed(new PerformanceTrack(Uid, FullHash, branches));


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

    public class PerformanceTrack
    {
    }
}
