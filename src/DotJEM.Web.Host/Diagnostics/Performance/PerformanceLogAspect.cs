using System;
using System.Collections.Concurrent;
using Castle.DynamicProxy;
using DotJEM.Diagnostic;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public class PerformanceLogAspect : IInterceptor
    {
        private readonly ILogger logger;
        private readonly ConcurrentDictionary<RuntimeMethodHandle, string> signatures = new();

        public PerformanceLogAspect(ILogger logger)
        {
            this.logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            using (logger.Track("Method", new JObject {["target"] = GetSignature(invocation)})) 
                invocation.Proceed();
        }

        private string GetSignature(IInvocation invocation)
        {
            return signatures.GetOrAdd(invocation.Method.MethodHandle, _ => $"{invocation.TargetType.Name}::{invocation.Method}");
        }
    }
}