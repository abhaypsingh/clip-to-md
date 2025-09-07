using System;
using System.Text;
using System.Text.RegularExpressions;
using ReverseMarkdown;
using HtmlAgilityPack;

namespace ClipTitle.Services;

public interface IMarkdownConverter
{
    string ConvertHtmlToMarkdown(string html);
    string ConvertPlainTextToMarkdown(string plainText);
}

public class MarkdownConverter : IMarkdownConverter
{
    private readonly Converter _reverseMarkdownConverter;
    private readonly Regex _codeDetectionRegex;

    public MarkdownConverter()
    {
        var config = new Config
        {
            UnknownTags = Config.UnknownTagsOption.PassThrough,
            GithubFlavored = true,
            RemoveComments = true,
            SmartHrefHandling = true
        };
        
        _reverseMarkdownConverter = new Converter(config);
        
        // Regex to detect code patterns
        _codeDetectionRegex = new Regex(
            @"(?:function\s+\w+|class\s+\w+|if\s*\(|for\s*\(|while\s*\(|import\s+|from\s+|using\s+|namespace\s+|public\s+|private\s+|protected\s+|def\s+|\{[\s\S]*\})",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
    }

    public string ConvertHtmlToMarkdown(string html)
    {
        try
        {
            // Extract HTML content from CF_HTML format if present
            var actualHtml = ExtractHtmlFromClipboardFormat(html);
            
            // Convert to markdown
            var markdown = _reverseMarkdownConverter.Convert(actualHtml);
            
            // Clean up excessive whitespace
            markdown = Regex.Replace(markdown, @"\n{3,}", "\n\n");
            
            return markdown.Trim();
        }
        catch (Exception ex)
        {
            // Fallback to treating as plain text
            return ConvertPlainTextToMarkdown(html);
        }
    }

    public string ConvertPlainTextToMarkdown(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        // Check if this looks like code
        if (IsLikelyCode(plainText))
        {
            // Wrap in code block
            var language = DetectLanguage(plainText);
            return $"```{language}\n{plainText}\n```";
        }

        // Process as regular text
        var lines = plainText.Split('\n');
        var result = new StringBuilder();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd();
            
            // Check for list patterns
            if (Regex.IsMatch(trimmedLine, @"^\s*[\*\-\+]\s+"))
            {
                result.AppendLine(trimmedLine);
            }
            else if (Regex.IsMatch(trimmedLine, @"^\s*\d+\.\s+"))
            {
                result.AppendLine(trimmedLine);
            }
            // Check for URLs
            else if (Regex.IsMatch(trimmedLine, @"https?://"))
            {
                var urlPattern = @"(https?://[^\s]+)";
                var replaced = Regex.Replace(trimmedLine, urlPattern, "[$1]($1)");
                result.AppendLine(replaced);
            }
            else
            {
                result.AppendLine(trimmedLine);
            }
        }

        return result.ToString().Trim();
    }

    private string ExtractHtmlFromClipboardFormat(string cfHtml)
    {
        // CF_HTML format includes headers like StartHTML, EndHTML, etc.
        var startHtmlMatch = Regex.Match(cfHtml, @"StartHTML:(\d+)");
        var endHtmlMatch = Regex.Match(cfHtml, @"EndHTML:(\d+)");
        
        if (startHtmlMatch.Success && endHtmlMatch.Success)
        {
            var startIndex = int.Parse(startHtmlMatch.Groups[1].Value);
            var endIndex = int.Parse(endHtmlMatch.Groups[1].Value);
            
            if (startIndex < cfHtml.Length && endIndex <= cfHtml.Length && startIndex < endIndex)
            {
                return cfHtml.Substring(startIndex, endIndex - startIndex);
            }
        }
        
        // If no CF_HTML headers found, assume it's raw HTML
        var htmlStartIndex = cfHtml.IndexOf("<html", StringComparison.OrdinalIgnoreCase);
        if (htmlStartIndex >= 0)
        {
            return cfHtml.Substring(htmlStartIndex);
        }
        
        return cfHtml;
    }

    private bool IsLikelyCode(string text)
    {
        // Check for common code patterns
        if (_codeDetectionRegex.IsMatch(text))
        {
            return true;
        }
        
        // Check for high density of brackets/braces
        var bracketCount = 0;
        foreach (var c in text)
        {
            if (c == '{' || c == '}' || c == '[' || c == ']' || c == '(' || c == ')')
            {
                bracketCount++;
            }
        }
        
        var bracketDensity = (double)bracketCount / text.Length;
        if (bracketDensity > 0.05) // More than 5% brackets
        {
            return true;
        }
        
        // Check for semicolon line endings
        var lines = text.Split('\n');
        var semicolonLines = 0;
        foreach (var line in lines)
        {
            if (line.TrimEnd().EndsWith(';'))
            {
                semicolonLines++;
            }
        }
        
        if (lines.Length > 3 && semicolonLines > lines.Length / 2)
        {
            return true;
        }
        
        return false;
    }

    private string DetectLanguage(string code)
    {
        // Simple language detection based on patterns
        if (Regex.IsMatch(code, @"\busing\s+System\b|\bnamespace\s+\w+\b|\bpublic\s+class\b"))
            return "csharp";
        if (Regex.IsMatch(code, @"\bfunction\s+\w+\s*\(|\bconst\s+\w+\s*=|\blet\s+\w+\s*=|\bvar\s+\w+\s*="))
            return "javascript";
        if (Regex.IsMatch(code, @"\bdef\s+\w+\s*\(|\bimport\s+\w+|\bfrom\s+\w+\s+import\b"))
            return "python";
        if (Regex.IsMatch(code, @"\b#include\s*<|\bint\s+main\s*\(|\bstd::\w+"))
            return "cpp";
        if (Regex.IsMatch(code, @"\bpackage\s+\w+|\bimport\s+java\.|\bpublic\s+static\s+void\s+main"))
            return "java";
        if (Regex.IsMatch(code, @"<\?php\b|\$\w+\s*=|\bfunction\s+\w+\s*\("))
            return "php";
        if (Regex.IsMatch(code, @"\bSELECT\s+.*\s+FROM\b|\bINSERT\s+INTO\b|\bCREATE\s+TABLE\b", RegexOptions.IgnoreCase))
            return "sql";
        
        return "";
    }
}