using UglyToad.PdfPig;

namespace AIMS.BackendServer.Services;

// ── Response Models ────────────────────────────────────────────
public record CVParsedResult
{
    public string RawText { get; init; } = "";
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Skills { get; init; } = "";
}

public record ScreeningResult
{
    public decimal MatchingScore { get; init; }
    public string KeywordsMatched { get; init; } = "";
    public string KeywordsMissing { get; init; } = "";
}

// ── Interface ──────────────────────────────────────────────────
public interface IAIScreeningService
{
    Task<CVParsedResult> ParseCVAsync(Stream pdfStream);
    Task<ScreeningResult> ScreenCVAsync(string cvText, string jdText);
}

// ── Implementation ─────────────────────────────────────────────
public class AIScreeningService : IAIScreeningService
{
    // 1. Đọc PDF → text
    public Task<CVParsedResult> ParseCVAsync(Stream pdfStream)
    {
        using var pdf = PdfDocument.Open(pdfStream);
        var text = string.Join(" ", pdf.GetPages().Select(p => p.Text));

        return Task.FromResult(new CVParsedResult
        {
            RawText = text,
            Name = ExtractName(text),
            Email = ExtractEmail(text),
            Phone = ExtractPhone(text),
            Skills = ExtractSkills(text),
        });
    }

    // 2. TF-IDF + Cosine Similarity
    public Task<ScreeningResult> ScreenCVAsync(string cvText, string jdText)
    {
        var cvTokens = Tokenize(cvText);
        var jdTokens = Tokenize(jdText);
        var vocab = cvTokens.Union(jdTokens).Distinct().ToList();

        var cvVec = ComputeTfIdf(cvTokens, vocab);
        var jdVec = ComputeTfIdf(jdTokens, vocab);

        double score = CosineSimilarity(cvVec, jdVec) * 100;
        var matched = jdTokens.Intersect(cvTokens).ToList();
        var missing = jdTokens.Except(cvTokens).ToList();

        return Task.FromResult(new ScreeningResult
        {
            MatchingScore = Math.Round((decimal)score, 2),
            KeywordsMatched = string.Join(", ", matched),
            KeywordsMissing = string.Join(", ", missing),
        });
    }

    // ── Helpers ───────────────────────────────────────────────────
    private static readonly HashSet<string> Stopwords = new()
    {
        "the","a","an","is","in","on","at","to","of","and","or","for",
        "with","as","by","from","that","this","are","was","be","have",
        "it","its","will","can","should","would","could","may",
    };

    private List<string> Tokenize(string text) =>
        text.ToLower()
            .Split(new[] { ' ', '\n', '\r', ',', '.', ';', ':', '(', ')', '/', '-', '\t' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !Stopwords.Contains(w))
            .ToList();

    private double[] ComputeTfIdf(List<string> tokens, List<string> vocab)
    {
        int total = tokens.Count == 0 ? 1 : tokens.Count;
        return vocab.Select(term =>
        {
            double tf = tokens.Count(t => t == term) / (double)total;
            double df = tokens.Contains(term) ? 1 : 0;
            double idf = df > 0 ? Math.Log(2.0 / df) : 0;
            return tf * idf;
        }).ToArray();
    }

    private static double CosineSimilarity(double[] a, double[] b)
    {
        double dot = a.Zip(b, (x, y) => x * y).Sum();
        double normA = Math.Sqrt(a.Sum(x => x * x));
        double normB = Math.Sqrt(b.Sum(x => x * x));
        return normA == 0 || normB == 0 ? 0 : dot / (normA * normB);
    }

    private static string ExtractEmail(string text)
    {
        var m = System.Text.RegularExpressions.Regex
            .Match(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        return m.Success ? m.Value : "";
    }

    private static string ExtractPhone(string text)
    {
        var m = System.Text.RegularExpressions.Regex
            .Match(text, @"(\+84|0)[0-9]{9,10}");
        return m.Success ? m.Value : "";
    }

    private static string ExtractName(string text) =>
        text.Split('\n').FirstOrDefault(l => l.Trim().Length > 2)?.Trim() ?? "";

    private static readonly string[] TechKeywords =
    {
        "c#","dotnet",".net","python","java","javascript","typescript",
        "react","angular","vue","sql","azure","docker","git","api",
        "html","css","ef core","linq","rest","microservice","spring",
        "nodejs","mongodb","postgresql","redis","kubernetes","ci/cd",
    };

    private static string ExtractSkills(string text)
    {
        var found = TechKeywords.Where(k => text.ToLower().Contains(k));
        return string.Join(", ", found);
    }
}