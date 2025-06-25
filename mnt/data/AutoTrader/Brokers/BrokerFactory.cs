using System;
using AutoTrader.Brokers.Interfaces;
using AutoTrader.Brokers.Questrade;
using AutoTrader.Config;

namespace AutoTrader.Brokers
{
    public static class BrokerFactory
    {
        public static (IBrokerMarketService MarketService,IBrokerAuthService AuthService) CreateBroker(AppSettings settings)
        {
            switch (settings.Broker)
            {
                case "Questrade":
                    var qAuth = new QuestradeAuthService(settings.Questrade);
                    qAuth.AuthenticateAsync().Wait(); // block here for now — or use async outside if possible

                    var qMarket = new QuestradeMarketService(
                        qAuth.AccessToken,
                        qAuth.ApiServer
                    );

                    return (qMarket, qAuth);

                case "Moomoo":
                    throw new NotImplementedException("Moomoo broker is not yet implemented.");

                default:
                    throw new Exception($"❌ Unsupported broker specified: {settings.Broker}");
            }
        }
    }
}
