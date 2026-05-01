using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace AIMS.BackendServer.Services.ML
{
    public interface IFeatureCsvConverter
    {
        /// <summary>
        /// Convert a generic enhanced resume CSV into the FeatureData CSV expected by trainer.
        /// Default mapping assumes columns: cgpa, skills_score, experience_years, hired
        /// </summary>
        Task ConvertToFeatureCsvAsync(string inputPath, string outputPath,
            string gpaColumn = "cgpa",
            string skillsScoreColumn = "skills_score",
            string experienceColumn = "experience_years",
            string labelColumn = "hired");
    }

    public class FeatureCsvConverter : IFeatureCsvConverter
    {
        private readonly ILogger<FeatureCsvConverter> _logger;

        public FeatureCsvConverter(ILogger<FeatureCsvConverter> logger)
        {
            _logger = logger;
        }

        public async Task ConvertToFeatureCsvAsync(string inputPath, string outputPath,
            string gpaColumn = "cgpa",
            string skillsScoreColumn = "skills_score",
            string experienceColumn = "experience_years",
            string labelColumn = "hired")
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Input CSV not found", inputPath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null,
                MissingFieldFound = null,
                IgnoreBlankLines = true,
            };

            // First pass: find max skills score to normalize cosine-like feature
            double maxSkills = 0.0;
            using (var r = new StreamReader(inputPath, Encoding.UTF8))
            using (var reader = new CsvReader(r, config))
            {
                await reader.ReadAsync();
                reader.ReadHeader();
                var headers = reader.HeaderRecord ?? Array.Empty<string>();
                while (await reader.ReadAsync())
                {
                    if (headers.Contains(skillsScoreColumn, StringComparer.OrdinalIgnoreCase))
                    {
                        var raw = reader.GetField(skillsScoreColumn);
                        if (double.TryParse(raw?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                            maxSkills = Math.Max(maxSkills, Math.Abs(val));
                    }
                }
            }

            if (maxSkills <= 0) maxSkills = 1.0;

            // Second pass: write features
            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);

            var outputHeaders = new[] { "SkillsMatchedCount", "MissingSkillsCount", "Gpa", "YearsOfExperience", "CosineSimilarity", "Label" };
            foreach (var h in outputHeaders) csvWriter.WriteField(h);
            await csvWriter.NextRecordAsync();

            using (var r = new StreamReader(inputPath, Encoding.UTF8))
            using (var reader = new CsvReader(r, config))
            {
                await reader.ReadAsync();
                reader.ReadHeader();
                var headers = reader.HeaderRecord ?? Array.Empty<string>();
                while (await reader.ReadAsync())
                {
                    double skillsScore = 0.0;
                    double gpaVal = double.NaN;
                    double years = 0.0;
                    bool label = false;

                    if (headers.Contains(skillsScoreColumn, StringComparer.OrdinalIgnoreCase))
                        double.TryParse(reader.GetField(skillsScoreColumn)?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out skillsScore);

                    if (headers.Contains(gpaColumn, StringComparer.OrdinalIgnoreCase))
                    {
                        double.TryParse(reader.GetField(gpaColumn)?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var rawGpa);
                        // assume cgpa is on scale 10 if >4
                        if (rawGpa > 4.0) gpaVal = Math.Clamp(rawGpa / 10.0 * 4.0, 0.0, 4.0);
                        else gpaVal = Math.Clamp(rawGpa, 0.0, 4.0);
                    }

                    if (headers.Contains(experienceColumn, StringComparer.OrdinalIgnoreCase))
                        double.TryParse(reader.GetField(experienceColumn)?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out years);

                    if (headers.Contains(labelColumn, StringComparer.OrdinalIgnoreCase))
                    {
                        var raw = reader.GetField(labelColumn)?.Trim().ToLowerInvariant();
                        label = raw == "1" || raw == "1.0" || raw == "true" || raw == "yes";
                    }

                    var skillsMatched = (float)skillsScore;
                    var missingSkills = 0f;
                    var cosine = (float)Math.Clamp(skillsScore / maxSkills, 0.0, 1.0);
                    var gpa = double.IsNaN(gpaVal) ? 0f : (float)gpaVal;
                    var yearsF = (float)years;

                    csvWriter.WriteField(skillsMatched.ToString(CultureInfo.InvariantCulture));
                    csvWriter.WriteField(missingSkills.ToString(CultureInfo.InvariantCulture));
                    csvWriter.WriteField(gpa.ToString(CultureInfo.InvariantCulture));
                    csvWriter.WriteField(yearsF.ToString(CultureInfo.InvariantCulture));
                    csvWriter.WriteField(cosine.ToString(CultureInfo.InvariantCulture));
                    csvWriter.WriteField(label ? "true" : "false");
                    await csvWriter.NextRecordAsync();
                }
            }
        }
    }
}
