using System;
using Microsoft.ML.Data;

namespace AIMS.BackendServer.Services.ML;

public class FeatureData
{
    // ML.NET requires LoadColumn attributes when loading into a POCO
    [LoadColumn(0)]
    public float SkillsMatchedCount { get; set; }

    [LoadColumn(1)]
    public float MissingSkillsCount { get; set; }

    [LoadColumn(2)]
    public float Gpa { get; set; }

    [LoadColumn(3)]
    public float YearsOfExperience { get; set; }

    [LoadColumn(4)]
    public float CosineSimilarity { get; set; }

    // Label for training. Use "true"/"false" in CSV or adapt preprocessing to convert 0/1 → true/false.
    [LoadColumn(5)]
    public bool Label { get; set; }
}
