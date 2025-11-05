namespace ConfidentialBox.Core.Entities;

public class AIModel
{
    public int Id { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public string ModelType { get; set; } = string.Empty; // AnomalyDetection, FileThreatClassifier, BehaviorAnalysis

    public string ModelVersion { get; set; } = string.Empty;

    public DateTime TrainedAt { get; set; } = DateTime.UtcNow;

    public int TrainingSamples { get; set; }

    public double Accuracy { get; set; }

    public double Precision { get; set; }

    public double Recall { get; set; }

    public bool IsActive { get; set; } = true;

    public string ModelPath { get; set; } = string.Empty;

    public string Metadata { get; set; } = string.Empty; // JSON
}