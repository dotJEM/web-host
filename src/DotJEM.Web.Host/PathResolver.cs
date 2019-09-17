using System.Web.Hosting;

namespace DotJEM.Web.Host
{
    public interface IPathResolver
    {
        string MapPath(string path);
    }

    public class PathResolver : IPathResolver
    {
        public string MapPath(string path)
        {
            return HostingEnvironment.MapPath(path);
        }
    }
}