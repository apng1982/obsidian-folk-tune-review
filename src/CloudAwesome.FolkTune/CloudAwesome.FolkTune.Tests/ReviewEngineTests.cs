using System;
using System.Collections.Generic;
using CloudAwesome.FolkTune.Models;
using CloudAwesome.FolkTune.Services;
using NUnit.Framework;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class ReviewEngineTests
    {
        private ReviewEngine _engine;
        private ReviewStore _store;

        [SetUp]
        public void SetUp()
        {
            // We can't easily mock VaultScanner/ReviewStoreManager without refactoring to interfaces,
            // so we'll test the logic that doesn't strictly depend on the IO parts or we'll bypass them if possible.
            // For these tests, we'll manually set the private fields via reflection or just accept we're doing a bit of integration.
            // Actually, let's just test SubmitReview by ensuring it updates a store we provide.
            
            _engine = new ReviewEngine(new VaultScanner(), new ReviewStoreManager(), new SelectionService(), new IdInitializer());
            _store = new ReviewStore();
            
            // Injecting store and allTunes via reflection for testing logic
            var storeField = typeof(ReviewEngine).GetField("_store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            storeField.SetValue(_engine, _store);
            
            var allTunesField = typeof(ReviewEngine).GetField("_allTunes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            allTunesField.SetValue(_engine, new List<TuneNote> { 
                new TuneNote { Id = "123", Title = "Test Tune" },
                new TuneNote { Id = "456", Title = "Frank's Reel" },
                new TuneNote { Id = "789", Title = "Margret’s Waltz" } // Uses curly quote in store
            });
        }
        
        [Test]
        [TestCase("Frank's Reel")]
        [TestCase("Margret's Waltz")] // Searching with straight quote
        [TestCase("Margret’s Waltz")] // Searching with curly quote
        public void FindTunes_HandlesApostrophesAndSmartQuotes(string query)
        {
            var results = _engine.FindTunes(query);
            
            Assert.That(results.Count, Is.GreaterThan(0), $"Failed to find tune with query: {query}");
            Assert.That(results[0].Title, Does.Contain("Reel").Or.Contains("Waltz"));
        }

        [Test]
        public void SubmitReview_UpdatesLastScoreAndInterval()
        {
            _engine.SubmitReview("123", 3, "Some notes");

            Assert.That(_store.Tunes.ContainsKey("123"), Is.True);
            var record = _store.Tunes["123"];
            Assert.That(record.Score, Is.EqualTo(3));
            Assert.That(record.Notes, Is.EqualTo("Some notes"));
            Assert.That(record.IntervalDays, Is.EqualTo(90));
            Assert.That(record.Last, Is.EqualTo(DateTime.Today.ToString("yyyy-MM-dd")));
        }

        [Test]
        public void MarkAsPlayed_UpdatesSessionLast()
        {
            var date = new DateTime(2025, 12, 25);
            _engine.MarkAsPlayed("123", date);

            Assert.That(_store.Tunes.ContainsKey("123"), Is.True);
            Assert.That(_store.Tunes["123"].SessionLast, Is.EqualTo("2025-12-25"));
        }

        [Test]
        public void ExcludeTune_SetsExcludeToTrue()
        {
            _engine.ExcludeTune("123");
            Assert.That(_store.Tunes["123"].Exclude, Is.True);
        }

        [Test]
        public void MarkAsSessionMaintained_SetsMaintenanceToSession()
        {
            _engine.MarkAsSessionMaintained("123");
            Assert.That(_store.Tunes["123"].Maintenance, Is.EqualTo("session"));
        }

        [Test]
        public void FindTunes_ReturnsCorrectTunes()
        {
            var results = _engine.FindTunes("Test Tune");
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Id, Is.EqualTo("123"));

            results = _engine.FindTunes("123");
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Title, Is.EqualTo("Test Tune"));
        }
    }
}
