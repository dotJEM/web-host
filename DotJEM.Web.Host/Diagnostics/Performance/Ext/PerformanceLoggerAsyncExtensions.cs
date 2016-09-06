using System.Threading.Tasks;

namespace DotJEM.Web.Host.Diagnostics.Performance.Ext
{
    public static class PerformanceLoggerAsyncExtensions
    {
        public static async Task<T> TrackTask<T>(this IPerformanceLogger self, string name, Task<T> task, params object[] args)
        {
            using (self.Track(name, args))
                return await task;
        }
    }
}