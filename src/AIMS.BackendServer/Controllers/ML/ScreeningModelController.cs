using System;
using System.IO;
using System.Threading.Tasks;
using AIMS.BackendServer.Services.ML;
using AIMS.BackendServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AIMS.BackendServer.Controllers.ML
{
    [ApiController]
    [Route("api/admin/screening")]
    public class ScreeningModelController : ControllerBase
    {
        private readonly IScreeningModelService _modelService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly ILogger<ScreeningModelController> _logger;
        private readonly ICsvNormalizer _csvNormalizer;

        public ScreeningModelController(IScreeningModelService modelService,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
            ILogger<ScreeningModelController> logger,
            ICsvNormalizer csvNormalizer)
        {
            _modelService = modelService;
            _env = env;
            _logger = logger;
            _csvNormalizer = csvNormalizer;
        }

        [HttpPost("train")]
        public async Task<IActionResult> Train([FromBody] TrainRequest? req)
        {
            // Default CSV path (project includes datatest folder under wwwroot)
            var defaultCsv = Path.Combine(_env.ContentRootPath, "wwwroot", "datatest", "resume_dataset_200k_enhanced (1).csv");
            var csv = string.IsNullOrWhiteSpace(req?.CsvPath) ? defaultCsv : req.CsvPath;

            if (!System.IO.File.Exists(csv))
            {
                return BadRequest(new { error = "CSV file not found", path = csv });
            }

            try
            {
                var result = await _modelService.TrainAsync(csv);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Training failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new { modelLoaded = _modelService.IsModelAvailable });
        }

        [HttpPost("export")]
        public async Task<IActionResult> Export([FromBody] ExportRequest? req,
            [FromServices] IAIScreeningService ais)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.CvText) || string.IsNullOrWhiteSpace(req.JobDescription))
                return BadRequest(new { error = "cvText and jobDescription are required" });

            var feature = await ais.ExtractFeaturesAsync(req.CvText, req.JobDescription, req.CandidateGpa);
            // Set label if provided
            if (req.Label.HasValue)
                feature.Label = req.Label.Value;

            var defaultCsv = Path.Combine(_env.ContentRootPath, "wwwroot", "datatest", "resume_features.csv");
            var csv = string.IsNullOrWhiteSpace(req.CsvPath) ? defaultCsv : req.CsvPath;

            try
            {
                // Use DI resolved exporter
                var exporter = HttpContext.RequestServices.GetService(typeof(AIMS.BackendServer.Services.ML.IFeatureExporter))
                    as AIMS.BackendServer.Services.ML.IFeatureExporter;
                if (exporter is null)
                    return StatusCode(500, new { error = "Feature exporter not available" });

                await exporter.AppendAsync(feature, csv);
                return Ok(new { path = csv });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("normalize-csv")]
        public async Task<IActionResult> NormalizeCsv([FromBody] NormalizeRequest? req)
        {
            var defaultInput = Path.Combine(_env.ContentRootPath, "wwwroot", "datatest", "resume_dataset_200k_enhanced (1).csv");
            var input = string.IsNullOrWhiteSpace(req?.InputPath) ? defaultInput : req.InputPath!;
            if (!System.IO.File.Exists(input))
                return BadRequest(new { error = "Input CSV not found", path = input });

            var defaultOutput = Path.Combine(_env.ContentRootPath, "wwwroot", "datatest", "resume_dataset_200k_enhanced_normalized.csv");
            var output = string.IsNullOrWhiteSpace(req?.OutputPath) ? defaultOutput : req.OutputPath!;

            try
            {
                await _csvNormalizer.NormalizeAsync(input, output,
                    req?.LabelColumn ?? "hired",
                    req?.GpaColumn ?? "cgpa",
                    req?.GpaScaleColumn ?? "gpa_scale",
                    req?.OutputLabelColumn ?? "Label",
                    req?.OutputGpa4Column ?? "gpa_4");

                return Ok(new { path = output });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV normalization failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("convert-to-features")]
        public async Task<IActionResult> ConvertToFeatures([FromBody] ConvertFeaturesRequest? req)
        {
            var defaultInput = Path.Combine(_env.ContentRootPath, "wwwroot", "datatest", "resume_dataset_200k_enhanced (1).csv");
            var input = string.IsNullOrWhiteSpace(req?.InputPath) ? defaultInput : req.InputPath!;
            if (!System.IO.File.Exists(input))
                return BadRequest(new { error = "Input CSV not found", path = input });

            var defaultOutput = Path.Combine(_env.ContentRootPath, "wwwroot", "datatest", "resume_features.csv");
            var output = string.IsNullOrWhiteSpace(req?.OutputPath) ? defaultOutput : req.OutputPath!;

            try
            {
                var conv = HttpContext.RequestServices.GetService(typeof(AIMS.BackendServer.Services.ML.IFeatureCsvConverter))
                    as AIMS.BackendServer.Services.ML.IFeatureCsvConverter;
                if (conv is null)
                    return StatusCode(500, new { error = "Feature converter not available" });

                await conv.ConvertToFeatureCsvAsync(input, output,
                    req?.GpaColumn ?? "cgpa",
                    req?.SkillsScoreColumn ?? "skills_score",
                    req?.ExperienceColumn ?? "experience_years",
                    req?.LabelColumn ?? "hired");

                if (req?.TrainImmediately == true)
                {
                    if (!System.IO.File.Exists(output))
                        return StatusCode(500, new { error = "Feature CSV not produced" });

                    var result = await _modelService.TrainAsync(output);
                    return Ok(new { path = output, train = result });
                }

                return Ok(new { path = output });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Convert to features failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class TrainRequest
    {
        public string? CsvPath { get; set; }
    }

    public class ExportRequest
    {
        public string? CsvPath { get; set; }
        public string? CvText { get; set; }
        public string? JobDescription { get; set; }
        public bool? Label { get; set; }
        public decimal? CandidateGpa { get; set; }
    }

    public class NormalizeRequest
    {
        public string? InputPath { get; set; }
        public string? OutputPath { get; set; }
        public string? LabelColumn { get; set; }
        public string? GpaColumn { get; set; }
        public string? GpaScaleColumn { get; set; }
        public string? OutputLabelColumn { get; set; }
        public string? OutputGpa4Column { get; set; }
    }

    public class ConvertFeaturesRequest
    {
        public string? InputPath { get; set; }
        public string? OutputPath { get; set; }
        public string? GpaColumn { get; set; }
        public string? SkillsScoreColumn { get; set; }
        public string? ExperienceColumn { get; set; }
        public string? LabelColumn { get; set; }
        public bool TrainImmediately { get; set; }
    }
}
