using System;
using System.Collections.Generic;
using CloudAwesome.FolkTune.Helpers;
using CloudAwesome.FolkTune.Models;
using CloudAwesome.FolkTune.Services;
using NUnit.Framework;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class SelectionServiceTests
    {
        private SelectionService _selectionService;

        [SetUp]
        public void SetUp()
        {
            _selectionService = new SelectionService();
        }

        [Test]
        public void CalculateDue_NeverReviewed_IsOverdue()
        {
            var options = new SelectionService.SelectionOptions { Today = new DateTime(2026, 1, 1), DefaultInterval = 365 };
            var result = _selectionService.CalculateDue(null, options);

            Assert.That(result.NeverReviewed, Is.True);
            Assert.That(result.IsOverdue, Is.True);
        }

        [Test]
        public void CalculateDue_RecentReview_IsNotOverdue()
        {
            var options = new SelectionService.SelectionOptions { Today = new DateTime(2026, 1, 1), DefaultInterval = 365 };
            var record = new TuneReviewRecord { Last = "2025-12-01", IntervalDays = 100 };
            
            var result = _selectionService.CalculateDue(record, options);

            Assert.That(result.IsOverdue, Is.False);
            Assert.That(result.EffectiveLast, Is.EqualTo(new DateTime(2025, 12, 1)));
        }

        [Test]
        public void SelectTunes_PrioritizesOverdueThenNeverReviewedThenNeglected()
        {
            var options = new SelectionService.SelectionOptions 
            { 
                Count = 5, 
                Today = new DateTime(2026, 1, 1), 
                DefaultInterval = 365 
            };

            var tunes = new List<TuneNote>
            {
                new TuneNote { Id = "1", Title = "Overdue 1", Learn = false },
                new TuneNote { Id = "2", Title = "Overdue 2", Learn = false },
                new TuneNote { Id = "3", Title = "Never Reviewed", Learn = false },
                new TuneNote { Id = "4", Title = "Neglected 1", Learn = false },
                new TuneNote { Id = "5", Title = "Recent", Learn = false }
            };

            var store = new ReviewStore
            {
                Tunes = new Dictionary<string, TuneReviewRecord>
                {
                    { "1", new TuneReviewRecord { Last = "2024-01-01", IntervalDays = 30 } }, // Very overdue
                    { "2", new TuneReviewRecord { Last = "2025-12-01", IntervalDays = 10 } }, // Slightly overdue
                    // 3 is never reviewed
                    { "4", new TuneReviewRecord { Last = "2025-01-01", IntervalDays = 500 } }, // Not due, but old
                    { "5", new TuneReviewRecord { Last = "2025-12-30", IntervalDays = 365 } }  // Recent
                }
            };

            var selected = _selectionService.SelectTunes(tunes, store, options, WikiLinkHelper.ExtractDisplayText);

            Assert.That(selected.Count, Is.EqualTo(5));
            Assert.That(selected[0].Title, Is.EqualTo("Overdue 1"));
            Assert.That(selected[1].Title, Is.EqualTo("Overdue 2"));
            Assert.That(selected[2].Title, Is.EqualTo("Never Reviewed"));
            Assert.That(selected[3].Title, Is.EqualTo("Neglected 1"));
            Assert.That(selected[4].Title, Is.EqualTo("Recent"));
        }
    }
}
