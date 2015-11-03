using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotJEM.Web.Host
{
    //TODO: (jmd 2015-10-01) Task and Async. 
    public interface IDiagnosticsDumpService
    {
        void Dump(string message);
        void Dump(Guid id, string message);
    }

    public class DiagnosticsDumpService : IDiagnosticsDumpService
    {
        private readonly IPathResolver path;

        public DiagnosticsDumpService(IPathResolver path)
        {
            this.path = path;
        }

        public void Dump(string message)
        {
            Dump(Guid.NewGuid(), message);
        }

        public void Dump(Guid id, string message)
        {
            Task.Run(() => InternalDump(id, message));
        }

        private void InternalDump(Guid id, string message)
        {
            string dumpPath = path.MapPath($"/APP_DATA/dump/dump-{id.ToString("N")}.dmp");
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dumpPath));
                    File.WriteAllText(dumpPath, message);
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                    // ignored
                }
            }
        }
    }
}