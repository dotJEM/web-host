using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using DotJEM.Diagnostic;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public class PerformanceLogAspect : IInterceptor
    {
        private readonly ILogger logger;
        private readonly IPerformanceLogAspectSignatureCache cache;

        public PerformanceLogAspect(ILogger logger, IPerformanceLogAspectSignatureCache cache = null)
        {
            this.logger = logger;
            this.cache = cache ?? new DefaultPerformanceLogAspectSignatureCache();
        }

        public void Intercept(IInvocation invocation)
        {
            #pragma warning disable 4014
            AwaitIntercept(invocation);
            #pragma warning restore 4014
        }

        private async Task AwaitIntercept(IInvocation invocation)
        {
            using (logger.Track(invocation.TargetType.Name, 
                new JObject {["target"] = invocation.TargetType.FullName, ["method"] = GetSignature(invocation)}))
            {
                invocation.Proceed();
                if (invocation.ReturnValue is Task task)
                    await task.ConfigureAwait(false);
            }
        }

        private string GetSignature(IInvocation invocation)
        {
            return string.Format(cache.GetOrAdd(invocation.Method.MethodHandle, _ => BuildSignatureFormat()), invocation.Arguments);
            string BuildSignatureFormat()
            {
                string args = string.Join(", ", invocation.Method.GetParameters()
                    .Select((param, i) => param.ParameterType == typeof(string) 
                        ? $"[{param.ParameterType}] \"{{{i}}}\"" 
                        : $"[{param.ParameterType}] {{{i}}}"));
                return $"{invocation.TargetType.Name}::{invocation.Method.Name}({args})";
            }
        }
    }

    public interface IPerformanceLogAspectSignatureCache
    {
        string GetOrAdd(RuntimeMethodHandle key, Func<RuntimeMethodHandle, string> valueFactory);
    }

    public class DefaultPerformanceLogAspectSignatureCache : IPerformanceLogAspectSignatureCache
    {
        private readonly ConcurrentDictionary<RuntimeMethodHandle, string> signatures = new();

        public string GetOrAdd(RuntimeMethodHandle key, Func<RuntimeMethodHandle, string> valueFactory) => signatures.GetOrAdd(key, valueFactory);
    }
}