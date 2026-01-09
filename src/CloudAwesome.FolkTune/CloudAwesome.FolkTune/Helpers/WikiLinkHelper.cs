using System.Text.RegularExpressions;

namespace CloudAwesome.FolkTune.Helpers
{
    public static class WikiLinkHelper
    {
        private static readonly Regex WikiLinkRegex = new Regex(@"\[\[(.*?)\]\]", RegexOptions.Compiled);

        public static string ExtractDisplayText(object input)
        {
            if (input == null) return string.Empty;
            
            string text = input.ToString();
            var match = WikiLinkRegex.Match(text);
            
            if (match.Success)
            {
                string content = match.Groups[1].Value;
                if (content.Contains("|"))
                {
                    return content.Split('|')[1];
                }
                
                if (content.Contains("/"))
                {
                    return content.Substring(content.LastIndexOf('/') + 1);
                }
                
                return content;
            }

            return text;
        }
    }
}
