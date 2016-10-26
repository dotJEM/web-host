using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using Newtonsoft.Json;

namespace DotJEM.Web.Host.Diagnostics.Performance.Correlations
{
    public interface ICorrelation
    {
        Guid Uid { get; }
        string Hash { get; }
        string FullHash { get; }
        // ICorrelationBranch Branch();
        ICorrelation Flow(ICorrelationFlow correlationFlow);
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

        //public ICorrelationBranch Branch() => scope.Branch();

        public ICorrelation Flow(ICorrelationFlow correlationFlow)
        {
            scope.Flow(correlationFlow);
            return this;
        }
    }

    //public interface ICorrelationBranch 
    //{
    //    string Hash { get; }
    //    ICorrelationBranch Root { get; }

    //    void Close();
    //    void Capture(DateTime time, long elapsed, string type, string identity, string[] args);
    //}

    //public class CorrelationBranch : ICorrelationBranch
    //{
    //    private readonly CorrelationScope scope;
    //    private readonly ICorrelationBranch parent;

    //    public Guid Uid { get; } = Guid.NewGuid();

    //    public string Hash => scope.Hash;
    //    public ICorrelationBranch Root => parent ?? this;

    //    public CorrelationBranch(CorrelationScope correlation, ICorrelationBranch parent)
    //    {
    //        this.scope = correlation;
    //        this.parent = parent;


    //    }

    //    public void Capture(DateTime time, long elapsed, string type, string identity, string[] args)
    //    {
    //        throw new NotImplementedException();
    //    }


    //    public void Close()
    //    {
    //        scope.Up(parent);
    //    }
    //}

    public interface ICorrelationScope : IDisposable
    {
        Guid Uid { get; }
        string Hash { get; }
        string FullHash { get; }
       // ICorrelationBranch Branch();
        void Flow(ICorrelationFlow flow);
    }

    public sealed class CorrelationScope : ICorrelationScope
    {
        private const string KEY = "CORRELATION_SCOPE_KEY_4D50C2D";

        public static ICorrelation Current => new Correlation(CallContext.LogicalGetData(KEY) as ICorrelationScope);

        private readonly ConcurrentQueue<ICorrelationFlow> completedFlows = new ConcurrentQueue<ICorrelationFlow>();
        private readonly ConcurrentDictionary<Guid,ICorrelationFlow> flows = new ConcurrentDictionary<Guid, ICorrelationFlow>();

        public Guid Uid { get; }
        public string Hash { get; }
        public string FullHash { get; }

        private readonly Action<CapturedScope> completed;

        internal CorrelationScope(Guid id, Action<CapturedScope> completed)
        {
            this.completed = completed;
            Uid = id;
            FullHash = Hashing.Compute(id.ToByteArray());
            Hash = FullHash.Substring(0, 7);

            CallContext.LogicalSetData(KEY, this);
        }

        public void Flow(ICorrelationFlow flow)
        {
            if(flows.TryAdd(flow.Uid, flow))
                flow.Completed += FlowCompleted;
        }

        private void FlowCompleted(object sender, EventArgs e)
        {
            ICorrelationFlow flow = sender as ICorrelationFlow;
            if (flow != null)
            {
                flow.Completed -= FlowCompleted;
                ICorrelationFlow value = null;
                flows.TryRemove(flow.Uid, out value);

                completedFlows.Enqueue(flow);
                if (flows.IsEmpty)
                {
                    completed(new CapturedScope(Uid, FullHash, completedFlows.ToArray()));
                }
            }
        }

        public void Dispose()
        {
            completed(new CapturedScope(Uid, FullHash, flows.Values.ToArray()));
            CallContext.LogicalSetData(KEY, null);
        }
    }

    public static class Hashing
    {
        public static string Compute(byte[] bytes)
        {
            using (SHA1 hasher = SHA1.Create())
            {
                byte[] hash = hasher.ComputeHash(bytes);
                return string.Join(string.Empty, hash.Select(b => b.ToString("x2")));
            }
        }
    }

    public class CapturedFlow
    {
        public Guid Id { get; }
        public Guid ParentId { get; set; }
        public DateTime Time { get; }
        public long Elapsed { get; }
        public string Hash { get; set; }
        public string Type { get; }
        public string Identity { get; }

        public string[] Arguments { get; }

        public CapturedFlow(Guid parentId, Guid id, string hash, DateTime time, long elapsed, string type, string identity, string[] arguments)
        {
            ParentId = parentId;
            Id = id;
            Hash = hash;
            Time = time;
            Elapsed = elapsed;
            Type = type;
            Identity = identity;
            Arguments = arguments;
        }
    }

    public class CapturedScope
    {
        public Guid Id { get; }
        public string Hash { get; }
        public CapturedFlow[] Flows { get; }
        public DateTime Time { get; } = DateTime.UtcNow;


        public CapturedScope(Guid uid, string hash, ICorrelationFlow[] flows)
        {
            Id = uid;
            Hash = hash;
            Flows = flows.SelectMany(f => f.Flows).ToArray();
        }


    }
}
