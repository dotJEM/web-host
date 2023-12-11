using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using DotJEM.Web.Host.Tasks;
using DotJEM.Web.Host.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Diagnostics.ExceptionLoggers;

public class DiagnosticsExceptionLogger : IExceptionLogger
{
    private readonly IDiagnosticsLogger logger;
    private readonly IJsonConverter converter;
    private readonly IDiagnosticsDumpService dump;

    public DiagnosticsExceptionLogger(IDiagnosticsLogger logger, IJsonConverter converter, IDiagnosticsDumpService dump)
    {
        this.logger = logger;
        this.converter = converter;
        this.dump = dump;
    }

    public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
    {
        JObject json = converter.ToJObject(new
        {
            context.Exception,
            Trace = EnumeratorUtil.Generate(new StringReader(context.Exception.StackTrace).ReadLine).ToArray(),
            Request = new
            {
                Headers = context.Request.Headers.ToDictionary(pair => pair.Key, p => p.Value),
                RequestUri = context.Request.RequestUri.ToString(),
                context.Request.Method.Method,
                Body = ReadPayload(context),
                Version = context.Request.Version.ToString()
            }
        });
        string message = context.Exception.Message;
        return Task.Factory.StartNew(() =>
        {
            try
            {
                logger.LogFailure(Severity.Error, message, json);
            }
            catch (Exception ex)
            {
                dump.Dump("Failed to log error to diagnostic log: " + Environment.NewLine + Environment.NewLine + ex);
            }
        }, cancellationToken);
    }

    private static string ReadPayload(ExceptionLoggerContext context)
    {
        try
        {
            //TODO: use regular await.
            return Sync.Await(context.Request.Content.ReadAsStringAsync());
        }
        catch (Exception) {
            return null;
        }
    }
}