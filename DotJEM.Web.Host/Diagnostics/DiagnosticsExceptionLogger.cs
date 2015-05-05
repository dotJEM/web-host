using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Diagnostics
{
    public class DiagnosticsExceptionLogger : IExceptionLogger
    {
        private readonly IDiagnosticsLogger logger;
        private readonly IJsonConverter converter;

        public DiagnosticsExceptionLogger(IDiagnosticsLogger logger, IJsonConverter converter)
        {
            this.logger = logger;
            this.converter = converter;
        }

        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            var json = converter.ToJObject(new
            {
                context.Exception,
                Trace = EnumeratorUtil.Generate(new StringReader(context.Exception.StackTrace).ReadLine).ToArray(),
                Request = new
                {
                    Headers = context.Request.Headers.ToDictionary(pair => pair.Key, p => p.Value),
                    RequestUri = context.Request.RequestUri.ToString(),
                    context.Request.Method.Method,
                    Version = context.Request.Version.ToString()
                }
            });
            string message = context.Exception.Message;
            return Task.Factory.StartNew(() =>
            {
                logger.LogFailure(Severity.Error, message, json);
            }, cancellationToken);
        }
    }

    public static class EnumeratorUtil
    {
        public static IEnumerable<T> Generate<T>(Func<T> generatorFunc, Func<T, bool> breakFunc = null)
        {
            T def = default(T);
            breakFunc = breakFunc ?? (v => Equals(v, def));
            while (true)
            {
                T value = generatorFunc();
                if (breakFunc(value)) 
                    yield break;

                yield return value;
            }
        } 
    }
}