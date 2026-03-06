namespace CloudAwesome.FolkTune.Services
{
    public static class VaultStructure
    {
        public const string ReviewDirectoryName = ".tune-review";
        public const string ReviewStoreFileName = "reviews.json";

        public static string GetTunesDirectory(string vaultPath) =>
            Path.Combine(vaultPath, "Tunes", "Tunes");

        public static string GetSetsDirectory(string vaultPath) =>
            Path.Combine(vaultPath, "Tunes", "Sets");

        public static string GetReviewDirectory(string vaultPath) =>
            Path.Combine(vaultPath, ReviewDirectoryName);

        public static string GetDefaultReviewStorePath(string vaultPath) =>
            Path.Combine(GetReviewDirectory(vaultPath), ReviewStoreFileName);

        public static string GetScanDirectory(string vaultPath, VaultScanTarget target) =>
            target switch
            {
                VaultScanTarget.Tunes => GetTunesDirectory(vaultPath),
                VaultScanTarget.Sets => GetSetsDirectory(vaultPath),
                _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported scan target.")
            };
    }
}
