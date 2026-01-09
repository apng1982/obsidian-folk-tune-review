using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace CloudAwesome.FolkTune.Services
{
    public class IdInitializer
    {
        public class InitOptions
        {
            public string VaultPath { get; set; } = string.Empty;
            public string SubFolder { get; set; } = string.Empty;
            public bool DryRun { get; set; }
            public int? Limit { get; set; }
            public bool IncludeExisting { get; set; }
        }

        public class InitResult
        {
            public List<string> UpdatedFiles { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public List<string> Duplicates { get; set; } = new();
            public bool Success { get; set; } = true;
        }

        private readonly IDeserializer _deserializer;

        public IdInitializer()
        {
            _deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public InitResult Initialize(InitOptions options)
        {
            var result = new InitResult();
            var searchPath = string.IsNullOrEmpty(options.SubFolder) 
                ? options.VaultPath 
                : Path.Combine(options.VaultPath, options.SubFolder);

            if (!Directory.Exists(searchPath))
            {
                throw new DirectoryNotFoundException($"Path not found: {searchPath}");
            }

            var allFiles = Directory.GetFiles(searchPath, "*.md", SearchOption.AllDirectories)
                .Where(f => !f.Contains(".obsidian"))
                .ToList();

            var idMap = new Dictionary<string, List<string>>();
            var missingIdFiles = new List<string>();

            foreach (var file in allFiles)
            {
                var content = File.ReadAllText(file);
                var yaml = ExtractYaml(content);
                if (yaml == null)
                {
                    result.Warnings.Add($"File has no YAML front matter: {file}");
                    continue;
                }

                var id = GetIdFromYaml(yaml);
                if (!string.IsNullOrEmpty(id))
                {
                    if (!idMap.ContainsKey(id)) idMap[id] = new List<string>();
                    idMap[id].Add(file);
                }
                else
                {
                    missingIdFiles.Add(file);
                }
            }

            // Detect duplicates
            foreach (var kvp in idMap)
            {
                if (kvp.Value.Count > 1)
                {
                    result.Duplicates.Add($"Duplicate ID '{kvp.Key}' found in: {string.Join(", ", kvp.Value)}");
                    result.Success = false;
                }
            }

            if (!result.Success) return result;

            // Proceed with initialization
            var filesToUpdate = options.IncludeExisting 
                ? allFiles.Where(f => !result.Warnings.Any(w => w.Contains(f))).ToList()
                : missingIdFiles;

            int count = 0;
            foreach (var file in filesToUpdate)
            {
                if (options.Limit.HasValue && count >= options.Limit.Value) break;

                if (!options.DryRun)
                {
                    AddOrUpdateId(file);
                }
                result.UpdatedFiles.Add(file);
                count++;
            }

            return result;
        }

        private string? ExtractYaml(string content)
        {
            if (!content.StartsWith("---")) return null;
            var endOfYaml = content.IndexOf("---", 3);
            if (endOfYaml == -1) return null;
            return content.Substring(3, endOfYaml - 3).Trim();
        }

        private string? GetIdFromYaml(string yaml)
        {
            try
            {
                var dict = _deserializer.Deserialize<Dictionary<string, object>>(yaml);
                if (dict != null && dict.TryGetValue("id", out var id))
                {
                    return id?.ToString();
                }
            }
            catch
            {
                // Ignore malformed YAML here, it will be reported as warning if it couldn't be parsed at all
            }
            return null;
        }

        private void AddOrUpdateId(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var endOfYaml = content.IndexOf("---", 3);
            if (endOfYaml == -1) return;

            var yaml = content.Substring(3, endOfYaml - 3);
            var body = content.Substring(endOfYaml);
            
            var newId = Guid.NewGuid().ToString();
            var idPattern = @"(?m)^id\s*:.*$";
            string newYaml;

            if (Regex.IsMatch(yaml, idPattern))
            {
                newYaml = Regex.Replace(yaml, idPattern, $"id: \"{newId}\"");
            }
            else
            {
                newYaml = "\nid: \"" + newId + "\"" + (yaml.StartsWith("\n") ? "" : "\n") + yaml;
            }

            File.WriteAllText(filePath, "---" + newYaml + body);
        }
    }
}
