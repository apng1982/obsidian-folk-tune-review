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

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath });

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

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, DryRun = true });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(1));
            Assert.That(File.ReadAllText(filePath), Is.EqualTo(originalContent));
        }

        [Test]
        public void Initialize_DetectsDuplicates_AndFails()
        {
            File.WriteAllText(Path.Combine(_tempVaultPath, "File1.md"), "---\nid: \"dup\"\n---");
            File.WriteAllText(Path.Combine(_tempVaultPath, "File2.md"), "---\nid: \"dup\"\n---");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath });

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

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, Limit = 2 });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(2));
        }

        [Test]
        public void Initialize_NoYaml_AddsWarning()
        {
            File.WriteAllText(Path.Combine(_tempVaultPath, "NoYaml.md"), "Just body content");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(0));
            Assert.That(result.Warnings.Count, Is.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("NoYaml.md"));
        }
    }
}
