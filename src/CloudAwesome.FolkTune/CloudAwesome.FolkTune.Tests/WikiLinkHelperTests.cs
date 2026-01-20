using CloudAwesome.FolkTune.Helpers;

namespace CloudAwesome.FolkTune.Tests
{
    [TestFixture]
    public class WikiLinkHelperTests
    {
        [Test]
        public void ExtractDisplayText_NullInput_ReturnsEmptyString()
        {
            var result = WikiLinkHelper.ExtractDisplayText(null);
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [TestCase("[[Ref/Geo/Scottish|Scottish]]", "Scottish")]
        [TestCase("[[Ref/Geo/Scottish]]", "Scottish")]
        [TestCase("Scottish", "Scottish")]
        [TestCase("[[Ref/Key/D.|D.]]", "D.")]
        public void ExtractDisplayText_ReturnsExpected(string input, string expected)
        {
            var result = WikiLinkHelper.ExtractDisplayText(input);
            Assert.That(result, Is.EqualTo(expected));
        }
        
        [Test]
        public void ExtractDisplayText_HandlesKeysListInput()
        {
            var input = new List<string> { "[[Ref/Key/D.|D.]]", "[[Ref/Key/G.|G.]]" };
            var result = WikiLinkHelper.ExtractDisplayText(input);
            
            Assert.That(result, Is.EqualTo("D., G."));
        }
        
        [Test]
        public void ExtractDisplayText_HandlesWhistleListInput()
        {
            var input = new List<string> { "[[Ref/Whistle/Tenor E|Tenor E]]", "[[Ref/Whistle/Tenor D|Tenor D]]" };
            var result = WikiLinkHelper.ExtractDisplayText(input);
            
            Assert.That(result, Is.EqualTo("Tenor E, Tenor D"));
        }

        [Test]
        public void ExtractDisplayText_EscapesForSpectre()
        {
            // Spectre uses [ for markup. ExtractSingle should escape it to [[
            var input = "Music [Session]";
            var result = WikiLinkHelper.ExtractDisplayText(input);
            
            Assert.That(result, Is.EqualTo("Music [[Session]]"));
        }
    }
}
