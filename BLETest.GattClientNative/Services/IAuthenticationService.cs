namespace BLETest.GattClientNative.Services
{
    public interface IAuthenticationService
    {
        bool IsRegistered { get; }
        string? ServerDeviceId { get; }
        byte[]? ServerPublicKey { get; }

        void SetServerInfo(string deviceId, byte[] publicKey);
        Task<byte[]> CreateAuthenticationDataAsync();
        Task<bool> SaveCredentialsAsync();
        Task<bool> LoadCredentialsAsync();
        Task ClearCredentialsAsync();
    }
}
