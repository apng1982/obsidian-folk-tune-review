using CloudAwesome.FolkTune.Reviewer.Commands;
using NUnit.Framework;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class SessionCommandTests
    {
        [Test]
        public void TryParseSessionFileLine_WithPlainTune_ReturnsPlayedEntry()
        {
            var result = SessionCommand.TryParseSessionFileLine("Dark Island", out var entry, out var error);

            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(entry, Is.Not.Null);
            var parsed = entry!;
            Assert.That(parsed.Query, Is.EqualTo("Dark Island"));
            Assert.That(parsed.Score, Is.Null);
            Assert.That(parsed.MarkSessionMaintained, Is.False);
        }

        [Test]
        public void TryParseSessionFileLine_WithNumericScore_ReturnsScoredEntry()
        {
            var result = SessionCommand.TryParseSessionFileLine("Campbell's Farewell to Redcastle, 4", out var entry, out var error);

            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(entry, Is.Not.Null);
            var parsed = entry!;
            Assert.That(parsed.Query, Is.EqualTo("Campbell's Farewell to Redcastle"));
            Assert.That(parsed.Score, Is.EqualTo(4));
            Assert.That(parsed.MarkSessionMaintained, Is.False);
        }

        [Test]
        public void TryParseSessionFileLine_WithMaintenanceMarker_ReturnsMaintainedEntry()
        {
            var result = SessionCommand.TryParseSessionFileLine("Dashing White Sergeant, m", out var entry, out var error);

            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(entry, Is.Not.Null);
            var parsed = entry!;
            Assert.That(parsed.Query, Is.EqualTo("Dashing White Sergeant"));
            Assert.That(parsed.Score, Is.Null);
            Assert.That(parsed.MarkSessionMaintained, Is.True);
        }

        [Test]
        public void TryParseSessionFileLine_WithInvalidScore_ReturnsError()
        {
            var result = SessionCommand.TryParseSessionFileLine("Dark Island, 10", out var entry, out var error);

            Assert.That(result, Is.False);
            Assert.That(entry, Is.Null);
            Assert.That(error, Does.Contain("expected rating 0-9 or m"));
        }

        [Test]
        public void TryParseSessionFileLine_WithBlankLine_IsIgnored()
        {
            var result = SessionCommand.TryParseSessionFileLine("   ", out var entry, out var error);

            Assert.That(result, Is.False);
            Assert.That(entry, Is.Null);
            Assert.That(error, Is.Null);
        }
    }
}
