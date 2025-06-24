using Microsoft.ML.Data;

namespace AutoTrader.ML
{
    public class TradeSample
    {
        [VectorType]
        public float[] Features { get; set; }

        [LoadColumn(-1)]
        public bool Label { get; set; }
    }
}
