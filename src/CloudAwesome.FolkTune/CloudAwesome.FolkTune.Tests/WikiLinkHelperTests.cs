using CloudAwesome.FolkTune.Helpers;
using NUnit.Framework;

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
        public void ExtractDisplayText_ReturnsExpected(string input, string expected)
        {
            var result = WikiLinkHelper.ExtractDisplayText(input);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
