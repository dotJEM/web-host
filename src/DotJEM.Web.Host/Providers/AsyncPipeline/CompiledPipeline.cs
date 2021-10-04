using System;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{

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

        public override string ToString()
        {
            return $"{context}{Environment.NewLine}{pipeline}";
        }
    }
}