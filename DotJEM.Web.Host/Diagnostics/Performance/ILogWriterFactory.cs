namespace DotJEM.Web.Host.Diagnostics.Performance
{
    public interface ILogWriterFactory
    {
        ILogWriter Create(string path, long maxSize, int maxFiles, bool compress);
    }
}