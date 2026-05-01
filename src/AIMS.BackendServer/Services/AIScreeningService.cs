using System.Text;
using System.Text.RegularExpressions;
using AIMS.BackendServer.Data.Entities;
using UglyToad.PdfPig;
using AIMS.BackendServer.Services.ML;

namespace AIMS.BackendServer.Services;

public interface IAIScreeningService
{
    Task<CVParsedData> ParseCVAsync(string filePath, int applicationId);

    Task<AIScreeningResult> ScreenCVAsync(
        string cvText,
        string jobDescriptionText,
        int applicationId,
        decimal? candidateGpa = null,
        decimal? minGpa = null);

    Task<AIMS.BackendServer.Services.ML.FeatureData> ExtractFeaturesAsync(
        string cvText,
        string jobDescriptionText,
        decimal? candidateGpa = null);
}

public class AIScreeningService : IAIScreeningService
{
    private const int MinimumReadableTextLength = 50;

    private readonly ILogger<AIScreeningService> _logger;
    private readonly AIMS.BackendServer.Services.ML.IScreeningModelService? _screeningModelService;

    public AIScreeningService(ILogger<AIScreeningService> logger,
        AIMS.BackendServer.Services.ML.IScreeningModelService? screeningModelService = null)
    {
        _logger = logger;
        _screeningModelService = screeningModelService;
    }

    private static readonly HashSet<string> Stopwords = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "is", "are", "was", "were", "be", "been",
        "have", "has", "had", "do", "does", "did", "will", "would", "could",
        "should", "may", "might", "shall", "can", "this", "that", "these",
        "those", "i", "me", "my", "we", "our", "you", "your", "he", "his",
        "she", "her", "it", "its", "they", "their", "what", "which", "who",
        "when", "where", "how", "all", "each", "every", "both", "few", "more",
        "most", "other", "some", "such", "no", "not", "only", "same", "so",
        "than", "too", "very", "just", "about", "above", "after", "before",
        "va", "hoac", "la", "cua", "cho", "voi", "trong", "cac", "nhung",
        "duoc", "ung", "vien", "kinh", "nghiem", "lam", "viec",
    };

    private static readonly Dictionary<string, string[]> SkillCatalog = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["C#"] = new[] { "c#", "csharp", "c sharp" },
        [".NET"] = new[] { ".net", "dotnet", "asp.net", "aspnet", "asp.net core", "aspnet core" },
        ["ASP.NET Core"] = new[] { "asp.net core", "aspnet core", "web api", "asp.net mvc", "mvc" },
        ["Entity Framework"] = new[] { "entity framework", "ef core", "efcore", "linq" },
        ["SQL"] = new[] { "sql", "sql server", "mysql", "postgresql", "database" },
        ["JavaScript"] = new[] { "javascript", "js", "ecmascript" },
        ["TypeScript"] = new[] { "typescript", "ts" },
        ["React"] = new[] { "react", "reactjs", "react.js", "react native" },
        ["Vue"] = new[] { "vue", "vuejs", "vue.js" },
        ["Angular"] = new[] { "angular" },
        ["HTML"] = new[] { "html", "html5" },
        ["CSS"] = new[] { "css", "css3", "tailwind", "tailwindcss", "bootstrap" },
        ["Node.js"] = new[] { "nodejs", "node.js", "express", "expressjs" },
        ["Java"] = new[] { "java", "spring", "spring boot" },
        ["Python"] = new[] { "python", "django", "flask", "fastapi" },
        ["Machine Learning"] = new[] { "machine learning", "ml", "tensorflow", "pytorch", "ml.net" },
        ["NLP"] = new[] { "nlp", "natural language processing", "text mining" },
        ["Docker"] = new[] { "docker", "container" },
        ["Kubernetes"] = new[] { "kubernetes", "k8s" },
        ["Azure"] = new[] { "azure", "azure devops" },
        ["AWS"] = new[] { "aws", "amazon web services", "ecr", "ec2", "s3" },
        ["Git"] = new[] { "git", "github", "gitlab", "bitbucket" },
        ["REST API"] = new[] { "rest", "rest api", "api", "http api" },
        ["JWT"] = new[] { "jwt", "oauth", "openid connect" },
        ["Microservices"] = new[] { "microservices", "microservice" },
        ["Testing"] = new[] { "testing", "manual test", "api test", "automation test", "selenium", "k6" },
        ["UML"] = new[] { "uml", "use case", "bpmn", "frs", "brd" },
        ["Power BI"] = new[] { "powerbi", "power bi", "dashboard" },
        ["Figma"] = new[] { "figma", "ui/ux", "wireframe", "prototype" },
    };

    public async Task<CVParsedData> ParseCVAsync(string filePath, int applicationId)
    {
        try
        {
            var rawText = NormalizeWhitespace(await ExtractTextAsync(filePath));
            if (string.IsNullOrWhiteSpace(rawText) || rawText.Length < MinimumReadableTextLength)
                throw new InvalidDataException("PDF không đọc được. Vui lòng dùng PDF có thể copy text.");

            var parsed = new CVParsedData
            {
                ApplicationId = applicationId,
                RawText = rawText,
                FullName = ExtractName(rawText),
                EmailExtracted = ExtractEmail(rawText),
                PhoneExtracted = ExtractPhone(rawText),
                SkillsExtracted = string.Join(", ", ExtractSkills(rawText)),
                EducationExtracted = ExtractSection(rawText,
                    new[] { "education", "academic", "university", "degree", "hoc van", "dao tao" }),
                ExperienceExtracted = ExtractSection(rawText,
                    new[] { "experience", "work", "project", "internship", "kinh nghiem", "du an" }),
                ParsedAt = DateTime.UtcNow,
            };

            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CV parsing failed for application {ApplicationId}.", applicationId);
            throw;
        }
    }

    public Task<AIMS.BackendServer.Services.ML.FeatureData> ExtractFeaturesAsync(
            string cvText,
            string jobDescriptionText,
            decimal? candidateGpa = null)
        {
            var normalizedCv = NormalizeWhitespace(cvText);
            var normalizedJd = NormalizeWhitespace(jobDescriptionText);

            var cvTokens = Tokenize(normalizedCv);
            var jdTokens = Tokenize(normalizedJd);
            var vocabulary = cvTokens.Union(jdTokens, StringComparer.OrdinalIgnoreCase).Distinct().ToList();

            var cvVector = ComputeTfIdf(cvTokens, vocabulary, new[] { cvTokens, jdTokens });
            var jdVector = ComputeTfIdf(jdTokens, vocabulary, new[] { cvTokens, jdTokens });
            var cosineScore = CosineSimilarity(cvVector, jdVector);

            var jdSkills = ExtractSkills(normalizedJd);
            var cvSkills = ExtractSkills(normalizedCv);
            var matched = jdSkills.Intersect(cvSkills, StringComparer.OrdinalIgnoreCase).ToList();
            var missing = jdSkills.Except(cvSkills, StringComparer.OrdinalIgnoreCase).ToList();

            decimal? gpaValue = candidateGpa ?? ExtractGpa(normalizedCv);
            var years = ExtractYearsOfExperience(normalizedCv);

            var feature = new AIMS.BackendServer.Services.ML.FeatureData
            {
                SkillsMatchedCount = matched.Count,
                MissingSkillsCount = missing.Count,
                Gpa = (float)(gpaValue ?? 0m),
                YearsOfExperience = years,
                CosineSimilarity = (float)cosineScore,
                Label = false // placeholder when extracting; caller can set label before export
            };

            return Task.FromResult(feature);
        }

    public async Task<AIScreeningResult> ScreenCVAsync(
        string cvText,
        string jobDescriptionText,
        int applicationId,
        decimal? candidateGpa = null,
        decimal? minGpa = null)
    {
        var normalizedCv = NormalizeWhitespace(cvText);
        var normalizedJd = NormalizeWhitespace(jobDescriptionText);

        if (string.IsNullOrWhiteSpace(normalizedCv) || normalizedCv.Length < MinimumReadableTextLength)
            return BuildFailedResult(applicationId,
                "PDF không đọc được. Vui lòng dùng PDF có thể copy text.");

        if (string.IsNullOrWhiteSpace(normalizedJd))
            return BuildFailedResult(applicationId,
                "JD chưa có nội dung để AI so khớp.");

        var cvTokens = Tokenize(normalizedCv);
        var jdTokens = Tokenize(normalizedJd);
        var vocabulary = cvTokens.Union(jdTokens, StringComparer.OrdinalIgnoreCase).Distinct().ToList();

        var cvVector = ComputeTfIdf(cvTokens, vocabulary, new[] { cvTokens, jdTokens });
        var jdVector = ComputeTfIdf(jdTokens, vocabulary, new[] { cvTokens, jdTokens });
        var cosineScore = CosineSimilarity(cvVector, jdVector);

        var gpaScore = CalculateGpaScore(candidateGpa ?? ExtractGpa(normalizedCv), minGpa);
        var experienceScore = CalculateExperienceScore(ExtractYearsOfExperience(normalizedCv));
        var totalScore = (cosineScore * 0.6) + (gpaScore * 0.2) + (experienceScore * 0.2);
        var matchingScore = Math.Round(Math.Clamp(totalScore, 0, 1) * 100, 2);

        var jdSkills = ExtractSkills(normalizedJd);
        var cvSkills = ExtractSkills(normalizedCv);
        var matched = jdSkills.Intersect(cvSkills, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var missing = jdSkills.Except(cvSkills, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        // Build feature vector for ML model
        var skillsMatchedCount = matched.Count;
        var missingSkillsCount = missing.Count;
        decimal? gpaValue = candidateGpa ?? ExtractGpa(normalizedCv);
        var years = ExtractYearsOfExperience(normalizedCv);

        var feature = new FeatureData
        {
            SkillsMatchedCount = skillsMatchedCount,
            MissingSkillsCount = missingSkillsCount,
            Gpa = (float)(gpaValue ?? 0m),
            YearsOfExperience = years,
            CosineSimilarity = (float)cosineScore
        };

        // If ML model available, use it. Otherwise fallback to rule-based score.
        if (_screeningModelService is not null && _screeningModelService.IsModelAvailable)
        {
            try
            {
                var pred = await _screeningModelService.PredictAsync(feature);
                if (pred is not null)
                {
                    var probability = Math.Clamp(pred.Probability, 0f, 1f);
                    var matchingPercent = Math.Round(probability * 100, 2);
                    return new AIScreeningResult
                    {
                        ApplicationId = applicationId,
                        MatchingScore = (decimal)matchingPercent,
                        KeywordsMatched = string.Join(", ", matched),
                        KeywordsMissing = string.Join(", ", missing),
                        ProcessingStatus = "Completed",
                        ErrorMessage = null,
                        ScreenedAt = DateTime.UtcNow,
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ML prediction failed for application {ApplicationId}. Falling back to rule-based scoring.", applicationId);
            }
        }

        // Fallback: original heuristic
        return new AIScreeningResult
        {
            ApplicationId = applicationId,
            MatchingScore = (decimal)matchingScore,
            KeywordsMatched = string.Join(", ", matched),
            KeywordsMissing = string.Join(", ", missing),
            ProcessingStatus = "Completed",
            ErrorMessage = null,
            ScreenedAt = DateTime.UtcNow,
        };
    }

    private static AIScreeningResult BuildFailedResult(int applicationId, string errorMessage)
        => new()
        {
            ApplicationId = applicationId,
            MatchingScore = 0,
            KeywordsMatched = string.Empty,
            KeywordsMissing = string.Empty,
            ProcessingStatus = "Failed",
            ErrorMessage = errorMessage,
            ScreenedAt = DateTime.UtcNow,
        };

    private static async Task<string> ExtractTextAsync(string filePath)
    {
        await Task.CompletedTask;

        if (!string.Equals(Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Hệ thống chỉ chấp nhận CV định dạng PDF.");

        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(filePath);
        foreach (var page in pdf.GetPages())
            sb.AppendLine(page.Text);

        return sb.ToString();
    }

    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var normalized = RemoveDiacritics(text).ToLowerInvariant();
        return Regex.Split(normalized, @"[^a-z0-9#\.\+]+")
            .Select(t => t.Trim('.', '+'))
            .Where(t => t.Length > 1)
            .Where(t => !Stopwords.Contains(t))
            .ToList();
    }

    private static double[] ComputeTfIdf(
        List<string> tokens,
        List<string> vocabulary,
        List<string>[] allDocTokens)
    {
        var total = tokens.Count;
        var vector = new double[vocabulary.Count];

        if (total == 0)
            return vector;

        for (var i = 0; i < vocabulary.Count; i++)
        {
            var term = vocabulary[i];
            var tf = tokens.Count(t => string.Equals(t, term, StringComparison.OrdinalIgnoreCase)) / (double)total;
            var df = allDocTokens.Count(doc => doc.Contains(term, StringComparer.OrdinalIgnoreCase));
            var idf = Math.Log((allDocTokens.Length + 1d) / (df + 1d)) + 1d;
            vector[i] = tf * idf;
        }

        return vector;
    }

    private static double CosineSimilarity(double[] a, double[] b)
    {
        double dot = 0;
        double normA = 0;
        double normB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator < 1e-10 ? 0 : dot / denominator;
    }

    private static string? ExtractEmail(string text)
    {
        var match = Regex.Match(text,
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        return match.Success ? match.Value : null;
    }

    private static string? ExtractPhone(string text)
    {
        var match = Regex.Match(text,
            @"(?:\+84|0)(?:[\s\.-]?\d){9,10}");
        return match.Success ? Regex.Replace(match.Value, @"[\s\.-]", string.Empty) : null;
    }

    private static string? ExtractName(string text)
    {
        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length is > 2 and < 60)
            .Take(8)
            .ToList();

        return lines.FirstOrDefault(l =>
            !Regex.IsMatch(l, @"[\d@]")
            && Regex.IsMatch(l, @"^[A-Za-zÀ-ỹ\s'.-]+$"));
    }

    private static IReadOnlyCollection<string> ExtractSkills(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var normalized = $" {RemoveDiacritics(text).ToLowerInvariant()} ";
        return SkillCatalog
            .Where(skill => skill.Value.Any(alias => ContainsSkill(normalized, alias)))
            .Select(skill => skill.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ContainsSkill(string normalizedText, string alias)
    {
        var normalizedAlias = Regex.Escape(RemoveDiacritics(alias).ToLowerInvariant());
        return Regex.IsMatch(normalizedText, $@"(?<![a-z0-9]){normalizedAlias}(?![a-z0-9])");
    }

    private static string? ExtractSection(string text, string[] sectionKeywords)
    {
        var lines = text.Split('\n').ToList();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = RemoveDiacritics(lines[i]).ToLowerInvariant().Trim();
            if (!sectionKeywords.Any(k => line.Contains(RemoveDiacritics(k), StringComparison.OrdinalIgnoreCase)))
                continue;

            var section = lines
                .Skip(i + 1)
                .Take(10)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            return string.Join(" | ", section);
        }

        return null;
    }

    private static decimal? ExtractGpa(string text)
    {
        var match = Regex.Match(text, @"\b(?:gpa|điểm|diem)\s*[:\-]?\s*(?<gpa>[0-4](?:[\.,]\d{1,2})?)\b",
            RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;

        var value = match.Groups["gpa"].Value.Replace(',', '.');
        return decimal.TryParse(value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var gpa)
            ? Math.Clamp(gpa, 0, 4)
            : null;
    }

    private static int ExtractYearsOfExperience(string text)
    {
        var normalized = RemoveDiacritics(text).ToLowerInvariant();
        var matches = Regex.Matches(normalized,
            @"(?<years>\d{1,2})(\+)?\s*(years?|yrs?|nam)\s*(of\s*)?(experience|kinh nghiem)?");

        return matches
            .Select(m => int.TryParse(m.Groups["years"].Value, out var years) ? years : 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static double CalculateGpaScore(decimal? candidateGpa, decimal? minGpa)
    {
        if (!candidateGpa.HasValue)
            return minGpa.HasValue ? 0.5 : 1.0;

        if (!minGpa.HasValue || minGpa.Value <= 0)
            return Math.Clamp((double)(candidateGpa.Value / 4m), 0, 1);

        return Math.Clamp((double)(candidateGpa.Value / minGpa.Value), 0, 1);
    }

    private static double CalculateExperienceScore(int years)
        => years switch
        {
            <= 0 => 0.5,
            1 => 0.65,
            2 => 0.8,
            >= 3 => 1.0,
        };

    private static string NormalizeWhitespace(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return Regex.Replace(text, @"[ \t\r\f\v]+", " ").Trim();
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Select(c => c == 'đ' ? 'd' : c == 'Đ' ? 'D' : c)
            .ToArray();

        return new string(chars).Normalize(NormalizationForm.FormC);
    }
}
