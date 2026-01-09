using System;
using System.Collections.Generic;
using System.Linq;
using CloudAwesome.FolkTune.Models;

namespace CloudAwesome.FolkTune.Services
{
    public class SelectionService
    {
        public class SelectionOptions
        {
            public int Count { get; set; }
            public bool IncludeSession { get; set; }
            public bool IncludeExcluded { get; set; }
            public string OriginFilter { get; set; }
            public int DefaultInterval { get; set; } = 365;
            public DateTime Today { get; set; } = DateTime.Today;
        }

        public List<TuneNote> SelectTunes(
            List<TuneNote> allTunes, 
            ReviewStore store, 
            SelectionOptions options,
            Func<object, string> displayExtractor)
        {
            var eligibleTunes = allTunes
                .Where(t => t.IsLearned)
                .Where(t => 
                {
                    if (string.IsNullOrEmpty(t.Id)) return false;
                    
                    if (store.Tunes.TryGetValue(t.Id, out var record))
                    {
                        if (record.Exclude && !options.IncludeExcluded) return false;
                        if (record.Maintenance == "session" && !options.IncludeSession) return false;
                    }
                    return true;
                });

            if (!string.IsNullOrEmpty(options.OriginFilter))
            {
                eligibleTunes = eligibleTunes.Where(t => 
                    displayExtractor(t.Origin)
                        .Contains(options.OriginFilter, StringComparison.OrdinalIgnoreCase));
            }

            var tuneStates = eligibleTunes.Select(t => 
            {
                store.Tunes.TryGetValue(t.Id, out var record);
                return new { Tune = t, Record = record, DueInfo = CalculateDue(record, options) };
            }).ToList();

            // 1. Eligible overdue tunes first, sorted by most overdue
            var overdue = tuneStates
                .Where(ts => ts.DueInfo.IsOverdue && !ts.DueInfo.NeverReviewed)
                .OrderByDescending(ts => ts.DueInfo.OverdueDays)
                .Select(ts => ts.Tune);

            // 2. Then eligible never-reviewed tunes
            var neverReviewed = tuneStates
                .Where(ts => ts.DueInfo.NeverReviewed)
                .Select(ts => ts.Tune);

            // 3. Then top-up with eligible non-due tunes sorted by oldest effectiveLast
            var nonDue = tuneStates
                .Where(ts => !ts.DueInfo.IsOverdue && !ts.DueInfo.NeverReviewed)
                .OrderBy(ts => ts.DueInfo.EffectiveLast)
                .Select(ts => ts.Tune);

            return overdue
                .Concat(neverReviewed)
                .Concat(nonDue)
                .Take(options.Count)
                .ToList();
        }

        public DueInfo CalculateDue(TuneReviewRecord record, SelectionOptions options)
        {
            if (record == null)
            {
                return new DueInfo { NeverReviewed = true, IsOverdue = true, OverdueDays = int.MaxValue };
            }

            DateTime? last = null;
            if (DateTime.TryParse(record.Last, out var l)) last = l;

            DateTime? sessionLast = null;
            if (DateTime.TryParse(record.SessionLast, out var sl)) sessionLast = sl;

            DateTime? effectiveLast = null;
            if (last.HasValue && sessionLast.HasValue) effectiveLast = last > sessionLast ? last : sessionLast;
            else if (last.HasValue) effectiveLast = last;
            else if (sessionLast.HasValue) effectiveLast = sessionLast;

            if (!effectiveLast.HasValue)
            {
                return new DueInfo { NeverReviewed = true, IsOverdue = true, OverdueDays = int.MaxValue };
            }

            var interval = record.IntervalDays ?? options.DefaultInterval;
            var dueDate = effectiveLast.Value.AddDays(interval);
            var overdueDays = (options.Today - dueDate).Days;

            return new DueInfo
            {
                EffectiveLast = effectiveLast,
                DueDate = dueDate,
                OverdueDays = overdueDays,
                IsOverdue = overdueDays >= 0
            };
        }

        public class DueInfo
        {
            public bool NeverReviewed { get; set; }
            public DateTime? EffectiveLast { get; set; }
            public DateTime? DueDate { get; set; }
            public int OverdueDays { get; set; }
            public bool IsOverdue { get; set; }
        }
    }
}
