using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIMS.BackendServer.Services.ML
{
    public class ScreeningAutoRetrainer : BackgroundService
    {
        private readonly IScreeningModelService _modelService;
        private readonly IConfiguration _config;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly ILogger<ScreeningAutoRetrainer> _logger;

        public ScreeningAutoRetrainer(IScreeningModelService modelService,
            IConfiguration config,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
            ILogger<ScreeningAutoRetrainer> logger)
        {
            _modelService = modelService;
            _config = config;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var section = _config.GetSection("ScreeningModel");
            var enabled = section.GetValue<bool>("AutoRetrainEnabled");
            if (!enabled)
            {
                _logger.LogInformation("ScreeningAutoRetrainer disabled by configuration.");
                return;
            }

            var intervalMinutes = section.GetValue<int>("AutoRetrainIntervalMinutes", 1440);
            var csvPath = section.GetValue<string>("AutoRetrainCsvPath") ?? Path.Combine(_env.ContentRootPath, "wwwroot", "datatest", "resume_features.csv");

            _logger.LogInformation("ScreeningAutoRetrainer started. interval={mins}min, csv={csv}", intervalMinutes, csvPath);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!System.IO.File.Exists(csvPath))
                    {
                        _logger.LogWarning("Auto retrain CSV not found: {csv}", csvPath);
                    }
                    else
                    {
                        var res = await _modelService.TrainAsync(csvPath);
                        _logger.LogInformation("Auto retrain completed: AUC={auc:F4}, F1={f1:F4}", res.Auc, res.F1Score);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto retrain failed");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
                }
                catch (TaskCanceledException) { break; }
            }
        }
    }
}
