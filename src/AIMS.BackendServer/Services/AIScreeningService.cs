using AIMS.BackendServer.Data.Entities;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using System.Text.RegularExpressions;

namespace AIMS.BackendServer.Services;

// ── Interface ──────────────────────────────────────────────────
public interface IAIScreeningService
{
    Task<CVParsedData> ParseCVAsync(string filePath, int applicationId);
    Task<AIScreeningResult> ScreenCVAsync(int applicationId, string requiredSkills);
}

// ── Implementation ─────────────────────────────────────────────
public class AIScreeningService : IAIScreeningService
{
    // ── Stopwords tiếng Anh phổ biến ──────────────────────────
    private static readonly HashSet<string> Stopwords = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "a","an","the","and","or","but","in","on","at","to","for",
        "of","with","by","from","is","are","was","were","be","been",
        "have","has","had","do","does","did","will","would","could",
        "should","may","might","shall","can","this","that","these",
        "those","i","me","my","we","our","you","your","he","his",
        "she","her","it","its","they","their","what","which","who",
        "when","where","how","all","each","every","both","few","more",
        "most","other","some","such","no","not","only","same","so",
        "than","too","very","just","about","above","after","before",
    };

    // ── Tech keywords quan trọng ───────────────────────────────
    private static readonly HashSet<string> TechKeywords = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "csharp","c#","dotnet",".net","net","aspnet","asp.net",
        "python","java","javascript","typescript","react","angular","vue",
        "sql","mysql","postgresql","mongodb","redis","elasticsearch",
        "docker","kubernetes","azure","aws","gcp","git","github","gitlab",
        "html","css","bootstrap","jquery","nodejs","express",
        "jwt","oauth","rest","api","microservices","mvc",
        "entity","framework","linq","ef","efcore",
        "machine","learning","ml","ai","nlp","tensorflow","pytorch",
        "agile","scrum","devops","cicd","jenkins","linux","ubuntu",
        "oop","solid","design","patterns","clean","architecture",
    };

    // ─────────────────────────────────────────────────────────
    // ParseCVAsync — Bóc tách văn bản từ CV (PDF/DOCX)
    // ─────────────────────────────────────────────────────────
    public async Task<CVParsedData> ParseCVAsync(
        string filePath, int applicationId)
    {
        var rawText = await ExtractTextAsync(filePath);

        var parsed = new CVParsedData
        {
            ApplicationId = applicationId,
            RawText = rawText,
            FullName = ExtractName(rawText),
            EmailExtracted = ExtractEmail(rawText),
            PhoneExtracted = ExtractPhone(rawText),
            SkillsExtracted = ExtractSkills(rawText),
            EducationExtracted = ExtractSection(rawText,
                new[] { "education", "academic", "university", "degree" }),
            ExperienceExtracted = ExtractSection(rawText,
                new[] { "experience", "work", "project", "internship" }),
            ParsedAt = DateTime.UtcNow,
        };

        return parsed;
    }

    // ─────────────────────────────────────────────────────────
    // ScreenCVAsync — TF-IDF + Cosine Similarity
    // ─────────────────────────────────────────────────────────
    public async Task<AIScreeningResult> ScreenCVAsync(
        int applicationId,
        string requiredSkills)
    {
        await Task.CompletedTask; // Để async signature nhất quán

        // Lấy CV text từ CVParsedData (truyền vào qua overload khác)
        // → Xem ScreeningController gọi service này

        throw new NotImplementedException(
            "Gọi ScreenCVAsync(cvText, requiredSkills, applicationId)");
    }

    // ── Overload thực tế được dùng ─────────────────────────────
    public Task<AIScreeningResult> ScreenCVAsync(
        string cvText,
        string requiredSkills,
        int applicationId)
    {
        // ── Bước 1: Tokenize + Normalize ──────────────────────
        var cvTokens = Tokenize(cvText);
        var jdTokens = Tokenize(requiredSkills);

        // ── Bước 2: Lấy vocabulary chung ─────────────────────
        var vocabulary = cvTokens.Union(jdTokens).Distinct().ToList();

        // ── Bước 3: Tính TF-IDF vectors ──────────────────────
        var cvVector = ComputeTfIdf(cvTokens, vocabulary,
            new[] { cvTokens, jdTokens });
        var jdVector = ComputeTfIdf(jdTokens, vocabulary,
            new[] { cvTokens, jdTokens });

        // ── Bước 4: Cosine Similarity ─────────────────────────
        var score = CosineSimilarity(cvVector, jdVector);
        var matchingScore = Math.Round(score * 100, 2); // Đổi sang %

        // ── Bước 5: Phân tích keywords ────────────────────────
        var jdKeywords = jdTokens
            .Where(t => TechKeywords.Contains(t))
            .Distinct()
            .ToList();

        var cvKeywords = cvTokens
            .Where(t => TechKeywords.Contains(t))
            .Distinct()
            .ToList();

        var matched = jdKeywords
            .Intersect(cvKeywords, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missing = jdKeywords
            .Except(cvKeywords, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new AIScreeningResult
        {
            ApplicationId = applicationId,
            MatchingScore = (decimal)matchingScore,
            KeywordsMatched = string.Join(", ", matched),
            KeywordsMissing = string.Join(", ", missing),
            ScreenedAt = DateTime.UtcNow,
        };

        return Task.FromResult(result);
    }

    // ─────────────────────────────────────────────────────────
    // HELPER: Đọc text từ PDF hoặc DOCX
    // ─────────────────────────────────────────────────────────
    private static async Task<string> ExtractTextAsync(string filePath)
    {
        await Task.CompletedTask;

        var ext = Path.GetExtension(filePath).ToLower();
        var sb = new StringBuilder();

        if (ext == ".pdf")
        {
            // Dùng PdfPig đọc PDF
            using var pdf = PdfDocument.Open(filePath);
            foreach (var page in pdf.GetPages())
                sb.AppendLine(page.Text);
        }
        else if (ext == ".docx")
        {
            // Dùng DocumentFormat.OpenXml đọc DOCX
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body != null)
                sb.Append(body.InnerText);
        }
        else if (ext == ".doc")
        {
            // .doc cũ — đọc raw bytes tìm text
            var bytes = await File.ReadAllBytesAsync(filePath);
            var text = Encoding.UTF8.GetString(bytes)
                .Replace("\0", " ")
                .Replace("\r\n", " ");
            sb.Append(text);
        }

        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────
    // HELPER: Tokenize — lowercase, remove stopwords, stem
    // ─────────────────────────────────────────────────────────
    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Normalize: lowercase, giữ chữ cái và số
        var normalized = text.ToLower();

        // Tách tokens bằng regex
        var tokens = Regex.Split(normalized, @"[^a-z0-9#\.\+]+")
            .Where(t => t.Length > 1)               // Bỏ ký tự đơn
            .Where(t => !Stopwords.Contains(t))     // Bỏ stopwords
            .ToList();

        return tokens;
    }

    // ─────────────────────────────────────────────────────────
    // HELPER: Tính TF-IDF vector
    // TF = tần suất từ / tổng số từ trong doc
    // IDF = log(N / df) — N = số docs, df = số docs chứa từ
    // ─────────────────────────────────────────────────────────
    private static double[] ComputeTfIdf(
        List<string> tokens,
        List<string> vocabulary,
        List<string>[] allDocTokens)
    {
        int N = allDocTokens.Length;
        double total = tokens.Count;
        var vector = new double[vocabulary.Count];

        for (int i = 0; i < vocabulary.Count; i++)
        {
            var term = vocabulary[i];

            // TF
            var tf = tokens.Count(t => t == term) / (total + 1e-10);

            // DF = số documents chứa term
            var df = allDocTokens.Count(doc => doc.Contains(term));

            // IDF = log(N / (df + 1)) — smoothing tránh chia 0
            var idf = Math.Log((double)N / (df + 1) + 1);

            vector[i] = tf * idf;
        }

        return vector;
    }

    // ─────────────────────────────────────────────────────────
    // HELPER: Cosine Similarity = (A·B) / (|A| × |B|)
    // ─────────────────────────────────────────────────────────
    private static double CosineSimilarity(double[] a, double[] b)
    {
        double dot = 0, normA = 0, normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator < 1e-10 ? 0 : dot / denominator;
    }

    // ─────────────────────────────────────────────────────────
    // HELPER: Trích xuất Email bằng Regex
    // ─────────────────────────────────────────────────────────
    private static string? ExtractEmail(string text)
    {
        var match = Regex.Match(text,
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        return match.Success ? match.Value : null;
    }

    // ─────────────────────────────────────────────────────────
    // HELPER: Trích xuất Phone bằng Regex
    // ─────────────────────────────────────────────────────────
    private static string? ExtractPhone(string text)
    {
        var match = Regex.Match(text,
            @"(\+84|0)[0-9]{9,10}");
        return match.Success ? match.Value : null;
    }

    // ─────────────────────────────────────────────────────────
    // HELPER: Trích xuất tên (dòng đầu tiên thường là tên)
    // ─────────────────────────────────────────────────────────
    private static string? ExtractName(string text)
    {
        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 2 && l.Length < 50)
            .Take(5)
            .ToList();

        // Tên thường là dòng đầu không chứa số hoặc email
        return lines.FirstOrDefault(l =>
            !Regex.IsMatch(l, @"[\d@]") &&
            Regex.IsMatch(l, @"^[A-Za-zÀ-ỹ\s]+$"));
    }

    // ─────────────────────────────────────────────────────────
    // HELPER: Trích xuất tech skills từ CV
    // ─────────────────────────────────────────────────────────
    private static string ExtractSkills(string text)
    {
        var tokens = Tokenize(text);
        var skills = tokens
            .Where(t => TechKeywords.Contains(t))
            .Distinct()
            .ToList();

        return string.Join(", ", skills);
    }

    // ─────────────────────────────────────────────────────────
    // HELPER: Trích xuất section theo keywords tiêu đề
    // ─────────────────────────────────────────────────────────
    private static string? ExtractSection(
        string text,
        string[] sectionKeywords)
    {
        var lines = text.Split('\n').ToList();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i].ToLower().Trim();
            if (sectionKeywords.Any(k => line.Contains(k)))
            {
                // Lấy 10 dòng tiếp theo
                var section = lines
                    .Skip(i + 1)
                    .Take(10)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                return string.Join(" | ", section);
            }
        }

        return null;
    }
}