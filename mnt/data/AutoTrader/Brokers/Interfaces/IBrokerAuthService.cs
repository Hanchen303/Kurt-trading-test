namespace AutoTrader.Brokers.Interfaces
{
    public interface IBrokerAuthService
    {
        Task AuthenticateAsync();
        Task<string> GetAccessTokenAsync();
        Task<bool> RefreshTokenAsync(); // ✅ Fix this line
    }

}
