namespace AutoTrader.Config
{
    public class AppSettings
    {
        public string Broker { get; set; }
        public QuestradeConfig Questrade { get; set; }
        public MoomooConfig Moomoo { get; set; }
    }

    public class QuestradeConfig
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string ApiServer { get; set; }
        public bool IsPractice { get; set; }
    }

    public class MoomooConfig
    {
        public string AppKey { get; set; }
        public string AppSecret { get; set; }
        public string AccessToken { get; set; }
        public string Region { get; set; }
        public string AccountId { get; set; }
    }
}
