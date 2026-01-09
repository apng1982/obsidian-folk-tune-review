using System;
using System.Collections.Generic;
using System.IO;
using CloudAwesome.FolkTune.Models;
using CloudAwesome.FolkTune.Services;
using NUnit.Framework;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class VaultScannerTests
    {
        private string _tempVaultPath;

        [SetUp]
        public void SetUp()
        {
            _tempVaultPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempVaultPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempVaultPath))
            {
                Directory.Delete(_tempVaultPath, true);
            }
        }

        [Test]
        public void ParseFile_ValidYaml_ReturnsTuneNote()
        {
            var filePath = Path.Combine(_tempVaultPath, "TestTune.md");
            var content = @"---
id: ""12345""
learn: false
origin: ""[[Ref/Geo/Scottish|Scottish]]""
---
Some body content";
            File.WriteAllText(filePath, content);

            var scanner = new VaultScanner();
            var tune = scanner.ParseFile(filePath);

            Assert.That(tune, Is.Not.Null);
            Assert.That(tune.Id, Is.EqualTo("12345"));
            Assert.That(tune.Learn, Is.False);
            Assert.That(tune.Title, Is.EqualTo("TestTune"));
            Assert.That(tune.FilePath, Is.EqualTo(filePath));
        }

        [Test]
        public void Scan_FindsAllMarkdownFiles()
        {
            var tunesPath = Path.Combine(_tempVaultPath, "Tunes", "Tunes");
            Directory.CreateDirectory(tunesPath);
            
            File.WriteAllText(Path.Combine(tunesPath, "Tune1.md"), "---\nid: \"1\"\n---");
            File.WriteAllText(Path.Combine(tunesPath, "Tune2.md"), "---\nid: \"2\"\n---");
            
            var obsidianPath = Path.Combine(_tempVaultPath, ".obsidian");
            Directory.CreateDirectory(obsidianPath);
            File.WriteAllText(Path.Combine(obsidianPath, "config.md"), "---\nid: \"hidden\"\n---");

            var scanner = new VaultScanner();
            var results = scanner.Scan(_tempVaultPath);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.Exists(t => t.Title == "Tune1"), Is.True);
            Assert.That(results.Exists(t => t.Title == "Tune2"), Is.True);
            Assert.That(results.Exists(t => t.Title == "config"), Is.False);
        }
    }
}
