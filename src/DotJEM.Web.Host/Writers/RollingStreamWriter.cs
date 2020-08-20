using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Writers
{
     public class RollingStreamWriter : StreamWriter
    {
        private const int DEFAULT_BUFFER_SIZE = 1024 * 4;

        private readonly string path;
        private readonly long maxSize;
        private readonly int maxFiles;
        private readonly bool zip;

        private readonly object padLock = new { };
        private volatile StreamWriter innerWriter;
        
        public RollingStreamWriter(string path, long maxSize, int maxFiles, bool zip)
            : base(Stream.Null)
        {
            this.path = path;
            this.maxSize = maxSize;
            this.maxFiles = maxFiles;
            this.zip = zip;

            this.innerWriter = CreateWriter();
        }

        public override void Write(char[] buffer, int index, int count)
        {
            lock (padLock)
            {
                innerWriter.Write(buffer, index, count);
            }
        }

        public override void Flush()
        {
            lock (padLock)
            {
                innerWriter.Flush();
                CheckFileSizeLimitReached();
            }
        }

        public override void WriteLine()
        {
            lock (padLock)
            {
                innerWriter.WriteLine();
                CheckFileSizeLimitReached();
            }
        }

        public override void WriteLine(string value)
        {
            lock (padLock)
            {
                innerWriter.WriteLine(value);
                CheckFileSizeLimitReached();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (padLock)
                {
                    innerWriter.Dispose();
                    innerWriter = null;
                    pendingAsyncTask?.Wait();
                }
            }
            base.Dispose(disposing);
        }

        private void CheckFileSizeLimitReached()
        {
            lock (padLock)
            {
                if (innerWriter.BaseStream.Length < this.maxSize) return;

                try
                {
                    innerWriter.Dispose();
                    innerWriter = null;
                    CheckFileLimitReached();
                }
                finally
                {
                    innerWriter = CreateWriter();
                }
            }
        }

        private StreamWriter CreateWriter()
        {
            return new StreamWriter(File.Open(GenerateDateBoundPath(path), FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8, DEFAULT_BUFFER_SIZE);
        }

        private static string UniqueShortHash
        {
            get
            {
                using (SHA1Managed sha = new SHA1Managed())
                {
                    byte[] bytes = sha.ComputeHash(Guid.NewGuid().ToByteArray());
                    string[] values = bytes.Take(4).Select(b => b.ToString("X2")).ToArray();
                    string hash = string.Concat(values);
                    return hash.ToLower();
                }
            }
        }

        private volatile Task pendingAsyncTask;

        private void CheckFileLimitReached()
        {
            Task pendingTask = pendingAsyncTask;
            if (pendingTask == null || pendingTask.IsCompleted)
            {
                string[] files = Directory.GetFiles(Path.GetDirectoryName(path), GenerateWildcardPath(path));
                if (files.Length > maxFiles)
                {
                    pendingAsyncTask = CompactAndClean(files);
                }
            }
        }

        private Task CompactAndClean(string[] files)
        {
            return Task.Run(() =>
            {
                if (zip)
                {
                    using (ZipArchive archive = ZipFile.Open(GenerateDateBoundZipPath(path), ZipArchiveMode.Create))
                    {
                        foreach (string file in files)
                        {
                            try
                            {
                                archive.CreateEntryFromFile(file, Path.GetFileName(file));
                                File.Delete(file);
                            }
                            catch { /* ignore.*/ }
                        }
                    }

                    string[] zipFiles = Directory.GetFiles(Path.GetDirectoryName(path), GenerateZipWildcardPath(path));
                    DeleteFiles(zipFiles);
                }
                else
                {
                    DeleteFiles(files);
                }
            });
        }

        private void DeleteFiles(string[] files)
        {
            foreach (string file in files.OrderByDescending(f => f).Skip(maxFiles))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    /* ignore.*/
                }
            }
        }

        private static string GenerateWildcardPath(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return $"{fileName}-*-*{ext}";
        }

        private static string GenerateZipWildcardPath(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            return $"{fileName}-*-*.zip";
        }

        private static string GenerateDateBoundPath(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            return Path.Combine(dir, $"{fileName}-{DateTime.Now:yyyy-MM-ddTHH-mm-ss}-{UniqueShortHash}{ext}");
        }

        private static string GenerateDateBoundZipPath(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            return Path.Combine(dir, $"{fileName}-{DateTime.Now:yyyy-MM-ddTHH-mm-ss}-{UniqueShortHash}.zip");
        }
    }

}
