using System.Threading.Tasks;

namespace AIMS.BackendServer.Services.ML;

public interface IScreeningModelService
{
    bool IsModelAvailable { get; }

    /// <summary>
    /// Predict using loaded model. Returns null if model not available.
    /// </summary>
    Task<ScreeningPrediction?> PredictAsync(FeatureData features);

    /// <summary>
    /// Train model from CSV and save to default model path (or provided path).
    /// CSV should have headers that match <see cref="FeatureData"/> property names.
    /// </summary>
    Task<TrainResult> TrainAsync(string csvPath, string? modelOutputPath = null);
}

public class TrainResult
{
    public double Accuracy { get; set; }
    public double F1Score { get; set; }
    public double Auc { get; set; }
    public string Message { get; set; } = string.Empty;
}
