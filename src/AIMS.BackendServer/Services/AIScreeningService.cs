using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AIMS.BackendServer.Services;

public interface IAIScreeningService
{
    Task<CVParsedResult> ParseCVAsync(Stream pdfStream);
    Task<ScreeningResult> ScreenCVAsync(string cvText, string jdText);
}

public class AIScreeningService : IAIScreeningService
{
    // ── 1. Đọc PDF → text (thay PyPDF2) ──────────────────────
    public Task<CVParsedResult> ParseCVAsync(Stream pdfStream)
    {
        using var pdf = PdfDocument.Open(pdfStream);
        var text = string.Join(" ", pdf.GetPages()
                                       .Select(p => p.Text));

        return Task.FromResult(new CVParsedResult
        {
            RawText = text,
            Name = ExtractName(text),
            Email = ExtractEmail(text),
            Phone = ExtractPhone(text),
            Skills = ExtractSkills(text),
        });
    }

    // ── 2. TF-IDF + Cosine Similarity (thay sklearn) ─────────
    public Task<ScreeningResult> ScreenCVAsync(string cvText, string jdText)
    {
        var cvTokens = Tokenize(cvText);
        var jdTokens = Tokenize(jdText);

        // Toàn bộ từ vựng (vocabulary)
        var vocab = cvTokens.Union(jdTokens).Distinct().ToList();

        // Tính TF-IDF vector cho CV và JD
        var cvVector = ComputeTfIdf(cvTokens, vocab);
        var jdVector = ComputeTfIdf(jdTokens, vocab);

        // Cosine Similarity
        double score = CosineSimilarity(cvVector, jdVector) * 100;

        // Keywords phân tích
        var matched = jdTokens.Intersect(cvTokens).ToList();
        var missing = jdTokens.Except(cvTokens).ToList();

        return Task.FromResult(new ScreeningResult
        {
            MatchingScore = Math.Round((decimal)score, 2),
            KeywordsMatched = string.Join(", ", matched),
            KeywordsMissing = string.Join(", ", missing),
        });
    }

    // ── Helper methods ────────────────────────────────────────

    private List<string> Tokenize(string text)
    {
        var stopwords = new HashSet<string>
        {
            "the","a","an","is","in","on","at","to","of","and",
            "or","for","with","as","by","from","that","this","are","was"
        };

        return text.ToLower()
                   .Split(new[] { ' ', '\n', '\r', ',', '.', ';', ':', '(', ')' },
                          StringSplitOptions.RemoveEmptyEntries)
                   .Where(w => w.Length > 2 && !stopwords.Contains(w))
                   .ToList();
    }

    private double[] ComputeTfIdf(List<string> tokens, List<string> vocab)
    {
        int totalTerms = tokens.Count;

        return vocab.Select(term =>
        {
            // TF: tần suất xuất hiện trong document
            double tf = tokens.Count(t => t == term) / (double)totalTerms;

            // IDF: log(N / df) — ở đây N=2 documents
            double df = (tokens.Contains(term) ? 1 : 0);
            double idf = df > 0 ? Math.Log(2.0 / df) : 0;

            return tf * idf;
        }).ToArray();
    }

    private double CosineSimilarity(double[] vecA, double[] vecB)
    {
        double dot = vecA.Zip(vecB, (a, b) => a * b).Sum();
        double normA = Math.Sqrt(vecA.Sum(x => x * x));
        double normB = Math.Sqrt(vecB.Sum(x => x * x));
        return (normA == 0 || normB == 0) ? 0 : dot / (normA * normB);
    }

    private string ExtractEmail(string text)
    {
        var match = System.Text.RegularExpressions.Regex
            .Match(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        return match.Success ? match.Value : string.Empty;
    }

    private string ExtractPhone(string text)
    {
        var match = System.Text.RegularExpressions.Regex
            .Match(text, @"(\+84|0)[0-9]{9,10}");
        return match.Success ? match.Value : string.Empty;
    }

    private string ExtractName(string text) =>
        text.Split('\n').FirstOrDefault(l => l.Trim().Length > 2)?.Trim() ?? "";

    private string ExtractSkills(string text)
    {
        var keywords = new[]
        {
            "c#","dotnet",".net","python","java","javascript","typescript",
            "react","angular","vue","sql","azure","docker","git","api",
            "html","css","ef core","linq","rest","microservice"
        };
        var found = keywords.Where(k => text.ToLower().Contains(k));
        return string.Join(", ", found);
    }
}

// ── Response models ───────────────────────────────────────────────
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