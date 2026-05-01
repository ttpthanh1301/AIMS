using System;
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
    public interface ICsvNormalizer
    {
        Task NormalizeAsync(string inputPath, string outputPath,
            string labelColumn = "label_suitable",
            string gpaColumn = "gpa",
            string gpaScaleColumn = "gpa_scale",
            string outputLabelColumn = "Label",
            string outputGpa4Column = "gpa_4");
    }

    public class CsvNormalizer : ICsvNormalizer
    {
        private readonly ILogger<CsvNormalizer> _logger;

        public CsvNormalizer(ILogger<CsvNormalizer> logger)
        {
            _logger = logger;
        }

        public async Task NormalizeAsync(string inputPath, string outputPath,
            string labelColumn = "label_suitable",
            string gpaColumn = "gpa",
            string gpaScaleColumn = "gpa_scale",
            string outputLabelColumn = "Label",
            string outputGpa4Column = "gpa_4")
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Input CSV not found", inputPath);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null,
                MissingFieldFound = null,
                DetectColumnCountChanges = false,
                IgnoreBlankLines = true,
            };

            using var reader = new StreamReader(inputPath, Encoding.UTF8);
            using var csv = new CsvReader(reader, config);
            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord?.ToList() ?? new System.Collections.Generic.List<string>();

            var outputHeaders = headers.ToList();
            if (!outputHeaders.Contains(outputLabelColumn, StringComparer.OrdinalIgnoreCase))
                outputHeaders.Add(outputLabelColumn);
            if (!outputHeaders.Contains(outputGpa4Column, StringComparer.OrdinalIgnoreCase))
                outputHeaders.Add(outputGpa4Column);

            // Ensure output directory
            var outDir = Path.GetDirectoryName(outputPath) ?? ".";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            foreach (var h in outputHeaders) csvWriter.WriteField(h);
            await csvWriter.NextRecordAsync();

            while (await csv.ReadAsync())
            {
                var record = outputHeaders.ToDictionary(h => h, h => string.Empty, StringComparer.OrdinalIgnoreCase);

                // copy existing fields
                foreach (var h in headers)
                {
                    try
                    {
                        record[h] = csv.GetField(h) ?? string.Empty;
                    }
                    catch
                    {
                        record[h] = string.Empty;
                    }
                }

                // normalize label
                if (headers.Any(h => string.Equals(h, labelColumn, StringComparison.OrdinalIgnoreCase)))
                {
                    var raw = record[labelColumn];
                    var trimmed = (raw ?? string.Empty).Trim().ToLowerInvariant();
                    string normalizedLabel;
                    if (trimmed == "1" || trimmed == "1.0" || trimmed == "true" || trimmed == "yes")
                        normalizedLabel = "true";
                    else if (trimmed == "0" || trimmed == "0.0" || trimmed == "false" || trimmed == "no" || string.IsNullOrWhiteSpace(trimmed))
                        normalizedLabel = "false";
                    else
                        normalizedLabel = trimmed; // preserve if already true/false

                    record[outputLabelColumn] = normalizedLabel;
                }

                // normalize GPA -> gpa_4
                double? gpa4 = null;
                if (headers.Any(h => string.Equals(h, gpaColumn, StringComparison.OrdinalIgnoreCase)))
                {
                    var rawGpa = record[gpaColumn] ?? string.Empty;
                    var norm = rawGpa.Replace(',', '.').Trim();
                    if (double.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out var gpaVal))
                    {
                        // detect scale
                        bool isScale10 = false;
                        if (headers.Any(h => string.Equals(h, gpaScaleColumn, StringComparison.OrdinalIgnoreCase)))
                        {
                            var scale = (record.ContainsKey(gpaScaleColumn) ? (record[gpaScaleColumn] ?? string.Empty) : string.Empty).Trim();
                            if (scale == "10" || scale == "10.0") isScale10 = true;
                        }

                        // heuristic: if value > 4, assume scale 10
                        if (!isScale10 && gpaVal > 4.0) isScale10 = true;

                        if (isScale10)
                            gpa4 = Math.Clamp(gpaVal / 10.0 * 4.0, 0.0, 4.0);
                        else
                            gpa4 = Math.Clamp(gpaVal, 0.0, 4.0);
                    }
                }

                if (gpa4.HasValue)
                    record[outputGpa4Column] = gpa4.Value.ToString(CultureInfo.InvariantCulture);
                else
                    record[outputGpa4Column] = string.Empty;

                // write row
                foreach (var h in outputHeaders)
                    csvWriter.WriteField(record.TryGetValue(h, out var v) ? v : string.Empty);

                await csvWriter.NextRecordAsync();
                await csvWriter.FlushAsync();
            }
        }
    }
}
