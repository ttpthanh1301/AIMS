using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AIMS.BackendServer.Services.ML
{
    public interface IFeatureExporter
    {
        Task AppendAsync(FeatureData feature, string csvPath);
    }

    public class FeatureExporter : IFeatureExporter
    {
        private static readonly string[] Headers = new[]
        {
            nameof(FeatureData.SkillsMatchedCount),
            nameof(FeatureData.MissingSkillsCount),
            nameof(FeatureData.Gpa),
            nameof(FeatureData.YearsOfExperience),
            nameof(FeatureData.CosineSimilarity),
            nameof(FeatureData.Label)
        };

        public async Task AppendAsync(FeatureData feature, string csvPath)
        {
            var dir = Path.GetDirectoryName(csvPath) ?? ".";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var exists = File.Exists(csvPath);
            await using var fs = new FileStream(csvPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            await using var sw = new StreamWriter(fs, Encoding.UTF8);

            if (!exists)
            {
                await sw.WriteLineAsync(string.Join(',', Headers));
            }

            // Write one row; use invariant culture for decimals
            var row = string.Join(',', new[]
            {
                feature.SkillsMatchedCount.ToString(CultureInfo.InvariantCulture),
                feature.MissingSkillsCount.ToString(CultureInfo.InvariantCulture),
                feature.Gpa.ToString(CultureInfo.InvariantCulture),
                feature.YearsOfExperience.ToString(CultureInfo.InvariantCulture),
                feature.CosineSimilarity.ToString(CultureInfo.InvariantCulture),
                (feature.Label ? "true" : "false")
            });

            await sw.WriteLineAsync(row);
            await sw.FlushAsync();
        }
    }
}
