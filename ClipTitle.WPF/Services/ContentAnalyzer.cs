using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ClipTitle.Services
{
    public interface IContentAnalyzer
    {
        ContentAnalysis AnalyzeContent(string content);
        string GenerateSmartTitle(ContentAnalysis analysis);
    }

    public class ContentAnalyzer : IContentAnalyzer
    {
        private readonly ILogger<ContentAnalyzer> _logger;
        
        // Common stop words to filter out
        private readonly HashSet<string> _stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "is", "at", "which", "on", "a", "an", "as", "are", "was", "were", "been", "be",
            "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may",
            "might", "must", "can", "this", "that", "these", "those", "i", "you", "he", "she", "it",
            "we", "they", "what", "who", "when", "where", "why", "how", "all", "each", "every", "both",
            "few", "more", "most", "other", "some", "such", "no", "nor", "not", "only", "own", "same",
            "so", "than", "too", "very", "just", "but", "for", "with", "about", "against", "between",
            "into", "through", "during", "before", "after", "above", "below", "to", "from", "up", "down",
            "in", "out", "off", "over", "under", "again", "further", "then", "once", "and", "or", "if"
        };

        // Technical keywords for categorization
        private readonly Dictionary<string, HashSet<string>> _domainKeywords = new Dictionary<string, HashSet<string>>
        {
            ["code"] = new HashSet<string> { "function", "class", "method", "variable", "const", "let", "var", 
                "public", "private", "return", "import", "export", "async", "await", "def", "namespace", 
                "interface", "struct", "enum", "package", "module", "component", "service", "controller" },
            
            ["api"] = new HashSet<string> { "endpoint", "request", "response", "http", "rest", "graphql", 
                "api", "json", "xml", "authorization", "authentication", "token", "header", "payload", "query" },
            
            ["data"] = new HashSet<string> { "database", "table", "query", "select", "insert", "update", 
                "delete", "join", "index", "column", "row", "schema", "sql", "nosql", "mongodb", "redis" },
            
            ["docs"] = new HashSet<string> { "documentation", "guide", "tutorial", "example", "usage", 
                "installation", "configuration", "setup", "reference", "overview", "introduction", "summary" },
            
            ["error"] = new HashSet<string> { "error", "exception", "failed", "failure", "bug", "issue", 
                "problem", "stacktrace", "debug", "warning", "critical", "fatal", "crash", "abort" },
            
            ["config"] = new HashSet<string> { "config", "configuration", "settings", "options", "properties", 
                "environment", "variable", "parameter", "flag", "yaml", "json", "ini", "toml" },
            
            ["test"] = new HashSet<string> { "test", "testing", "unit", "integration", "spec", "assert", 
                "expect", "mock", "stub", "coverage", "scenario", "case", "suite", "fixture" },
            
            ["log"] = new HashSet<string> { "log", "logging", "trace", "debug", "info", "warn", "error", 
                "timestamp", "level", "message", "event", "audit", "monitor", "metrics" }
        };

        public ContentAnalyzer(ILogger<ContentAnalyzer> logger)
        {
            _logger = logger;
        }

        public ContentAnalysis AnalyzeContent(string content)
        {
            var analysis = new ContentAnalysis
            {
                OriginalContent = content,
                ContentLength = content.Length,
                LineCount = content.Split('\n').Length,
                WordCount = CountWords(content)
            };

            // Detect content type
            analysis.ContentType = DetectContentType(content);
            
            // Extract key information
            analysis.Keywords = ExtractKeywords(content);
            analysis.TopNouns = ExtractNouns(content);
            analysis.Domain = DetectDomain(content);
            analysis.HasCode = DetectCode(content);
            analysis.Language = DetectProgrammingLanguage(content);
            
            // Extract entities
            analysis.Urls = ExtractUrls(content);
            analysis.Emails = ExtractEmails(content);
            analysis.FileNames = ExtractFileNames(content);
            analysis.Numbers = ExtractSignificantNumbers(content);
            
            // Generate summary features
            analysis.FirstSentence = ExtractFirstSentence(content);
            analysis.ImportantPhrases = ExtractImportantPhrases(content);
            
            return analysis;
        }

        public string GenerateSmartTitle(ContentAnalysis analysis)
        {
            var titleParts = new List<string>();
            
            // Get key concepts first (most important for identification)
            var keyConcepts = GetTopConcepts(analysis, 3);
            
            // Build readable title based on content type
            if (analysis.ContentType == ContentType.Code && !string.IsNullOrEmpty(analysis.Language))
            {
                // Format: "python-database-connection" or "javascript-react-component"
                titleParts.Add(analysis.Language);
                if (keyConcepts.Any())
                {
                    titleParts.AddRange(keyConcepts.Take(2));
                }
            }
            else if (analysis.ContentType == ContentType.Error)
            {
                // Format: "error-nullreference-exception" 
                titleParts.Add("error");
                if (keyConcepts.Any())
                {
                    titleParts.AddRange(keyConcepts.Take(2));
                }
            }
            else if (analysis.ContentType == ContentType.Log)
            {
                // Format: "log-application-startup"
                titleParts.Add("log");
                if (keyConcepts.Any())
                {
                    titleParts.AddRange(keyConcepts.Take(2));
                }
            }
            else if (analysis.ContentType == ContentType.Config)
            {
                // Format: "config-docker-compose"
                titleParts.Add("config");
                if (keyConcepts.Any())
                {
                    titleParts.AddRange(keyConcepts.Take(2));
                }
            }
            else if (analysis.ContentType == ContentType.Json)
            {
                // Format: "json-api-response"
                titleParts.Add("json");
                if (keyConcepts.Any())
                {
                    titleParts.AddRange(keyConcepts.Take(2));
                }
            }
            else if (analysis.ContentType == ContentType.Data)
            {
                // Format: "data-users-table"
                titleParts.Add("data");
                if (keyConcepts.Any())
                {
                    titleParts.AddRange(keyConcepts.Take(2));
                }
            }
            else if (!string.IsNullOrEmpty(analysis.Domain))
            {
                // For other content, use domain if available
                titleParts.Add(analysis.Domain);
                if (keyConcepts.Any())
                {
                    titleParts.AddRange(keyConcepts.Take(2));
                }
            }
            else if (keyConcepts.Any())
            {
                // Just use key concepts
                titleParts.AddRange(keyConcepts.Take(3));
            }
            else
            {
                // Fallback
                return GenerateFallbackTitle(analysis);
            }
            
            // Join with hyphens for readability
            var title = string.Join("-", titleParts.Where(p => !string.IsNullOrWhiteSpace(p)));
            
            // Ensure title is filesystem-friendly
            title = SanitizeForFilename(title);
            
            // Limit length but keep it readable
            if (title.Length > 50)
            {
                // Try to cut at word boundary
                title = title.Substring(0, 50);
                var lastDash = title.LastIndexOf('-');
                if (lastDash > 30)
                {
                    title = title.Substring(0, lastDash);
                }
            }
            
            return title;
        }

        private ContentType DetectContentType(string content)
        {
            // Check for code patterns
            if (Regex.IsMatch(content, @"(\bfunction\b|\bclass\b|\bdef\b|\bimport\b|\bconst\b|\blet\b|\bvar\b)", RegexOptions.IgnoreCase))
                return ContentType.Code;
            
            // Check for JSON
            if (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
            {
                if (content.Contains("\"") && content.Contains(":"))
                    return ContentType.Json;
            }
            
            // Check for XML/HTML
            if (Regex.IsMatch(content, @"<[^>]+>.*</[^>]+>", RegexOptions.Singleline))
                return ContentType.Markup;
            
            // Check for logs
            if (Regex.IsMatch(content, @"\d{4}-\d{2}-\d{2}.*\d{2}:\d{2}:\d{2}") || 
                Regex.IsMatch(content, @"\[(ERROR|WARN|INFO|DEBUG)\]", RegexOptions.IgnoreCase))
                return ContentType.Log;
            
            // Check for error/stack trace
            if (content.Contains("Exception") || content.Contains("Error") || content.Contains("at ") && content.Contains("("))
                return ContentType.Error;
            
            // Check for tabular data
            if (Regex.IsMatch(content, @"(\t|,|\|).+(\t|,|\|).+(\t|,|\|)", RegexOptions.Multiline))
                return ContentType.Data;
            
            // Check for configuration
            if (Regex.IsMatch(content, @"^\s*[\w\.\-]+\s*[=:]\s*.+$", RegexOptions.Multiline))
                return ContentType.Config;
            
            return ContentType.Text;
        }

        private List<string> ExtractKeywords(string content)
        {
            var words = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            // Extract words
            var matches = Regex.Matches(content, @"\b[a-zA-Z]{3,}\b");
            foreach (Match match in matches)
            {
                var word = match.Value.ToLower();
                if (!_stopWords.Contains(word))
                {
                    words[word] = words.GetValueOrDefault(word, 0) + 1;
                }
            }
            
            // Score words based on frequency and position
            var scoredWords = words.Select(kvp => new
            {
                Word = kvp.Key,
                Score = kvp.Value * GetWordImportanceScore(kvp.Key, content)
            });
            
            return scoredWords
                .OrderByDescending(w => w.Score)
                .Take(10)
                .Select(w => w.Word)
                .ToList();
        }

        private double GetWordImportanceScore(string word, string content)
        {
            double score = 1.0;
            
            // Boost score for words in first 200 characters
            if (content.Length > 200 && content.Substring(0, 200).Contains(word, StringComparison.OrdinalIgnoreCase))
                score *= 1.5;
            
            // Boost score for capitalized words (likely proper nouns)
            if (Regex.IsMatch(content, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(content, $@"\b[A-Z]{Regex.Escape(word.Substring(1))}\b"))
                score *= 1.3;
            
            // Boost score for technical terms
            if (_domainKeywords.Values.Any(set => set.Contains(word)))
                score *= 1.4;
            
            return score;
        }

        private List<string> ExtractNouns(string content)
        {
            var nouns = new List<string>();
            
            // Look for capitalized words (likely proper nouns)
            var capitalizedWords = Regex.Matches(content, @"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\b");
            foreach (Match match in capitalizedWords)
            {
                if (!_stopWords.Contains(match.Value.ToLower()))
                {
                    nouns.Add(match.Value);
                }
            }
            
            // Look for CamelCase/PascalCase (likely class/variable names)
            var camelCaseWords = Regex.Matches(content, @"\b[A-Z][a-z]+(?:[A-Z][a-z]+)+\b");
            foreach (Match match in camelCaseWords)
            {
                nouns.Add(match.Value);
            }
            
            return nouns.Distinct().Take(5).ToList();
        }

        private string DetectDomain(string content)
        {
            var contentLower = content.ToLower();
            var domainScores = new Dictionary<string, int>();
            
            foreach (var domain in _domainKeywords)
            {
                var score = domain.Value.Count(keyword => contentLower.Contains(keyword));
                if (score > 0)
                {
                    domainScores[domain.Key] = score;
                }
            }
            
            return domainScores.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "";
        }

        private bool DetectCode(string content)
        {
            // Common code patterns
            return Regex.IsMatch(content, @"[{}\[\]();]") &&
                   (Regex.IsMatch(content, @"\b(function|class|def|import|const|let|var|public|private|return)\b") ||
                    Regex.IsMatch(content, @"=>|->|==|!=|<=|>=|\+\+|--"));
        }

        private string DetectProgrammingLanguage(string content)
        {
            if (Regex.IsMatch(content, @"\b(using\s+System|namespace\s+\w+|public\s+class)\b"))
                return "csharp";
            if (Regex.IsMatch(content, @"\b(import\s+.*\s+from|export\s+default|const\s+\w+\s*=\s*\(|=>)\b"))
                return "javascript";
            if (Regex.IsMatch(content, @"\b(def\s+\w+\(|import\s+\w+|from\s+\w+\s+import|print\()\b"))
                return "python";
            if (Regex.IsMatch(content, @"\b(package\s+\w+|public\s+static\s+void\s+main|System\.out\.println)\b"))
                return "java";
            if (Regex.IsMatch(content, @"\b(func\s+\w+\(|import\s+""fmt""|package\s+main)\b"))
                return "go";
            if (Regex.IsMatch(content, @"\b(fn\s+\w+\(|use\s+\w+|impl\s+\w+|mut\s+\w+)\b"))
                return "rust";
            if (Regex.IsMatch(content, @"<\?php|function\s+\w+\(.*\)\s*{|\$\w+\s*="))
                return "php";
            if (Regex.IsMatch(content, @"\b(SELECT|INSERT|UPDATE|DELETE|FROM|WHERE|JOIN)\b", RegexOptions.IgnoreCase))
                return "sql";
            
            return "";
        }

        private List<string> ExtractUrls(string content)
        {
            var urls = new List<string>();
            var urlPattern = @"https?://[^\s]+";
            var matches = Regex.Matches(content, urlPattern);
            foreach (Match match in matches)
            {
                urls.Add(match.Value);
            }
            return urls.Take(3).ToList();
        }

        private List<string> ExtractEmails(string content)
        {
            var emails = new List<string>();
            var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
            var matches = Regex.Matches(content, emailPattern);
            foreach (Match match in matches)
            {
                emails.Add(match.Value);
            }
            return emails.Take(3).ToList();
        }

        private List<string> ExtractFileNames(string content)
        {
            var fileNames = new List<string>();
            var filePattern = @"\b[\w\-]+\.(txt|pdf|doc|docx|xls|xlsx|csv|json|xml|html|css|js|ts|cs|java|py|go|rs|cpp|h|md|yml|yaml|ini|conf|log)\b";
            var matches = Regex.Matches(content, filePattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                fileNames.Add(match.Value);
            }
            return fileNames.Distinct().Take(3).ToList();
        }

        private List<string> ExtractSignificantNumbers(string content)
        {
            var numbers = new List<string>();
            // Look for version numbers, IDs, ports, etc.
            var patterns = new[]
            {
                @"\b\d+\.\d+\.\d+\b", // Version numbers
                @"\b\d{4,}\b", // Long numbers (IDs, ports)
                @"#\d+\b", // Issue/PR numbers
            };
            
            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(content, pattern);
                foreach (Match match in matches)
                {
                    numbers.Add(match.Value);
                }
            }
            
            return numbers.Distinct().Take(3).ToList();
        }

        private string ExtractFirstSentence(string content)
        {
            var sentences = Regex.Split(content, @"(?<=[.!?])\s+");
            return sentences.FirstOrDefault()?.Trim() ?? "";
        }

        private List<string> ExtractImportantPhrases(string content)
        {
            var phrases = new List<string>();
            
            // Extract quoted strings
            var quotedStrings = Regex.Matches(content, @"""([^""]+)""");
            foreach (Match match in quotedStrings)
            {
                if (match.Groups[1].Value.Length > 3 && match.Groups[1].Value.Length < 50)
                {
                    phrases.Add(match.Groups[1].Value);
                }
            }
            
            // Extract headings (lines that might be titles)
            var lines = content.Split('\n');
            foreach (var line in lines.Take(10))
            {
                if (line.Length > 5 && line.Length < 100 && 
                    !line.TrimStart().StartsWith("//") && 
                    !line.TrimStart().StartsWith("#") &&
                    Regex.IsMatch(line, @"^[A-Z]"))
                {
                    phrases.Add(line.Trim());
                }
            }
            
            return phrases.Take(3).ToList();
        }

        private List<string> GetTopConcepts(ContentAnalysis analysis, int count)
        {
            var concepts = new List<string>();
            
            // Add top nouns first
            concepts.AddRange(analysis.TopNouns.Take(2));
            
            // Add top keywords that aren't already included
            concepts.AddRange(analysis.Keywords.Where(k => !concepts.Contains(k, StringComparer.OrdinalIgnoreCase)).Take(count - concepts.Count));
            
            // If we have file names, they're often very relevant
            if (analysis.FileNames.Any())
            {
                var fileName = Path.GetFileNameWithoutExtension(analysis.FileNames.First());
                if (!string.IsNullOrEmpty(fileName))
                {
                    concepts.Insert(0, fileName);
                }
            }
            
            return concepts.Take(count).ToList();
        }

        private string GenerateFallbackTitle(ContentAnalysis analysis)
        {
            if (analysis.ContentType == ContentType.Error)
                return "error-trace";
            if (analysis.ContentType == ContentType.Log)
                return "log-output";
            if (analysis.ContentType == ContentType.Code)
                return $"code-snippet{(string.IsNullOrEmpty(analysis.Language) ? "" : "-" + analysis.Language)}";
            if (analysis.ContentType == ContentType.Json)
                return "json-data";
            if (analysis.ContentType == ContentType.Config)
                return "config-settings";
            if (analysis.ContentType == ContentType.Data)
                return "data-table";
            
            return $"clip-{analysis.WordCount}w-{analysis.LineCount}l";
        }

        private string SanitizeForFilename(string title)
        {
            // Replace invalid characters
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("", title.Split(invalid));
            
            // Replace spaces with hyphens
            sanitized = Regex.Replace(sanitized, @"\s+", "-");
            
            // Remove duplicate separators
            sanitized = Regex.Replace(sanitized, @"[-_]{2,}", "-");
            
            // Trim separators
            sanitized = sanitized.Trim('-', '_');
            
            return sanitized.ToLower();
        }

        private int CountWords(string content)
        {
            return Regex.Matches(content, @"\b\w+\b").Count;
        }
    }

    public class ContentAnalysis
    {
        public string OriginalContent { get; set; } = "";
        public ContentType ContentType { get; set; }
        public string Domain { get; set; } = "";
        public List<string> Keywords { get; set; } = new List<string>();
        public List<string> TopNouns { get; set; } = new List<string>();
        public bool HasCode { get; set; }
        public string Language { get; set; } = "";
        public List<string> Urls { get; set; } = new List<string>();
        public List<string> Emails { get; set; } = new List<string>();
        public List<string> FileNames { get; set; } = new List<string>();
        public List<string> Numbers { get; set; } = new List<string>();
        public string FirstSentence { get; set; } = "";
        public List<string> ImportantPhrases { get; set; } = new List<string>();
        public int ContentLength { get; set; }
        public int LineCount { get; set; }
        public int WordCount { get; set; }
    }

    public enum ContentType
    {
        Text,
        Code,
        Json,
        Markup,
        Log,
        Error,
        Data,
        Config
    }
}