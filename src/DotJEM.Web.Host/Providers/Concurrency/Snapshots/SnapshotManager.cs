using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Storage;
using DotJEM.Web.Host.Configuration.Elements;
using DotJEM.Web.Host.Providers.Scheduler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Web.Host.Providers.Concurrency.Snapshots
{
    public interface IIndexSnapshotManager
    {
        void ReplaceStrategy(ISnapshotStrategy strategy);

        void Initialize();
        void TakeSnapshot();
        void RestoreSnapshot();
    }

    public class IndexSnapshotManager : IIndexSnapshotManager
    {
        private readonly IStorageIndex index;
        private readonly IStorageContext storage;

        private readonly string indexPath;
        private readonly string snapshotsPath;
        private readonly int maxSnapshots;
        private ISnapshotStrategy strategy;

        private bool initialized;

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
            this.snapshotsPath = path.MapPath(configuration.Index.Snapshots.Path);
            this.indexPath = path.MapPath(configuration.Index.Storage.Path);
            this.strategy = maxSnapshots > 0 ? new DefaultSnapshotStrategy() : null;
            
            if (!string.IsNullOrEmpty(configuration.Index.Snapshots.CronTime))
                scheduler.ScheduleCron("Snapshot-Schedule", _ => TakeSnapshot(), configuration.Index.Snapshots.CronTime);
        }

        public void Initialize()
        {
            initialized = true;
            TakeSnapshot();
        }

        public void ReplaceStrategy(ISnapshotStrategy strategy)
        {
            this.strategy = strategy;
        }

        public void TakeSnapshot()
        {
            if(!initialized || maxSnapshots <= 0 || strategy == null) return;
            
            index.Flush();
            index.Close();

            JObject json = storage.AreaInfos
                .Aggregate(new JObject(), (x, info) =>
                {
                    x[info.Name] = storage.Area(info.Name).Log.CurrentGeneration;
                    return x;
                });
            strategy.TakeSnapshot(snapshotsPath, indexPath, maxSnapshots, json);
        }

        public void RestoreSnapshot()
        {
            if(!initialized || maxSnapshots <= 0 || strategy == null) return;

            JObject metadata = strategy.RestoreSnapshot(snapshotsPath, indexPath);
            if(metadata == null) return;
            
            foreach (JProperty property in metadata.Properties())
            {
                storage.Area(property.Name).Log.Get(property.Value.ToObject<long>(), count: 0);
            }
        }
    }
    
    public interface ISnapshotStrategy
    {
        void TakeSnapshot(string snapshotsDirectory, string sourceDirectory, int maxSnapshots, JObject metadata);
        JObject RestoreSnapshot(string snapshotsDirectory, string indexDirectory);
    }

    public class DefaultSnapshotStrategy : ISnapshotStrategy
    {
        public void TakeSnapshot(string snapshotsDirectory, string sourceDirectory, int maxSnapshots, JObject metadata)
        {
            Directory.CreateDirectory(snapshotsDirectory);

            string targetPath = Path.Combine(snapshotsDirectory, $"{DateTime.Now:yyyy-MM-ddTHHmmss}.zip");

            //IF the directory exists, ignore this cycle.
            if(File.Exists(targetPath))
                return;

            foreach (string oldSnapshot in Directory
                .GetFiles(snapshotsDirectory, "*.zip")
                .OrderByDescending(dir => dir)
                .Skip(maxSnapshots - 1))
            {
                try
                {
                    File.Delete(oldSnapshot);
                }
                catch (Exception exception)
                {
                    //IGNORE for now.
                }
            }

            ZipFile.CreateFromDirectory(sourceDirectory, targetPath, CompressionLevel.Optimal, false);
            using ZipArchive archive = ZipFile.Open(targetPath, ZipArchiveMode.Update);
            ZipArchiveEntry entry = archive.CreateEntry("storage-generation.log");
            using JsonTextWriter writer = new JsonTextWriter(new StreamWriter(entry.Open()));
            metadata.WriteTo(writer);
        }

        public JObject RestoreSnapshot(string snapshotsDirectory, string indexDirectory)
        {
            foreach (string file in Directory.GetFiles(indexDirectory))
                File.Delete(file);

            foreach (string snapshot in Directory
                .GetFiles(snapshotsDirectory, "*.zip")
                .OrderByDescending(dir => dir))
            {
                try
                {
                    ZipFile.ExtractToDirectory(snapshot, indexDirectory);

                    JObject metadata;
                    using (JsonTextReader reader = new (new StreamReader(Path.Combine(indexDirectory, "storage-generation.log"))))
                        metadata = JObject.Load(reader);
                    File.Delete(Path.Combine(indexDirectory, "storage-generation.log"));
                    return metadata;
                }
                catch (Exception exception)
                {
                    //IGNORE for now.
                }
            }

            return null;
        }
    }
}
