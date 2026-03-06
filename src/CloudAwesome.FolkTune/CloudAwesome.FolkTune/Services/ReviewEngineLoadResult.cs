namespace CloudAwesome.FolkTune.Services
{
    public sealed class ReviewEngineLoadResult
    {
        public string VaultPath { get; init; } = string.Empty;
        public string ScanPath { get; init; } = string.Empty;
        public string StorePath { get; init; } = string.Empty;
        public bool ReviewStoreCreated { get; init; }
    }
}
