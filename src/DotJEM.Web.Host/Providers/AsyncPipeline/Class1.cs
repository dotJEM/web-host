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


    public interface IPipelineMethod<T>
    {
        PipelineExecutorDelegate<T> Target { get; }
        NextFactoryDelegate<T> NextFactory { get; }
        string Signature { get; }
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


    public interface ICompiledPipeline<T>
    {
        Task<T> Invoke();
    }

    public class CompiledPipeline<TContext, T> : ICompiledPipeline<T> where TContext : IPipelineContext
    {
        private readonly TContext context;
        private readonly IUnboundPipeline<TContext, T> pipeline;

        public CompiledPipeline(IUnboundPipeline<TContext, T> pipeline, TContext context)
        {
            this.pipeline = pipeline;
            this.context = context;
        }
        
        public Task<T> Invoke() => pipeline.Invoke(context);
    }

    public interface IUnboundPipeline<in TContext, T> where TContext : IPipelineContext
    {
        Task<T> Invoke(TContext context);
    }

    public class UnboundPipeline<TContext, T> : IUnboundPipeline<TContext, T> where TContext : IPipelineContext
    {
        private readonly INode<T> target;

        public UnboundPipeline(ILogger performance, Func<IPipelineContext, JObject> perfGenerator, IEnumerable<MethodNode<T>> nodes, Func<TContext, Task<T>> final)
        {
            if (performance.IsEnabled())
            {
                this.target = nodes.Reverse()
                    .Aggregate(
                        (INode<T>)new PNode<T>(performance,perfGenerator, new TerminationMethod<T>((context, _) => final((TContext)context)), null),
                        (node, methodNode) => new PNode<T>(performance,perfGenerator, methodNode, node));
            }
            else
            {
                this.target = nodes.Reverse()
                    .Aggregate(
                        (INode<T>)new Node<T>(new TerminationMethod<T>((context, _) => final((TContext)context)), null),
                        (node, methodNode) => new Node<T>(methodNode, node));
            }
        }

        public Task<T> Invoke(TContext context)
        {
            return target.Invoke(context);
        }

        private class PNode<T> : INode<T>
        {
            private readonly INode<T> next;
            private readonly PipelineExecutorDelegate<T> target;
            private readonly NextFactoryDelegate<T> factory;
            private readonly ILogger performance;
            private readonly Func<IPipelineContext, JObject> perfGenerator;
            private readonly string signature;

            public PNode(ILogger performance, Func<IPipelineContext, JObject> perfGenerator, IPipelineMethod<T> method, INode<T> next)
            {
                this.performance = performance;
                this.perfGenerator = perfGenerator;
                this.next = next;
                this.factory = method.NextFactory;
                this.target = method.Target;
                this.signature = method.Signature;
            }

            public async Task<T> Invoke(IPipelineContext context)
            {
                //TODO: Here we generate the same JObject again and again, however it may be faster than reusing and clearing correctly.
                JObject info = perfGenerator(context);
                info["$$handler"] = signature;
                using (performance.Track("pipeline", info))
                    return await target(context, factory(context, next));
            }
        }

        private class Node<T> : INode<T>
        {
            private readonly INode<T> next;
            private readonly PipelineExecutorDelegate<T> target;
            private readonly NextFactoryDelegate<T> factory;

            public Node(IPipelineMethod<T> method, INode<T> next)
            {
                this.next = next;
                this.factory = method.NextFactory;
                this.target = method.Target;
            }

            public Task<T> Invoke(IPipelineContext context)
            {
                return target(context, factory(context, next));
            }
        }
    }

    public interface INode { }

    public interface INode<T> : INode
    {
        Task<T> Invoke(IPipelineContext context);
    }

    public interface IPipelineHandler
    {
    }

    public interface IPipelineContext
    {
        bool TryGetValue(string key, out object value);

        object GetParameter(string key);

        IPipelineContext Replace(params (string key, object value)[] values);
    }

    public class PipelineContext : IPipelineContext
    {
        private readonly Dictionary<string, object> parameters = new();

        public virtual bool TryGetValue(string key, out object value) => parameters.TryGetValue(key, out value);

        public virtual object GetParameter(string key)
        {
            return parameters.TryGetValue(key, out object value) ? value : null;
        }

        public virtual IPipelineContext Replace(params (string key, object value)[] values)
        {
            foreach ((string key, object value) in values)
                parameters[key] = value;
            return this;
        }

        public PipelineContext Set(string key, object value)
        {
            parameters[key] = value;
            return this;
        }
    }
}
