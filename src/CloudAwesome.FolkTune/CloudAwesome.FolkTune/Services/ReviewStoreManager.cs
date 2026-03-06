using CloudAwesome.FolkTune.Models;
using Newtonsoft.Json;

namespace CloudAwesome.FolkTune.Services
{
    public class ReviewStoreManager
    {
        public ReviewStore Load(string storePath)
        {
            if (!File.Exists(storePath))
            {
                return new ReviewStore
                {
                    UpdatedUtc = DateTime.UtcNow,
                    SchemaVersion = 1
                };
            }

            var json = File.ReadAllText(storePath);
            return JsonConvert.DeserializeObject<ReviewStore>(json);
        }
        
        public (ReviewStore Store, bool Created) LoadOrCreate(string storePath)
        {
            if (File.Exists(storePath))
            {
                return (Load(storePath), false);
            }

            var directory = Path.GetDirectoryName(storePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var store = new ReviewStore
            {
                SchemaVersion = 1,
                UpdatedUtc = DateTime.UtcNow
            };

            Save(storePath, store);

            return (store, true);
        }

        public void Save(string storePath, ReviewStore store)
        {
            store.UpdatedUtc = DateTime.UtcNow;
            var json = JsonConvert.SerializeObject(store, Formatting.Indented);

            var directory = Path.GetDirectoryName(storePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Atomic write
            var tempFile = storePath + ".tmp";
            var backupFile = storePath + ".bak";

            File.WriteAllText(tempFile, json);

            if (File.Exists(storePath))
            {
                if (File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                }
                File.Copy(storePath, backupFile);
                File.Delete(storePath);
            }

            File.Move(tempFile, storePath);
        }
    }
}
