
# AutoTrader: Fully Dynamic ML-Integrated Trading System

## System Overview

AutoTrader is a fully modular, self-learning, hybrid algorithmic trading system that integrates rule-based trading strategies with machine learning models to dynamically adapt to market conditions.

---

## Directory Structure

### Core Application

- Program.cs — Main control center

### Questrade API

- Questrade/Authentication/AuthManager.cs — OAuth authentication
- Questrade/Account/AccountService.cs — Account info
- Questrade/Market/MarketService.cs — Market data loader
- Questrade/Market/CandleLoader.cs — Local candle file reader

### Analytics (Indicators)

- Analytics/Indicators.cs — Full indicator computation engine

### Configs

- Config/TradingConfig.cs — Strategy thresholds and risk config
- Config/TrainingFeatureConfig.cs — Dynamic ML feature selection

### Trading Logic (Strategy Layer)

- Strategy/BreakoutStrategy_ML.cs — ML-enhanced breakout strategy
- Strategy/CycleStrategy_ML.cs — ML-enhanced cycle strategy
- Strategy/StrategyCoordinator.cs — Coordinates final signals
- Strategy/RiskManager.cs — Position sizing, stop & target
- Strategy/TradePlan.cs — Trade planning object

### ML Training & Inference

- ML/TradeSample.cs — ML.NET input schema
- ML/MLPipeline_Cycle.cs — Cycle model training pipeline
- ML/MLPipeline_Breakout.cs — Breakout model training pipeline
- ML/DualDataGenerator.cs — Labeled dataset generator

### Full Retraining Pipeline

- ML/HistoricalDownloader.cs — Historical raw candle downloader
- ML/HistoricalSignalSimulator.cs — Generate historical trade plans
- ML/RetrainingPipeline.cs — Fully automate retraining workflow

### Config Files

- Configs/trading-config.json — Risk management config
- Configs/training-features.json — Dynamic feature selector

### Data Storage

- TrainingData/Raw/ — Raw downloaded candles (organized by ticker/date)
- TrainingData/Cycle/data-cycle.csv — Labeled Cycle dataset
- TrainingData/Breakout/data-breakout.csv — Labeled Breakout dataset
- MLModels/CycleModel.zip — Trained Cycle ML model
- MLModels/BreakoutModel.zip — Trained Breakout ML model

---

## Main Modes (Program.cs)

| Mode | Description |
|------|-------------|
| download | Download historical raw candles for multiple tickers |
| train-cycle | Train Cycle strategy ML model |
| train-breakout | Train Breakout strategy ML model |
| retrain | Full pipeline: download, simulate signals, label data, train models |
| trade | Evaluate live signals based on latest ML models |

---

## System Flow Summary

1️⃣ Download raw historical data via HistoricalDownloader  
2️⃣ Simulate strategy signals via HistoricalSignalSimulator  
3️⃣ Generate labeled data via DualDataGenerator  
4️⃣ Train ML models via MLPipeline_Cycle and MLPipeline_Breakout  
5️⃣ Live Trading: Rule-based strategy → ML confirmation → Risk Manager → Trade Plan

---

## Key Benefits

- Fully dynamic ML feature control
- Clean separation of rule logic vs machine learning
- Completely automated retraining pipeline
- Adaptive self-learning while retaining strategy interpretability

---

## Next Upgrade Possibilities

- Expand to full raw pattern ML detection (no rule-based pre-filter)
- Add deep learning model option (CNN, LSTM for time-series)
- Add backtesting engine
- Full execution integration with live brokerage

---


