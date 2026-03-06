using CloudAwesome.FolkTune.Services;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class VaultValidatorTests
    {
        private string _tempVaultPath = null!;

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
        public void ValidateReviewVault_WhenTunesDirectoryExists_DoesNotThrow()
        {
            Directory.CreateDirectory(Path.Combine(_tempVaultPath, "Tunes", "Tunes"));

            var validator = new VaultValidator();

            Assert.DoesNotThrow(() => validator.ValidateReviewVault(_tempVaultPath, VaultScanTarget.Tunes));
        }

        [Test]
        public void ValidateReviewVault_WhenTunesDirectoryMissing_Throws()
        {
            var validator = new VaultValidator();

            var ex = Assert.Throws<DirectoryNotFoundException>(
                () => validator.ValidateReviewVault(_tempVaultPath, VaultScanTarget.Tunes));

            Assert.That(ex!.Message, Does.Contain(Path.Combine("Tunes", "Tunes")));
        }
    }
}
