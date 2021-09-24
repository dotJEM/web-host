using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.Web.Host.Providers.AsyncPipeline.Attributes;
using DotJEM.Web.Host.Providers.AsyncPipeline.Factories;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{

    public interface INode { }

    public interface INode<T> : INode
    {
        Task<T> Invoke(IPipelineContext context);
    }

    public interface IPipelineMethod<T>
    {
        PipelineExecutorDelegate<T> Target { get; }
        NextFactoryDelegate<T> NextFactory { get; }
        string Signature { get; }
    }

    public class TerminationMethod<T> : IPipelineMethod<T>
    {
        public TerminationMethod(PipelineExecutorDelegate<T> target)
        {
            Target = target;
        }

        public PipelineExecutorDelegate<T> Target { get; }
        public NextFactoryDelegate<T> NextFactory { get; } = (_, _) => null;
        public string Signature => "PipelineTermination";
    }

    public interface IClassNode<T>
    {
        IEnumerable<MethodNode<T>> For(IPipelineContext context);
    }

    public class ClassNode<T> : IClassNode<T>
    {
        private readonly List<MethodNode<T>> nodes;

        public ClassNode(List<MethodNode<T>> nodes)
        {
            this.nodes = nodes;
        }

        public IEnumerable<MethodNode<T>> For(IPipelineContext context)
        {
            return nodes.Where(n => n.Accepts(context));
        }
    }


    public class MethodNode<T> : IPipelineMethod<T>
    {
        private readonly PipelineFilterAttribute[] filters;
        public string Signature { get; }

        public PipelineExecutorDelegate<T> Target { get; }
        public NextFactoryDelegate<T> NextFactory { get; }

        public MethodNode(PipelineFilterAttribute[] filters, PipelineExecutorDelegate<T> target, NextFactoryDelegate<T> nextFactory, string signature)
        {
            this.filters = filters;
            this.Signature = signature;
            this.Target = target;
            NextFactory = nextFactory;
        }

        public bool Accepts(IPipelineContext context)
        {
            return filters.All(selector => selector.Accepts(context));
        }
    }
}