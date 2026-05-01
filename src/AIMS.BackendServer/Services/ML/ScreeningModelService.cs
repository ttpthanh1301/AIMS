using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Trainers;
namespace AIMS.BackendServer.Services.ML;

public class ScreeningModelService : IScreeningModelService
{
    private readonly MLContext _mlContext;
    private readonly string _modelPath;
    private ITransformer? _model;
    private PredictionEngine<FeatureData, ScreeningPrediction>? _predictionEngine;
    private readonly ILogger<ScreeningModelService> _logger;

    public bool IsModelAvailable => _model != null;

    public ScreeningModelService(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
        ILogger<ScreeningModelService> logger)
    {
        _mlContext = new MLContext(seed: 0);
        _logger = logger;

        var modelDir = Path.Combine(env.ContentRootPath, "wwwroot", "ml");
        if (!Directory.Exists(modelDir))
            Directory.CreateDirectory(modelDir);

        _modelPath = Path.Combine(modelDir, "screening_model.zip");

        if (File.Exists(_modelPath))
        {
            try
            {
                LoadModel(_modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load existing screening model from {path}", _modelPath);
            }
        }
    }

    private void LoadModel(string path)
    {
        using var fs = File.OpenRead(path);
        _model = _mlContext.Model.Load(fs, out var schema);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<FeatureData, ScreeningPrediction>(_model);
        _logger.LogInformation("Loaded screening ML model from {path}", path);
    }

    public Task<ScreeningPrediction?> PredictAsync(FeatureData features)
    {
        if (!IsModelAvailable)
            return Task.FromResult<ScreeningPrediction?>(null);

        var pred = _predictionEngine!.Predict(features);
        return Task.FromResult<ScreeningPrediction?>(pred);
    }

    public Task<TrainResult> TrainAsync(string csvPath, string? modelOutputPath = null)
    {
        if (!File.Exists(csvPath))
            throw new FileNotFoundException("CSV not found", csvPath);

        modelOutputPath ??= _modelPath;

        // Load data
        var data = _mlContext.Data.LoadFromTextFile<FeatureData>(csvPath, hasHeader: true, separatorChar: ',');

        // Split: Optimize train/test ratio
        var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.15, seed: 42);

        // Pipeline with Feature Scaling + Enhanced Trainer
        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(FeatureData.SkillsMatchedCount),
                nameof(FeatureData.MissingSkillsCount),
                nameof(FeatureData.Gpa),
                nameof(FeatureData.YearsOfExperience),
                nameof(FeatureData.CosineSimilarity))
            // 🔧 ADD: Normalize features (MinMax scaling)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features", "Features"))
            // 🏆 BEST: LbfgsLogisticRegression - Tốt nhất cho dataset này
            .Append(_mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
                labelColumnName: nameof(FeatureData.Label),
                featureColumnName: "Features",
                l2Regularization: 0.01f));

        var model = pipeline.Fit(split.TrainSet);

        var preds = model.Transform(split.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(preds, labelColumnName: nameof(FeatureData.Label));

        // Save model
        using (var fs = File.Create(modelOutputPath))
        {
            _mlContext.Model.Save(model, split.TrainSet.Schema, fs);
        }

        // Load saved model into memory
        LoadModel(modelOutputPath);

        var result = new TrainResult
        {
            Accuracy = metrics.Accuracy,
            F1Score = metrics.F1Score,
            Auc = metrics.AreaUnderRocCurve,
            Message = "Trained and saved model."
        };

        _logger.LogInformation("⚡ TUNED Training: Accuracy={acc}, F1={f1}, AUC={auc}", result.Accuracy, result.F1Score, result.Auc);

        return Task.FromResult(result);
    }
}
