using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Diagnostics
{
    public class DiagnosticsLoggingHandler : DelegatingHandler
    {
        private readonly IPerformanceLogger logger;

        public DiagnosticsLoggingHandler(IPerformanceLogger logger)
        {
            this.logger = logger;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if(!logger.Enabled)
                return await base.SendAsync(request, cancellationToken);
            
            PerformanceTracker tracker = logger.Track(request);
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            tracker.Trace(response.StatusCode);
            return response;
        }
    }

    public interface IPerformanceLogger
    {
        bool Enabled { get; }
        PerformanceTracker Track(HttpRequestMessage request);
    }

    public class PerformanceLogger : IPerformanceLogger
    {
        public bool Enabled { get; private set; }

        private readonly ILogWriter writer;

        public PerformanceLogger(ILogWriterFactory factory, IWebHostConfiguration configuration)
        {
            //TODO: Null logger pattern
            if (configuration.Diagnostics == null || configuration.Diagnostics.Performance == null)
                return;

            Enabled = true;
            PerformanceConfiguration config = configuration.Diagnostics.Performance;

            string dir = Path.GetDirectoryName(config.Path);
            Debug.Assert(dir != null, "dir != null");

            Directory.CreateDirectory(dir);
            writer = factory.Create(config.Path, AdvConvert.ToByteCount(config.MaxSize), config.MaxFiles, config.Zip);
        }

        public PerformanceTracker Track(HttpRequestMessage request)
        {
            return new PerformanceTracker(request, LogPerformanceEvent);
        }

        private void LogPerformanceEvent(PerformanceTracker tracker)
        {
            if (!Enabled)
                return;

            writer.Write(tracker.ToString());
        }
    }

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

    public interface ILogWriter : IDisposable
    {
        void Write(string message);
        void Close();
    }

    public class QueueingLogWriter : ILogWriter
    {
        private readonly string path;
        private readonly long maxSize;
        private readonly int maxFiles;
        private readonly bool compress;
        private readonly string name;
        private readonly string directory;
        private readonly string extention;

        private readonly object padLock = new object();
        private readonly Queue<string> logQueue = new Queue<string>();
        private readonly Thread thread;
 
        private bool disposed;
        private StreamWriter current;
        private FileInfo file;

        public QueueingLogWriter(string path, long maxSize, int maxFiles, bool compress)
        {
            this.file = new FileInfo(path);
            this.path = path;
            this.maxSize = maxSize;
            this.maxFiles = maxFiles;
            this.compress = compress;
            this.name = Path.GetFileNameWithoutExtension(path);
            this.extention = Path.GetExtension(path);
            this.directory = Path.GetDirectoryName(path);
            this.current = new StreamWriter(path, true);

            thread = new Thread(WriteLoop);
            thread.Start();
        }

        public void Write(string message)
        {
            if(disposed)
                return;

            logQueue.Enqueue(message);
            if (logQueue.Count > 32)
            {
                lock (padLock)
                {
                    Monitor.PulseAll(padLock);
                }
            }
        }

        private void WriteLoop()
        {
            try
            {
                lock (padLock)
                {
                    while (true)
                    {
                        if (logQueue.Count < 1)
                            Monitor.Wait(padLock);
                        Flush(32);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Flush(logQueue.Count);
            }
        }

        private StreamWriter NextWriter()
        {
            file.Refresh();
            if (file.Length <= maxSize) return current;

            if (current != null)
                current.Close();

            Archive();

            return current = new StreamWriter(path, true);
        }

        private void Archive()
        {
            file.MoveTo(Path.Combine(directory, GenerateUniqueLogName()));
            file = new FileInfo(path);

            DirectoryInfo dir = new DirectoryInfo(directory);
            var logFiles = dir.GetFiles(name + "-*" + extention);
            if (logFiles.Length >= maxFiles)
            {
                if (compress)
                {
                    string zipname = Path.Combine(directory, GenerateUniqueArchiveName());
                    using (ZipArchive archive = ZipFile.Open(zipname, ZipArchiveMode.Create))
                    {
                        foreach (FileInfo f in logFiles)
                        {
                            try
                            {
                                archive.CreateEntryFromFile(f.FullName, f.Name);
                                f.Delete();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                            }
                        }
                    }

                    //TODO: If to many zip files.
                }
                else
                {
                    FileInfo oldest = dir.GetFiles(name + "-*" + extention).OrderByDescending(f => f.CreationTime).First();
                    oldest.Delete();
                }
            }
        }


        private string GenerateUniqueArchiveName()
        {
            return name + "-" + Guid.NewGuid().ToString("N") + ".zip";
        }

        private string GenerateUniqueLogName()
        {
            return name + "-" + Guid.NewGuid().ToString("N") + extention;
        }

        private void Flush(int count)
        {
            StreamWriter writer = NextWriter();
            while (logQueue.Count > 0 && count-- > 0)
            {
                writer.WriteLine(logQueue.Dequeue());
            }
            
            if (logQueue.Count > 0)
            {
                Flush(32);
                return;
            }

            writer.Flush();
        }

        public void Dispose()
        {
            disposed = true;
            thread.Abort();
            thread.Join();
        }

        public void Close()
        {
            Dispose();
        }
    }

    public class PerformanceTracker 
    {
        private readonly Action<PerformanceTracker> completed;

        private readonly string method;
        private readonly string uri;
        private readonly string user;
        private readonly long start;
        private readonly DateTime time;

        private HttpStatusCode status;
        private long end;

        public PerformanceTracker(HttpRequestMessage request, Action<PerformanceTracker> completed)
        {
            this.completed = completed;
            time = DateTime.UtcNow;
            method = request.Method.Method;
            uri = request.RequestUri.ToString();
            user = ClaimsPrincipal.Current.Identity.Name;
            start = Stopwatch.GetTimestamp();
        }

        public void Trace(HttpStatusCode status)
        {
            this.status = status;
            end = Stopwatch.GetTimestamp();

            Task.Run(() => completed(this));
        }

        public override string ToString()
        {
            return string.Format("{0:s}, {1}, {2}, {3}, {4}, {5}", time, method, uri, status, user, end - start);
        }
    }

    public class AdvConvert
    {
        private static readonly Regex timeSpanExpression = new Regex(@"((?'d'[0-9]+)\s?d(ay(s)?)?)?\s?" +
                                                                     @"((?'h'[0-9]+)\s?h(our(s)?)?)?\s?" +
                                                                     @"((?'m'[0-9]+)\s?m(in(ute(s)?)?)?)?\s?" +
                                                                     @"((?'s'[0-9]+)\s?s(ec(ond(s)?)?)?)?\s?" +
                                                                     @"((?'f'[0-9]+)\s?f(rac(tion(s)?)?)?|ms|millisecond(s)?)?\s?",
                                                                     RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex byteCountExpression = new Regex(@"((?'g'[0-9]+)\s?gb|gigabyte(s)?)?\s?" +
                                                                      @"((?'m'[0-9]+)\s?mb|megabyte(s)?)?\s?" +
                                                                      @"((?'k'[0-9]+)\s?kb|kilobyte(s)?)?\s?" +
                                                                      @"((?'b'[0-9]+)\s?b|byte(s)?)?\s?",
                                                                      RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Atempts to convert a string to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <remarks>
        /// The method first attempts to use the normal <see cref="TimeSpan.Parse"/> method, if that fails it then usesuses a range of wellknown formats
        /// to atempt a conversion of a string representing a <see cref="TimeSpan"/>.
        /// <p/>The order of which the values are defined must always be "Days, Hours, Minutes, Seconds and Fractions" But non of them are required,
        /// that means that a valid format could be '5 days 30 min' as well as '3h', and spaces are alowed between each value and it's unit definition.
        /// <p/>The folowing units are known.
        /// <table>
        /// <tr><td>Days</td><td>d, day, days</td></tr>
        /// <tr><td>Hours</td><td>h, hour, hours</td></tr>
        /// <tr><td>Minutes</td><td>m, min, minute, minutes</td></tr>
        /// <tr><td>Seconds</td><td>s, sec, second, seconds</td></tr>
        /// <tr><td>Fractions</td><td>f, frac, fraction. fractions, ms, millisecond, milliseconds</td></tr>
        /// </table>
        /// <p/>All Unit definitions ignores any casing.
        /// </remarks>
        /// <param name="input">A string representing a <see cref="TimeSpan"/>.</param>
        /// <returns>A TimeSpan from the given input.</returns>
        /// <example>
        /// This piece of code first parses the string "2m 30s" to a <see cref="TimeSpan"/> and then uses that <see cref="TimeSpan"/> to sleep for 2 minutes and 30 seconds.
        /// <code>
        /// public void SleepForSomeTime()
        /// {
        ///   //Two and a half minute.
        ///   TimeSpan sleep = Convert.ToTimeSpan("2m 30s");
        ///   Thread.Spleep(sleep);
        /// }
        /// </code>
        /// </example>
        /// <exception cref="FormatException">The given input could not be converted to a <see cref="TimeSpan"/> because the format was invalid.</exception>
        public static TimeSpan ToTimeSpan(string input)
        {
            TimeSpan outPut;
            if (TimeSpan.TryParse(input, out outPut))
                return outPut;

            Match match = timeSpanExpression.Match(input);
            if (match == null || !match.Success)
                throw new FormatException("Input string was not in a correct format.");

            int days = ParseGroup(match.Groups["d"]);
            int hours = ParseGroup(match.Groups["h"]); ;
            int minutes = ParseGroup(match.Groups["m"]); ;
            int seconds = ParseGroup(match.Groups["s"]); ;
            int milliseconds = ParseGroup(match.Groups["f"]); ;
            return new TimeSpan(days, hours, minutes, seconds, milliseconds);
        }

        /// <summary>
        /// Atempts to convert a string to <see cref="long"/> value as a number of bytes.
        /// </summary>
        /// <remarks>
        /// The method usesuses a range of wellknown formats to atempt a conversion of a string representing a size in bytes.
        /// <p/>The order of which the values are defined must always be "Gigabytes, Megabytes, Kilobytes, and Bytes" But non of them are required,
        /// that means that a valid format could be '5 gigabytes 512 bytes' as well as '3kb', and spaces are alowed between each value and it's unit definition.
        /// <p/>The folowing units are known.
        /// <table>
        /// <tr><td>Gigabytes</td><td>gb, gigabyte, gigabytes</td></tr>
        /// <tr><td>Megabytes</td><td>mb, megabyte, megabytes</td></tr>
        /// <tr><td>Kilobytes</td><td>kb, kilobyte, kilobytes</td></tr>
        /// <tr><td>Bytes</td><td>b, byte, bytes</td></tr>
        /// </table>
        /// <p/>All Unit definitions ignores any casing.
        /// </remarks>
        /// <param name="input">A string representing a total number of bytes as Gigabytes, Megabytes, Kilobytes and Bytes.</param>
        /// <returns>A <see cref="long"/> calculated as the total number of bytes from the given input.</returns>
        /// <example>
        /// This piece of code first parses the string "25mb 512kb" to a long and then uses to write an empty file in the "C:\Temp" folder.
        /// <code>
        /// public void WriteSomeFile()
        /// {
        ///   long lenght = Convert.ToByteCount("25mb 512kb");
        ///   FileHelper.CreateTextFile("C:\temp", new byte[lenght], true);
        /// }
        /// </code>
        /// </example>
        /// <exception cref="FormatException">The given input could not be converted because the format was invalid.</exception>
        public static long ToByteCount(string input)
        {
            Match match = byteCountExpression.Match(input);
            if (match == null || !match.Success)
                throw new FormatException("Input string was not in a correct format.");

            long gigaBytes = ParseGroup(match.Groups["g"]);
            long megaBytes = ParseGroup(match.Groups["m"]); ;
            long kiloBytes = ParseGroup(match.Groups["k"]); ;
            long bytes = ParseGroup(match.Groups["b"]); ;
            return bytes + (1024L * (kiloBytes + (1024L * (megaBytes + (1024L * gigaBytes)))));
        }

        public static TEnumeration ToEnum<TEnumeration>(string state)
        {
            return (TEnumeration)Enum.Parse(typeof(TEnumeration), state, true);
        }

        private static int ParseGroup(Group group)
        {
            if (group == null || string.IsNullOrEmpty(group.Value))
                return 0;
            return int.Parse(group.Value);
        }
    }
}
