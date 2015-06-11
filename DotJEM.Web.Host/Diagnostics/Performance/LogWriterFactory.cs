using System.Collections.Concurrent;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface ILogWriterFactory
    {
        ILogWriter Create(string path, long maxSize, int maxFiles, bool compress);
    }

    public class LogWriterFactory : ILogWriterFactory
    {
        private readonly IPathResolver resolver;
        private readonly ConcurrentDictionary<string, ILogWriter> writers = new ConcurrentDictionary<string, ILogWriter>();

        public LogWriterFactory(IPathResolver path)
        {
            resolver = path;
        }

        public ILogWriter Create(string path, long maxSize, int maxFiles, bool compress)
        {
            path = resolver.MapPath(path);
            return writers.GetOrAdd(path, s => new QueueingLogWriter(path, maxSize, maxFiles, compress));
        }
    }
}