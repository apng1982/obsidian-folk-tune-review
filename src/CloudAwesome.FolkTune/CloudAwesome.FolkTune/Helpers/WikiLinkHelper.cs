using System.Collections;
using System.Text.RegularExpressions;

namespace CloudAwesome.FolkTune.Helpers
{
    public static class WikiLinkHelper
    {
        private static readonly Regex WikiLinkRegex = new Regex(@"\[\[(.*?)\]\]", RegexOptions.Compiled);

        public static string ExtractDisplayText(object input)
        {
            if (input == null) return string.Empty;

            // Handle lists (YAML sequences)
            if (input is IEnumerable list && !(input is string))
            {
                var parts = new List<string>();
                foreach (var item in list)
                {
                    parts.Add(ExtractSingle(item));
                }
                return string.Join(", ", parts);
            }

            return ExtractSingle(input);
        }
        
        private static string ExtractSingle(object input)
        {
            if (input == null) return string.Empty;
            
            string text = input.ToString();
            var match = WikiLinkRegex.Match(text);
            string result;
            
            if (match.Success)
            {
                string content = match.Groups[1].Value;
                if (content.Contains("|"))
                {
                    result = content.Split('|')[1];
                }
                else if (content.Contains("/"))
                {
                    result = content.Substring(content.LastIndexOf('/') + 1);
                }
                else
                {
                    result = content;
                }
            }
            else
            {
                result = text;
            }

            return result.Replace("[", "[[").Replace("]", "]]");
        }
    }
}
