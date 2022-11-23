using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Remoting.MetadataServices;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Storage.Snapshot;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Providers.Scheduler;
using Lucene.Net.Index;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots
{
    public interface IIndexSnapshotManager
    {
        void ReplaceStrategy(ISnapshotStrategy strategy);
        void TakeSnapshot();
        void RestoreSnapshot();
    }

    public class IndexSnapshotManager : IIndexSnapshotManager
    {
        private readonly IStorageIndex index;
        private readonly IStorageContext storage;

        private readonly int maxSnapshots;
        private ISnapshotStrategy strategy;

        public IndexSnapshotManager(
            IStorageIndex index,
            IStorageContext storage,
            IWebHostConfiguration configuration, 
            IPathResolver path,
            IWebScheduler scheduler)
        {
            this.index = index;
            this.storage = storage;
            this.maxSnapshots = configuration.Index.Snapshots.MaxSnapshots;
            this.strategy = maxSnapshots > 0 ? new ZipSnapshotStrategy(path.MapPath(configuration.Index.Snapshots.Path)) : null;
            
            if (!string.IsNullOrEmpty(configuration.Index.Snapshots.CronTime))
                scheduler.ScheduleCron("Snapshot-Schedule", _ => TakeSnapshot(), configuration.Index.Snapshots.CronTime);
        }

        public void ReplaceStrategy(ISnapshotStrategy strategy)
        {
            this.strategy = strategy;
        }

        public void TakeSnapshot()
        {
            if(maxSnapshots <= 0 || strategy == null) return;
            
            JObject generations = storage.AreaInfos
                .Aggregate(new JObject(), (x, info) =>
                {
                    x[info.Name] = storage.Area(info.Name).Log.CurrentGeneration;
                    return x;
                });

            ISnapshotTarget target = strategy.CreateTarget(new JObject { ["storageGenerations"] = generations });
            index.Storage.Snapshot(target);

            strategy.CleanOldSnapshots(maxSnapshots);
        }

        public void RestoreSnapshot()
        {
            if(maxSnapshots <= 0 || strategy == null) return;

            int offset = 0;
            while (true)
            {

                try
                {
                    ISnapshotSourceWithMetadata source = strategy.CreateSource(offset++);
                    if (source == null)
                        return;

                    index.Storage.Restore(source);
                    if (source.Metadata["storageGenerations"] is not JObject metadata) continue;
                    
                    foreach (JProperty property in metadata.Properties())
                    {
                        storage.Area(property.Name).Log.Get(property.Value.ToObject<long>(), count: 0);
                    }
                }
                catch
                {
                    //IGNORE for now.
                }
            }


        }
    }
    


    public interface ISnapshotStrategy 
    {
        ISnapshotTarget CreateTarget(JObject metaData);
        ISnapshotSourceWithMetadata CreateSource(int offset);
        void CleanOldSnapshots(int maxSnapshots);
    }

    public interface ISnapshotSourceWithMetadata : ISnapshotSource
    {
        JObject Metadata { get; }
    }

    public class ZipSnapshotTarget : ISnapshotTarget
    {
        private readonly string path;
        private readonly JObject metaData;

        public ZipSnapshotTarget(string path, JObject metaData)
        {
            this.path = path;
            this.metaData = metaData;
        }

        public ISnapshotWriter Open(IndexCommit commit)
        {
            return new ZipSnapshotWriter(metaData, Path.Combine(path, $"{DateTime.Now:yyyy-MM-ddTHHmmss}.{commit.Generation:D8}.zip"))
                .WriteMetaData(commit);
        }
    }

    public class ZipSnapshotStrategy : ISnapshotStrategy
    {
        private readonly string path;

        public ZipSnapshotStrategy(string path)
        {
            this.path = path;
        }

        public ISnapshotTarget CreateTarget(JObject metaData)
        {
            return new ZipSnapshotTarget(path, metaData);
        }

        public ISnapshotSourceWithMetadata CreateSource(int offset)
        {
            string[] files = Directory.GetFiles(path, "*.zip")
                .OrderByDescending(file => file)
                .ToArray();
            return files.Length > offset ? new ZipSnapshotSource(files[offset]) : null;
        }

        public void CleanOldSnapshots(int maxSnapshots)
        {
            foreach (string file in Directory.GetFiles(path, "*.zip")
                         .OrderByDescending(file => file)
                         .Skip(maxSnapshots))
            {
                try
                {
                    File.Delete(file);
                }
                catch 
                {
                    //Ignore, try again next time.
                }
            }
        }

    }

    public class ZipSnapshotWriter : ISnapshotWriter
    {
        private readonly ZipArchive archive;
        private readonly JObject metadata;

        public ZipSnapshotWriter(JObject metadata, string file)
        {
            if (metadata["files"] is not JArray)
                metadata["files"] = new JArray();
            this.metadata = metadata;

            archive = ZipFile.Open(file, ZipArchiveMode.Create);
        }

        public void WriteFile(IndexInputStream stream)
        {
            using Stream target = archive.CreateEntry(stream.FileName).Open();
            stream.CopyTo(target);

            JArray filesArr = (JArray)metadata["files"];
            filesArr.Add(JToken.FromObject(stream.FileName));
        }

        public void WriteSegmentsFile(IndexInputStream stream)
        {
            using Stream target = archive.CreateEntry(stream.FileName).Open();
            stream.CopyTo(target);
            metadata["segmentsFile"] = stream.FileName;
        }

        public ZipSnapshotWriter WriteMetaData(IndexCommit commit)
        {
            metadata["generation"] = commit.Generation;
            metadata["version"] = commit.Version;
            return this;
        }

        public void Dispose()
        {
            using (Stream metaStream = archive.CreateEntry("metadata.json").Open())
            {
                using JsonWriter writer = new JsonTextWriter(new StreamWriter(metaStream));
                metadata.WriteTo(writer);
            }
            archive?.Dispose();
        }
    }

    public class ZipSnapshotSource : ISnapshotSourceWithMetadata
    {
        private readonly ZipArchive archive;

        public JObject Metadata { get; }

        public ZipSnapshotSource(string file)
        {
            archive = ZipFile.Open(file, ZipArchiveMode.Read);
            using Stream metaStream = archive.GetEntry("metadata.json")?.Open();
            using JsonReader reader = new JsonTextReader(new StreamReader(metaStream));
            Metadata = JObject.Load(reader);
        }

        public ISnapshot Open()
        {
            return new ZipSnapshot(archive, Metadata);
        }
    }

    public class ZipSnapshot : ISnapshot
    {
        private readonly ZipArchive archive;
        private readonly JObject metadata;

        public long Generation { get; }
        public ILuceneFile SegmentsFile { get; }
        public IEnumerable<ILuceneFile> Files { get; }

        public ZipSnapshot(ZipArchive archive, JObject metadata)
        {
            this.archive = archive;
            this.metadata = metadata;

            Files = new List<ILuceneFile>();
            if (metadata["files"] is JArray arr)
                Files = arr.Select(fileName => new ZipLuceneFile((string)fileName, archive));
            SegmentsFile = new ZipLuceneFile((string)metadata["segmentsFile"], archive);
            Generation = (long)metadata["generation"];
        }
        
        public void Dispose()
        {
            archive.Dispose();
        }

        private class ZipLuceneFile : ILuceneFile
        {
            private readonly ZipArchive archive;
            public string Name { get; }

            public ZipLuceneFile(string fileName, ZipArchive archive)
            {
                this.Name = fileName;
                this.archive = archive;
            }

            public Stream Open()
            {
                return archive.GetEntry(Name)?.Open();
            }

        }
    }

}
