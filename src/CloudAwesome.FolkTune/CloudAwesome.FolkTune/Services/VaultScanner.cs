using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CloudAwesome.FolkTune.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CloudAwesome.FolkTune.Services
{
    public class VaultScanner
    {
        private readonly IDeserializer _deserializer;

        public VaultScanner()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public List<TuneNote> ScanTunes(string vaultPath)
        {
            return Scan(vaultPath, VaultScanTarget.Tunes);
        }
        
        public List<TuneNote> ScanSets(string vaultPath)
        {
            return Scan(vaultPath, VaultScanTarget.Sets);
        }
        
        internal List<TuneNote> Scan(string vaultPath, VaultScanTarget target)
        {
            var searchPath = VaultStructure.GetScanDirectory(vaultPath, target);

            if (!Directory.Exists(searchPath))
            {
                throw new DirectoryNotFoundException($"Required scan path not found: {searchPath}");
            }

            var files = Directory.GetFiles(searchPath, "*.md", SearchOption.AllDirectories);
            var tunes = new List<TuneNote>();

            foreach (var file in files)
            {
                if (IsInHiddenDirectory(file))
                {
                    continue;
                }

                var tune = ParseFile(file);
                if (tune != null)
                {
                    tunes.Add(tune);
                }
            }

            return tunes;
        }

        public TuneNote? ParseFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var yamlContent = ExtractYaml(content);

            if (string.IsNullOrEmpty(yamlContent))
            {
                return null;
            }

            try
            {
                var tune = _deserializer.Deserialize<TuneNote>(yamlContent);
                tune.FilePath = filePath;
                tune.Title = Path.GetFileNameWithoutExtension(filePath);
                return tune;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to parse YAML in {filePath}: {ex.Message}");
                return null;
            }
        }

        private static bool IsInHiddenDirectory(string filePath)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? string.Empty);

            while (directory.Exists)
            {
                if (directory.Name.StartsWith(".", StringComparison.Ordinal))
                {
                    return true;
                }

                directory = directory.Parent;
                if (directory == null)
                {
                    break;
                }
            }

            return false;
        }

        private string ExtractYaml(string content)
        {
            if (!content.StartsWith("---")) return null;

            var endOfYaml = content.IndexOf("---", 3, StringComparison.Ordinal);
            if (endOfYaml == -1) return null;

            return content.Substring(3, endOfYaml - 3).Trim();
        }
    }
}
