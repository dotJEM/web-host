using System.Collections.Generic;
using System.IO;

namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface ILogWriterFactory
    {
        ILogWriter Create(string path, long maxSize, int maxFiles, bool compress);
    }

    public class LogWriterFactory : ILogWriterFactory
    {
        private readonly object padlock = new object();
        private readonly IPathResolver resolver;
        private readonly IDictionary<string, ILogWriter> writers = new Dictionary<string, ILogWriter>();

        public LogWriterFactory(IPathResolver path)
        {
            resolver = path;
        }

        public ILogWriter Create(string path, long maxSize, int maxFiles, bool compress)
        {
            path = resolver.MapPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            lock (padlock)
            {
                if (writers.ContainsKey(path))
                    return writers[path];

                return writers[path] = new QueueingLogWriter(path, maxSize, maxFiles, compress);
            }
        }
    }
}