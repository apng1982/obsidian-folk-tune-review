namespace CloudAwesome.FolkTune.Services
{
    public class VaultValidator
    {
        public void ValidateReviewVault(string vaultPath, VaultScanTarget target)
        {
            if (string.IsNullOrWhiteSpace(vaultPath))
            {
                throw new DirectoryNotFoundException("A vault path is required.");
            }

            if (!Directory.Exists(vaultPath))
            {
                throw new DirectoryNotFoundException($"Vault root not found: {vaultPath}");
            }

            var scanPath = VaultStructure.GetScanDirectory(vaultPath, target);
            if (!Directory.Exists(scanPath))
            {
                throw new DirectoryNotFoundException(
                    $"This does not appear to be a valid tune-review vault. Required directory not found: {scanPath}");
            }
        }
    }
}
