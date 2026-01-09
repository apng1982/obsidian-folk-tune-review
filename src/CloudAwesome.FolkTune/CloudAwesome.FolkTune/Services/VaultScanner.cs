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

        public List<TuneNote> Scan(string vaultPath, string subFolder = null)
        {
            var searchPath = string.IsNullOrEmpty(subFolder) 
                ? vaultPath 
                : Path.Combine(vaultPath, subFolder);

            if (!Directory.Exists(searchPath))
            {
                throw new DirectoryNotFoundException($"Vault path not found: {searchPath}");
            }

            var files = Directory.GetFiles(searchPath, "*.md", SearchOption.AllDirectories);
            var tunes = new List<TuneNote>();

            foreach (var file in files)
            {
                if (file.Contains(".obsidian")) continue;

                var tune = ParseFile(file);
                if (tune != null)
                {
                    tunes.Add(tune);
                }
            }

            return tunes;
        }

        public TuneNote ParseFile(string filePath)
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
                // TODO: Log warning about malformed YAML
                Console.WriteLine($"[WARNING] Failed to parse YAML in {filePath}: {ex.Message}");
                return null;
            }
        }

        private string ExtractYaml(string content)
        {
            if (!content.StartsWith("---")) return null;

            var endOfYaml = content.IndexOf("---", 3);
            if (endOfYaml == -1) return null;

            return content.Substring(3, endOfYaml - 3).Trim();
        }
    }
}
