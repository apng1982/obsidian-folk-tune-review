using System;
using System.Collections.Generic;
using System.IO;
using CloudAwesome.FolkTune.Services;
using NUnit.Framework;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class IdInitializerTests
    {
        private string _tempVaultPath;
        private IdInitializer _initializer;

        [SetUp]
        public void SetUp()
        {
            _tempVaultPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempVaultPath);
            _initializer = new IdInitializer();
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
        public void Initialize_AddsMissingIds()
        {
            var filePath = Path.Combine(_tempVaultPath, "NoId.md");
            File.WriteAllText(filePath, "---\ntitle: Test\n---\nBody");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "" });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(1));
            Assert.That(result.Success, Is.True);
            
            var content = File.ReadAllText(filePath);
            Assert.That(content, Does.Contain("id: \""));
            Assert.That(content, Does.Contain("title: Test"));
        }

        [Test]
        public void Initialize_DryRun_DoesNotModifyFiles()
        {
            var filePath = Path.Combine(_tempVaultPath, "NoId.md");
            var originalContent = "---\ntitle: Test\n---\nBody";
            File.WriteAllText(filePath, originalContent);

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "", DryRun = true });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(1));
            Assert.That(File.ReadAllText(filePath), Is.EqualTo(originalContent));
        }

        [Test]
        public void Initialize_DetectsDuplicates_AndFails()
        {
            File.WriteAllText(Path.Combine(_tempVaultPath, "File1.md"), "---\nid: \"dup\"\n---");
            File.WriteAllText(Path.Combine(_tempVaultPath, "File2.md"), "---\nid: \"dup\"\n---");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Duplicates.Count, Is.EqualTo(1));
            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(0));
        }

        [Test]
        public void Initialize_WithLimit_RespectsLimit()
        {
            File.WriteAllText(Path.Combine(_tempVaultPath, "1.md"), "---\n---");
            File.WriteAllText(Path.Combine(_tempVaultPath, "2.md"), "---\n---");
            File.WriteAllText(Path.Combine(_tempVaultPath, "3.md"), "---\n---");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "", Limit = 2 });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(2));
        }

        [Test]
        public void Initialize_NoYaml_AddsWarning()
        {
            File.WriteAllText(Path.Combine(_tempVaultPath, "NoYaml.md"), "Just body content");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "" });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(0));
            Assert.That(result.Warnings.Count, Is.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("NoYaml.md"));
        }

        [Test]
        public void AddOrUpdateId_EnsuresIdIsLastProperty()
        {
            var filePath = Path.Combine(_tempVaultPath, "LastProperty.md");
            File.WriteAllText(filePath, "---\ntitle: Test\norigin: [[Some/Where]]\n---\nBody");

            _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "" });

            var content = File.ReadAllText(filePath);
            var endOfYaml = content.IndexOf("---", 3);
            var yaml = content.Substring(3, endOfYaml - 3);
            
            var lines = yaml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.That(lines[^1], Does.StartWith("id: \""));
        }

        [Test]
        public void AddOrUpdateId_MovesExistingIdToLastProperty()
        {
            var filePath = Path.Combine(_tempVaultPath, "MoveExisting.md");
            File.WriteAllText(filePath, "---\nid: \"existing-id\"\ntitle: Test\n---\nBody");

            // Use IncludeExisting = true to trigger update
            _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "", IncludeExisting = true });

            var content = File.ReadAllText(filePath);
            var endOfYaml = content.IndexOf("---", 3);
            var yaml = content.Substring(3, endOfYaml - 3);
            
            var lines = yaml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.That(lines[^1], Does.StartWith("id: \""));
            Assert.That(lines.Length, Is.EqualTo(2)); // title and id
            Assert.That(lines[0], Does.StartWith("title:"));
        }
    }
}
