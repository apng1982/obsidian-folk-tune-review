using System.IO.Abstractions.TestingHelpers;
using CloudAwesome.FolkTune.Services;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class IdInitializerTests
    {
        private MockFileSystem _fileSystem = null!;
        private string _tempVaultPath;
        private IdInitializer _initializer;
        
        [SetUp]
        public void SetUp()
        {
            _fileSystem = new MockFileSystem();
            
            _tempVaultPath = _fileSystem.Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _fileSystem.Directory.CreateDirectory(_tempVaultPath);
            _initializer = new IdInitializer(_fileSystem);
        }

        [TearDown]
        public void TearDown()
        {
            if (_fileSystem.Directory.Exists(_tempVaultPath))
            {
                _fileSystem.Directory.Delete(_tempVaultPath, true);
            }
        }

        [Test]
        public void Initialize_AddsMissingIds()
        {
            var filePath = _fileSystem.Path.Combine(_tempVaultPath, "NoId.md");
            _fileSystem.File.WriteAllText(filePath, "---\ntitle: Test\n---\nBody");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "" });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(1));
            Assert.That(result.Success, Is.True);
            
            var content = _fileSystem.File.ReadAllText(filePath);
            Assert.That(content, Does.Contain("id: \""));
            Assert.That(content, Does.Contain("title: Test"));
        }

        [Test]
        public void Initialize_DryRun_DoesNotModifyFiles()
        {
            var filePath = _fileSystem.Path.Combine(_tempVaultPath, "NoId.md");
            var originalContent = "---\ntitle: Test\n---\nBody";
            _fileSystem.File.WriteAllText(filePath, originalContent);

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "", DryRun = true });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(1));
            Assert.That(_fileSystem.File.ReadAllText(filePath), Is.EqualTo(originalContent));
        }

        [Test]
        public void Initialize_DetectsDuplicates_AndFails()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_tempVaultPath, "File1.md"), "---\nid: \"dup\"\n---");
            _fileSystem.File.WriteAllText(Path.Combine(_tempVaultPath, "File2.md"), "---\nid: \"dup\"\n---");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "" });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Duplicates.Count, Is.EqualTo(1));
            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(0));
        }

        [Test]
        public void Initialize_WithLimit_RespectsLimit()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_tempVaultPath, "1.md"), "---\n---");
            _fileSystem.File.WriteAllText(Path.Combine(_tempVaultPath, "2.md"), "---\n---");
            _fileSystem.File.WriteAllText(Path.Combine(_tempVaultPath, "3.md"), "---\n---");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "", Limit = 2 });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(2));
        }

        [Test]
        public void Initialize_NoYaml_AddsWarning()
        {
            _fileSystem.File.WriteAllText(Path.Combine(_tempVaultPath, "NoYaml.md"), "Just body content");

            var result = _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "" });

            Assert.That(result.UpdatedFiles.Count, Is.EqualTo(0));
            Assert.That(result.Warnings.Count, Is.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("NoYaml.md"));
        }

        [Test]
        public void AddOrUpdateId_EnsuresIdIsLastProperty()
        {
            var filePath = _fileSystem.Path.Combine(_tempVaultPath, "LastProperty.md");
            _fileSystem.File.WriteAllText(filePath, "---\ntitle: Test\norigin: [[Some/Where]]\n---\nBody");

            _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "" });

            var content = _fileSystem.File.ReadAllText(filePath);
            var endOfYaml = content.IndexOf("---", 3);
            var yaml = content.Substring(3, endOfYaml - 3);
            
            var lines = yaml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.That(lines[^1], Does.StartWith("id: \""));
        }

        [Test]
        public void AddOrUpdateId_MovesExistingIdToLastProperty()
        {
            var filePath = _fileSystem.Path.Combine(_tempVaultPath, "MoveExisting.md");
            _fileSystem.File.WriteAllText(filePath, "---\nid: \"existing-id\"\ntitle: Test\n---\nBody");

            // Use IncludeExisting = true to trigger update
            _initializer.Initialize(new IdInitializer.InitOptions { VaultPath = _tempVaultPath, SubFolder = "", IncludeExisting = true });

            var content = _fileSystem.File.ReadAllText(filePath);
            var endOfYaml = content.IndexOf("---", 3);
            var yaml = content.Substring(3, endOfYaml - 3);
            
            var lines = yaml.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.That(lines[^1], Does.StartWith("id: \""));
            Assert.That(lines.Length, Is.EqualTo(2)); // title and id
            Assert.That(lines[0], Does.StartWith("title:"));
        }
    }
}
