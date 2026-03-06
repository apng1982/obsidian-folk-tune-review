using System;
using System.IO;
using CloudAwesome.FolkTune.Services;
using NUnit.Framework;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class ReviewStoreManagerTests
    {
        private string _tempPath = null!;

        [SetUp]
        public void SetUp()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempPath))
            {
                Directory.Delete(_tempPath, true);
            }
        }

        [Test]
        public void LoadOrCreate_WhenStoreMissing_CreatesValidStoreAndReturnsCreatedTrue()
        {
            var storePath = Path.Combine(_tempPath, ".tune-review", "reviews.json");
            var manager = new ReviewStoreManager();

            var result = manager.LoadOrCreate(storePath);

            Assert.That(result.Created, Is.True);
            Assert.That(File.Exists(storePath), Is.True);
            Assert.That(result.Store, Is.Not.Null);
            Assert.That(result.Store.SchemaVersion, Is.EqualTo(1));
            Assert.That(result.Store.Tunes, Is.Not.Null);
        }

        [Test]
        public void LoadOrCreate_WhenStoreExists_ReturnsCreatedFalse()
        {
            var storePath = Path.Combine(_tempPath, ".tune-review", "reviews.json");
            Directory.CreateDirectory(Path.GetDirectoryName(storePath)!);
            File.WriteAllText(
                storePath,
                """
                {
                  "schemaVersion": 1,
                  "updatedUtc": "2026-01-01T00:00:00Z",
                  "tunes": {}
                }
                """);

            var manager = new ReviewStoreManager();

            var result = manager.LoadOrCreate(storePath);

            Assert.That(result.Created, Is.False);
            Assert.That(result.Store, Is.Not.Null);
        }
    }
}
