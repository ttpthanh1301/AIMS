using Microsoft.ML.Data;

namespace AIMS.BackendServer.Services.ML;

public class ScreeningPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }
    public float Score { get; set; }
}
