using System;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Providers.AsyncPipeline
{

    public interface ICompiledPipeline<T>
    {
        Task<T> Invoke();
    }

    public class CompiledPipeline<T> : ICompiledPipeline<T>
    {
        private readonly IPipelineContext context;
        private readonly IUnboundPipeline<T> pipeline;

        public CompiledPipeline(IUnboundPipeline<T> pipeline, IPipelineContext context)
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