using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Pipeline
{
    public interface IPipelineManager
    {
        IPipeline Lookup(string contentType);
    }

    public class PipelineManager : IPipelineManager
    {
        private readonly List<IPipelineStep> steps = new List<IPipelineStep>(); 
        private readonly Dictionary<string, IPipeline> pipelines = new Dictionary<string, IPipeline>();

        public PipelineManager()
        {

        }

        

        public IPipeline Lookup(string contentType)
        {
            if (!pipelines.ContainsKey(contentType))
                return InitializePipeline(contentType);
            return pipelines[contentType];
        }

        private IPipeline InitializePipeline(string contentType)
        {
            return pipelines[contentType] = new Pipline(steps.Where(step => step.Accepts(contentType)).ToList());
        }

        public void Invalidate()
        {
            pipelines.Clear();
        }
    }

    public interface IPipeline
    {
        JObject Execute(JObject json);
    }

    public class Pipline : IPipeline
    {
        private readonly IEnumerable<IPipelineStep> steps;

        public Pipline(IEnumerable<IPipelineStep> steps)
        {
            this.steps = steps;
        }

        public JObject Execute(JObject json)
        {
            return steps.Aggregate(json, (jo, step) => step.Execute(jo));
        }
    }

    public interface IPipelineStep
    {
        bool Accepts(string contentType);

        JObject Execute(JObject json);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PipelineAcceptsAttribute : Attribute
    {
        public string[] ContentTypes { get; private set; }

        public PipelineAcceptsAttribute(params string[] contentTypes)
        {
            ContentTypes = contentTypes;
        }
    }

    public abstract class DinamicPipelineStep : IPipelineStep
    {
        private readonly Lazy<HashSet<string>> accepts;

        protected DinamicPipelineStep()
        {
            accepts = new Lazy<HashSet<string>>(Initialize);
        }

        public virtual bool Accepts(string contentType)
        {
            return accepts.Value.Contains(contentType);
        }

        public JObject Execute(JObject json)
        {
            return OnExecute(json);
        }

        private HashSet<string> Initialize()
        {
            PipelineAcceptsAttribute attr = (PipelineAcceptsAttribute)GetType()
                .GetCustomAttributes(typeof(PipelineAcceptsAttribute), false)
                .FirstOrDefault();
            return attr != null ? new HashSet<string>(attr.ContentTypes) : new HashSet<string>();
        }

        public abstract dynamic OnExecute(dynamic dynamic);
    }
}
