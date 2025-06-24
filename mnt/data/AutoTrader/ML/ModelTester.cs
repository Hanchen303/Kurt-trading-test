using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoTrader.Config;

namespace AutoTrader.ML
{
    public static class ModelTester
    {
        public static void RunFullModelTest()
        {
            Console.WriteLine("🚀 Starting full out-of-sample model test...");

            // Load dynamic feature config
            var configText = File.ReadAllText("Configs/training-features.json");
            var featureConfig = JsonSerializer.Deserialize<TrainingFeatureConfig>(configText);
            int featureCount = featureConfig.Features.Count;

            // Build schema
            var columns = new TextLoader.Column[featureCount + 1];
            for (int i = 0; i < featureCount; i++)
            {
                columns[i] = new TextLoader.Column($"f{i}", DataKind.Single, i);
            }
            columns[featureCount] = new TextLoader.Column("Label", DataKind.Boolean, featureCount);

            var mlContext = new MLContext();
            var textLoader = mlContext.Data.CreateTextLoader(
                columns: columns,
                hasHeader: true,
                separatorChar: ',');

            // Load Cycle Model
            var cycleModel = mlContext.Model.Load("MLModels/CycleModel.zip", out _);
            var cycleData = textLoader.Load("TrainingData/Test/Cycle/data-cycle.csv");
            EvaluateModel(mlContext, cycleModel, cycleData, "Cycle");

            // Load Breakout Model
            var breakoutModel = mlContext.Model.Load("MLModels/BreakoutModel.zip", out _);
            var breakoutData = textLoader.Load("TrainingData/Test/Breakout/data-breakout.csv");
            EvaluateModel(mlContext, breakoutModel, breakoutData, "Breakout");

            Console.WriteLine("✅ Model testing completed.");
        }

        private static void EvaluateModel(MLContext mlContext, ITransformer model, IDataView data, string strategyName)
        {
            var predictions = model.Transform(data);
            var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

            Console.WriteLine($"📊 {strategyName} Model Evaluation:");
            Console.WriteLine($"  Accuracy: {metrics.Accuracy:P2}");
            Console.WriteLine($"  AUC:      {metrics.AreaUnderRocCurve:P2}");
            Console.WriteLine($"  F1 Score: {metrics.F1Score:P2}");
            Console.WriteLine($"  Precision: {metrics.PositivePrecision:P2}");
            Console.WriteLine($"  Recall: {metrics.PositiveRecall:P2}");
            Console.WriteLine("----------------------------------");
        }
    }
}
