
using System.Collections.Generic;

namespace AutoTrader.Config
{
    public class TrainingConfig
    {
        public List<string> FeatureSet { get; set; }
        public LabelingConfig Labeling { get; set; }
        public ModelConfig Model { get; set; }
    }

    public class LabelingConfig
    {
        public string Type { get; set; }
        public int LookaheadMinutes { get; set; }
    }

    public class ModelConfig
    {
        public string Type { get; set; }
        public bool CrossValidation { get; set; }
        public string TargetMetric { get; set; }
    }
}
