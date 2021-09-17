using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Web.Host.Writers;
using NUnit.Framework;

namespace DotJEM.Web.Host.Test.Writers
{
    public class RollingStreamWriterTest
    {
        [Test]
        public void Pain()
        {
            using (TemporaryTestDirectory directory = TemporaryTestDirectory.Generate())
            {
                using (RollingStreamWriter writer = new RollingStreamWriter(directory.GenerateFile("lucene-writer.log"), 1024 * 5, 5, true)) 
                {
                    for (int i = 0; i < 1000000; i++)
                    {
                        writer.WriteLine("Bring on the pain!");
                        writer.WriteLine("We are writing all day long!!!");
                    }
                }
            }
        }

    }

    public class TemporaryTestDirectory : IDisposable
    {
        private readonly DirectoryInfo directory;

        public string FullName => directory.FullName;

        private TemporaryTestDirectory(DirectoryInfo directory)
        {
            this.directory = directory;
        }

        public string GenerateFile(string name)
        {
            return Path.Combine(directory.FullName, name);
        }

        public void Dispose()
        {
            directory.Refresh();
            if (directory.Exists)
            {
                directory.Delete(true);
            }
        }

        public static TemporaryTestDirectory Generate()
        {
            return new TemporaryTestDirectory(Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        }
    }
}
