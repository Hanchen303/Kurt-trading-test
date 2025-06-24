using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoTrader.Config;

namespace AutoTrader.ML
{
    public static class MLPipeline_Cycle
    {
        public static void RunTraining()
        {
            var mlContext = new MLContext();

            var dataPath = "TrainingData/Train/Cycle/data-cycle.csv";
            if (!File.Exists(dataPath))
            {
                Console.WriteLine($"❌ Training data not found at {dataPath}.");
                return;
            }

            var configText = File.ReadAllText("Configs/training-features.json");
            var featureConfig = JsonSerializer.Deserialize<TrainingFeatureConfig>(configText);
            int featureCount = featureConfig.Features.Count;

            var columns = new TextLoader.Column[featureCount + 1];
            for (int i = 0; i < featureCount; i++)
            {
                columns[i] = new TextLoader.Column($"f{i}", DataKind.Single, i);
            }
            columns[featureCount] = new TextLoader.Column("Label", DataKind.Boolean, featureCount);

            var loader = mlContext.Data.CreateTextLoader(
                columns: columns,
                hasHeader: true,
                separatorChar: ',');

            var data = loader.Load(dataPath);

            var featureColumnNames = columns.Take(featureCount).Select(c => c.Name).ToArray();
            var pipeline = mlContext.Transforms.Concatenate("FeaturesVector", featureColumnNames)
                .Append(mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: "Label", featureColumnName: "FeaturesVector"));

            var model = pipeline.Fit(data);

            Directory.CreateDirectory("MLModels");
            mlContext.Model.Save(model, data.Schema, "MLModels/CycleModel.zip");
            Console.WriteLine("✅ Cycle model trained and saved to MLModels/CycleModel.zip");

            // ARCHIVE MODEL
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm");
            Directory.CreateDirectory("ModelArchive");
            string archivePath = Path.Combine("ModelArchive", $"{timestamp}-CycleModel.zip");
            mlContext.Model.Save(model, data.Schema, archivePath);
            Console.WriteLine($"📦 Archived model to {archivePath}");
        }
    }
}
