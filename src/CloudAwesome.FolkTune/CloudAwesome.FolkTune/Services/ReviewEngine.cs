using System;
using System.Collections.Generic;
using System.Linq;
using CloudAwesome.FolkTune.Models;
using CloudAwesome.FolkTune.Helpers;

namespace CloudAwesome.FolkTune.Services
{
    public class ReviewEngine
    {
        private readonly VaultScanner _scanner;
        private readonly ReviewStoreManager _storeManager;
        private readonly SelectionService _selectionService;
        private readonly IdInitializer _idInitializer;
        private readonly VaultValidator _vaultValidator;
        
        private List<TuneNote> _allTunes;
        private ReviewStore _store;
        private string _storePath;

        public ReviewEngine(
            VaultScanner vaultScanner,
            ReviewStoreManager reviewStoreManager,
            SelectionService selectionService,
            IdInitializer idInitializer)
            : this(
                vaultScanner, 
                reviewStoreManager, 
                selectionService, 
                idInitializer, 
                new VaultValidator()) { }
        
        public ReviewEngine(
            VaultScanner scanner, 
            ReviewStoreManager storeManager, 
            SelectionService selectionService, 
            IdInitializer idInitializer,
            VaultValidator vaultValidator)
        {
            _scanner = scanner;
            _storeManager = storeManager;
            _selectionService = selectionService;
            _idInitializer = idInitializer;
            _vaultValidator = vaultValidator;
        }

        public ReviewEngineLoadResult Load(string vaultPath, string? storePath = null)
        {
            if (string.IsNullOrWhiteSpace(vaultPath))
            {
                throw new DirectoryNotFoundException("A vault path is required.");
            }

            var resolvedVaultPath = Path.GetFullPath(vaultPath);
            _vaultValidator.ValidateReviewVault(resolvedVaultPath, VaultScanTarget.Tunes);

            var resolvedStorePath = string.IsNullOrWhiteSpace(storePath)
                ? VaultStructure.GetDefaultReviewStorePath(resolvedVaultPath)
                : Path.GetFullPath(storePath);

            _allTunes = _scanner.ScanTunes(resolvedVaultPath);

            var (store, created) = _storeManager.LoadOrCreate(resolvedStorePath);
            _store = store;
            _storePath = resolvedStorePath;

            return new ReviewEngineLoadResult
            {
                VaultPath = resolvedVaultPath,
                ScanPath = VaultStructure.GetScanDirectory(resolvedVaultPath, VaultScanTarget.Tunes),
                StorePath = resolvedStorePath,
                ReviewStoreCreated = created
            };
        }

        public IdInitializer.InitResult InitializeIds(IdInitializer.InitOptions options)
        {
            return _idInitializer.Initialize(options);
        }

        public List<TuneNote> GetReviewCandidates(SelectionService.SelectionOptions options)
        {
            if (_allTunes == null || _store == null)
            {
                throw new InvalidOperationException("Engine not loaded. Call Load() first.");
            }
            return _selectionService.SelectTunes(_allTunes, _store, options, WikiLinkHelper.ExtractDisplayText);
        }

        public void SubmitReview(string id, int score, string notes = null)
        {
            EnsureTuneRecord(id);
            var record = _store.Tunes[id];

            record.Last = DateTime.Today.ToString("yyyy-MM-dd");
            record.Score = score;
            if (!string.IsNullOrEmpty(notes))
            {
                record.Notes = notes;
            }
            record.IntervalDays = MapScoreToInterval(score);
        }

        public void MarkAsPlayed(string id, DateTime date)
        {
            EnsureTuneRecord(id);
            var record = _store.Tunes[id];
            record.SessionLast = date.ToString("yyyy-MM-dd");
        }

        public void ExcludeTune(string id)
        {
            EnsureTuneRecord(id);
            _store.Tunes[id].Exclude = true;
        }

        public void MarkAsSessionMaintained(string id)
        {
            EnsureTuneRecord(id);
            _store.Tunes[id].Maintenance = "session";
        }

        public void Save()
        {
            if (_store == null || string.IsNullOrEmpty(_storePath))
            {
                throw new InvalidOperationException("Nothing to save.");
            }
            _storeManager.Save(_storePath, _store);
        }

        public ReviewStats GetStats(SelectionService.SelectionOptions options)
        {
            if (_allTunes == null || _store == null)
            {
                throw new InvalidOperationException("Engine not loaded. Call Load() first.");
            }

            var eligibleTunes = _allTunes
                .Where(t => t.IsLearned)
                .ToList();

            var excludedCount = eligibleTunes.Count(t => _store.Tunes.TryGetValue(t.Id, out var r) && r.Exclude);
            var sessionMaintainedCount = eligibleTunes.Count(t => _store.Tunes.TryGetValue(t.Id, out var r) && r.Maintenance == "session");

            var activeTunes = eligibleTunes
                .Where(t => 
                {
                    if (string.IsNullOrEmpty(t.Id)) return false;
                    if (_store.Tunes.TryGetValue(t.Id, out var r))
                    {
                        return !r.Exclude && r.Maintenance != "session";
                    }
                    return true;
                })
                .ToList();

            var dueInfos = activeTunes.Select(t => 
            {
                _store.Tunes.TryGetValue(t.Id, out var r);
                return _selectionService.CalculateDue(r, options);
            }).ToList();

            return new ReviewStats
            {
                TotalLearned = eligibleTunes.Count,
                TotalEligible = activeTunes.Count,
                ExcludedCount = excludedCount,
                SessionMaintainedCount = sessionMaintainedCount,
                NeverReviewedCount = dueInfos.Count(di => di.NeverReviewed),
                OverdueCount = dueInfos.Count(di => di.IsOverdue && !di.NeverReviewed),
                MostOverdue = activeTunes
                    .Select(t => {
                        _store.Tunes.TryGetValue(t.Id, out var r);
                        return new { Tune = t, Due = _selectionService.CalculateDue(r, options) };
                    })
                    .Where(x => x.Due.IsOverdue && !x.Due.NeverReviewed)
                    .OrderByDescending(x => x.Due.OverdueDays)
                    .Take(10)
                    .Select(x => $"{x.Tune.Title} ({x.Due.OverdueDays} days overdue)")
                    .ToList()
            };
        }

        public List<TuneNote> FindTunes(string query)
        {
            if (_allTunes == null) throw new InvalidOperationException("Engine not loaded.");
            
            var sanitizedQuery = SanitizeForComparison(query);
            
            return _allTunes
                .Where(t => t.Id == query || SanitizeForComparison(t.Title).Equals(sanitizedQuery, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        private string SanitizeForComparison(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            return input
                .Replace("’", "'") // Normalize smart quotes
                .Replace("‘", "'")
                .Replace("“", "\"")
                .Replace("”", "\"")
                .Trim();
        }

        private void EnsureTuneRecord(string id)
        {
            if (!_store.Tunes.TryGetValue(id, out var record))
            {
                var tune = _allTunes.FirstOrDefault(t => t.Id == id);
                _store.Tunes[id] = new TuneReviewRecord { Name = tune?.Title };
            }
        }

        private int MapScoreToInterval(int score)
        {
            return score switch
            {
                0 => 1,
                1 => 3,
                2 => 7,
                3 => 14,
                4 => 30,
                5 => 60,
                6 => 120,
                7 => 180,
                8 => 270,
                9 => 365,
                _ => 366
            };
        }
    }

    public class ReviewStats
    {
        public int TotalLearned { get; set; }
        public int TotalEligible { get; set; }
        public int ExcludedCount { get; set; }
        public int SessionMaintainedCount { get; set; }
        public int NeverReviewedCount { get; set; }
        public int OverdueCount { get; set; }
        public List<string> MostOverdue { get; set; } = new List<string>();
    }
}
