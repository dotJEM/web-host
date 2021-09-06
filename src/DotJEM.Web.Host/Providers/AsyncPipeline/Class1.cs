using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using DotJEM.Diagnostic;
using DotJEM.Web.Host.Diagnostics.Performance;
using DotJEM.Web.Host.Providers.AsyncPipeline.Attributes;
using DotJEM.Web.Host.Providers.AsyncPipeline.Factories;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{
    public class AsyncPipelineInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IPipelines>().ImplementedBy<PipelineManager>().LifestyleTransient());
        }
    }


    /* NEW CONCEPT: Named pipelines */


    //Task<JObject> Execute<TContext>(TContext context, Func<TContext, JObject> finalize) where TContext : IPipelineContext;


    public interface IPipelineMethod
    {
        PipelineExecutorDelegate Target { get; }
        NextFactoryDelegate NextFactory { get; }
        string Signature { get; }
    }

    public class MethodNode : IPipelineMethod
    {
        private readonly PipelineFilterAttribute[] filters;
        public string Signature { get; }

        public PipelineExecutorDelegate Target { get; }
        public NextFactoryDelegate NextFactory { get; }

        public MethodNode(PipelineFilterAttribute[] filters, PipelineExecutorDelegate target, NextFactoryDelegate nextFactory, string signature)
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

    public class TerminationMethod : IPipelineMethod
    {
        public TerminationMethod(PipelineExecutorDelegate target)
        {
            Target = target;
        }

        public PipelineExecutorDelegate Target { get; }
        public NextFactoryDelegate NextFactory { get; } = (_, _) => null;
        public string Signature => "PipelineTermination";
    }

    public interface IClassNode
    {
        IEnumerable<MethodNode> For(IPipelineContext context);
    }

    public class ClassNode : IClassNode
    {
        private readonly List<MethodNode> nodes;

        public ClassNode(List<MethodNode> nodes)
        {
            this.nodes = nodes;
        }

        public IEnumerable<MethodNode> For(IPipelineContext context)
        {
            return nodes.Where(n => n.Accepts(context));
        }
    }

    public interface ICompiledPipeline<in TContext> where TContext : IPipelineContext
    {
        Task<JObject> Invoke(TContext context);
    }

    public class CompiledPipeline<TContext> : ICompiledPipeline<TContext> where TContext : IPipelineContext
    {
        private readonly INode target;

        public CompiledPipeline(ILogger performance, Func<IPipelineContext, JObject> perfGenerator, IEnumerable<MethodNode> nodes, Func<TContext, Task<JObject>> final)
        {
            if (performance.IsEnabled())
            {
                this.target = nodes.Reverse()
                    .Aggregate(
                        (INode)new PNode(performance,perfGenerator, new TerminationMethod((context, _) => final((TContext)context)), null),
                        (node, methodNode) => new PNode(performance,perfGenerator, methodNode, node));
            }
            else
            {
                this.target = nodes.Reverse()
                    .Aggregate(
                        (INode)new Node(new TerminationMethod((context, _) => final((TContext)context)), null),
                        (node, methodNode) => new Node(methodNode, node));
            }
        }

        public Task<JObject> Invoke(TContext context)
        {
            return target.Invoke(context);
        }

        private class PNode : INode
        {
            private readonly INode next;
            private readonly PipelineExecutorDelegate target;
            private readonly NextFactoryDelegate factory;
            private readonly ILogger performance;
            private readonly Func<IPipelineContext, JObject> perfGenerator;
            private readonly string signature;

            public PNode(ILogger performance, Func<IPipelineContext, JObject> perfGenerator, IPipelineMethod method, INode next)
            {
                this.performance = performance;
                this.perfGenerator = perfGenerator;
                this.next = next;
                this.factory = method.NextFactory;
                this.target = method.Target;
                this.signature = method.Signature;
            }

            public async Task<JObject> Invoke(IPipelineContext context)
            {
                //TODO: Here we generate the same JObject again and again, however it may be faster than reusing and clearing correctly.
                JObject info = perfGenerator(context);
                info["$$handler"] = signature;
                using (performance.Track("pipeline", info))
                    return await target(context, factory(context, next));
            }
        }

        private class Node : INode
        {
            private readonly INode next;
            private readonly PipelineExecutorDelegate target;
            private readonly NextFactoryDelegate factory;

            public Node(IPipelineMethod method, INode next)
            {
                this.next = next;
                this.factory = method.NextFactory;
                this.target = method.Target;
            }

            public Task<JObject> Invoke(IPipelineContext context)
            {
                return target(context, factory(context, next));
            }
        }
    }

    public interface INode
    {
        Task<JObject> Invoke(IPipelineContext context);
    }

    public interface IPipelineHandler
    {
    }

    public interface IPipelineContext
    {
        bool TryGetValue(string key, out string value);

        object GetParameter(string key);

        IPipelineContext Replace(params (string key, object value)[] values);
    }


}
