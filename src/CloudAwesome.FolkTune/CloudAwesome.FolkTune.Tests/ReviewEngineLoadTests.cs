using CloudAwesome.FolkTune.Services;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class ReviewEngineLoadTests
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
        public void Load_ScansOnlyTunesTunesDirectory()
        {
            var tunesPath = Path.Combine(_tempVaultPath, "Tunes", "Tunes");
            var setsPath = Path.Combine(_tempVaultPath, "Tunes", "Sets");
            var queriesPath = Path.Combine(_tempVaultPath, "Queries");
            var refPath = Path.Combine(_tempVaultPath, "Ref");
            var templatesPath = Path.Combine(_tempVaultPath, "Templates");
            var dotsPath = Path.Combine(_tempVaultPath, "Tunes", "Dots");

            Directory.CreateDirectory(tunesPath);
            Directory.CreateDirectory(setsPath);
            Directory.CreateDirectory(queriesPath);
            Directory.CreateDirectory(refPath);
            Directory.CreateDirectory(templatesPath);
            Directory.CreateDirectory(dotsPath);

            File.WriteAllText(
                Path.Combine(tunesPath, "TuneA.md"),
                """
                ---
                id: "tune-a"
                learn: false
                origin: "Scottish"
                ---
                body
                """);

            File.WriteAllText(
                Path.Combine(tunesPath, "TuneB.md"),
                """
                ---
                id: "tune-b"
                learn: false
                origin: "Irish"
                ---
                body
                """);

            File.WriteAllText(
                Path.Combine(setsPath, "SetA.md"),
                """
                ---
                id: "set-a"
                learn: false
                origin: "Set Origin"
                ---
                body
                """);

            File.WriteAllText(
                Path.Combine(queriesPath, "QueryA.md"),
                """
                ---
                id: "query-a"
                learn: false
                origin: "Query Origin"
                ---
                body
                """);

            File.WriteAllText(
                Path.Combine(refPath, "RefA.md"),
                """
                ---
                id: "ref-a"
                learn: false
                origin: "Reference Origin"
                ---
                body
                """);

            File.WriteAllText(
                Path.Combine(templatesPath, "TemplateA.md"),
                """
                ---
                id: "template-a"
                learn: false
                origin: "Template Origin"
                ---
                body
                """);

            File.WriteAllText(
                Path.Combine(dotsPath, "DotA.md"),
                """
                ---
                id: "dot-a"
                learn: false
                origin: "Dot Origin"
                ---
                body
                """);

            var engine = new ReviewEngine(
                new VaultScanner(),
                new ReviewStoreManager(),
                new SelectionService(),
                new IdInitializer());

            var loadResult = engine.Load(_tempVaultPath);

            Assert.That(loadResult.ReviewStoreCreated, Is.True);

            var candidates = engine.GetReviewCandidates(new SelectionService.SelectionOptions
            {
                Count = 10,
                Today = DateTime.Today
            });

            Assert.That(candidates.Count, Is.EqualTo(2));
            Assert.That(candidates.Any(t => t.Title == "TuneA"), Is.True);
            Assert.That(candidates.Any(t => t.Title == "TuneB"), Is.True);
            Assert.That(candidates.Any(t => t.Title == "SetA"), Is.False);
            Assert.That(candidates.Any(t => t.Title == "QueryA"), Is.False);
            Assert.That(candidates.Any(t => t.Title == "RefA"), Is.False);
            Assert.That(candidates.Any(t => t.Title == "TemplateA"), Is.False);
            Assert.That(candidates.Any(t => t.Title == "DotA"), Is.False);
        }

        [Test]
        public void Load_IgnoresMalformedMarkdownOutsideTunesTunes()
        {
            var tunesPath = Path.Combine(_tempVaultPath, "Tunes", "Tunes");
            var queriesPath = Path.Combine(_tempVaultPath, "Queries");

            Directory.CreateDirectory(tunesPath);
            Directory.CreateDirectory(queriesPath);

            File.WriteAllText(
                Path.Combine(tunesPath, "TuneA.md"),
                """
                ---
                id: "tune-a"
                learn: false
                origin: "Scottish"
                ---
                body
                """);

            File.WriteAllText(
                Path.Combine(queriesPath, "Broken.md"),
                """
                ---
                id: "broken"
                learn: [not valid yaml
                ---
                body
                """);

            var engine = new ReviewEngine(
                new VaultScanner(),
                new ReviewStoreManager(),
                new SelectionService(),
                new IdInitializer());

            Assert.DoesNotThrow(() => engine.Load(_tempVaultPath));

            var candidates = engine.GetReviewCandidates(new SelectionService.SelectionOptions
            {
                Count = 10,
                Today = DateTime.Today
            });

            Assert.That(candidates.Count, Is.EqualTo(1));
            Assert.That(candidates.Single().Title, Is.EqualTo("TuneA"));
        }

        [Test]
        public void Load_WhenTunesTunesDirectoryMissing_Throws()
        {
            Directory.CreateDirectory(Path.Combine(_tempVaultPath, "Queries"));

            var engine = new ReviewEngine(
                new VaultScanner(),
                new ReviewStoreManager(),
                new SelectionService(),
                new IdInitializer());

            var ex = Assert.Throws<DirectoryNotFoundException>(() => engine.Load(_tempVaultPath));

            Assert.That(ex!.Message, Does.Contain(Path.Combine("Tunes", "Tunes")));
        }
        
        [Test]
        public void Load_FindTunes_OnlyFindsEntriesFromTunesTunesDirectory()
        {
            var tunesPath = Path.Combine(_tempVaultPath, "Tunes", "Tunes");
            var setsPath = Path.Combine(_tempVaultPath, "Tunes", "Sets");

            Directory.CreateDirectory(tunesPath);
            Directory.CreateDirectory(setsPath);

            File.WriteAllText(Path.Combine(tunesPath, "The Scholar.md"), """
                                                                         ---
                                                                         id: "tune-scholar"
                                                                         learn: false
                                                                         ---
                                                                         body
                                                                         """);

            File.WriteAllText(Path.Combine(setsPath, "The Scholar Set.md"), """
                                                                            ---
                                                                            id: "set-scholar"
                                                                            learn: false
                                                                            ---
                                                                            body
                                                                            """);

            var engine = new ReviewEngine(
                new VaultScanner(),
                new ReviewStoreManager(),
                new SelectionService(),
                new IdInitializer());

            engine.Load(_tempVaultPath);

            var matches = engine.FindTunes("Scholar");

            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches.Single().Title, Is.EqualTo("The Scholar"));
        }
    }
}
