using BLETest.Common;
using BLETest.Common.ComModel;
using MessagePack;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace BLETest.GattClientNative.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private const string DeviceIdKey = "auth_device_id";
        private const string ServerPubKeyKey = "auth_server_pubkey";
        private const string ClientPriKeyKey = "auth_client_prikey";
        private const string ClientPubKeyKey = "auth_client_pubkey";

        private AsymmetricCipherKeyPair? _clientKeyPair;

        public bool IsRegistered { get; private set; }
        public string? ServerDeviceId { get; private set; }
        public byte[]? ServerPublicKey { get; private set; }

        public AuthenticationService()
        {
            // 起動時に保存済み認証情報を読み込む
            _ = LoadCredentialsAsync();
        }

        public void SetServerInfo(string deviceId, byte[] publicKey)
        {
            ServerDeviceId = deviceId;
            ServerPublicKey = publicKey;
        }

        public Task<byte[]> CreateAuthenticationDataAsync()
        {
            if (string.IsNullOrEmpty(ServerDeviceId))
            {
                throw new InvalidOperationException("Server device ID is not set. Scan QR code first.");
            }

            // クライアント側の鍵ペアを生成
            _clientKeyPair = CryptoUtil.GenerateECDHKeyPair();
            var clientPublicKey = CryptoUtil.PubKeyToByte(_clientKeyPair.Public);

            // DeviceIdをバイト配列に変換
            var deviceIdGuid = Guid.Parse(ServerDeviceId);
            var deviceIdBytes = deviceIdGuid.ToByteArray();

            // DeviceIdに署名
            var signature = SignData(_clientKeyPair.Private, deviceIdBytes);

            // DeviceNewDataを作成
            var deviceNewData = new DeviceNewData
            {
                DeviceId = deviceIdGuid,
                MPubKey = clientPublicKey,
                DeviceIdSig = signature, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Id = 0
            };

            // MessagePackでシリアライズ
            var data = MessagePackSerializer.Serialize(deviceNewData);
            return Task.FromResult(data);
        }

        private byte[] SignData(AsymmetricKeyParameter privateKey, byte[] data)
        {
            //var signer = SignerUtilities.GetSigner("SHA256withECDSA");
            var signer = SignerUtilities.GetSigner(Constants.ECDH_CURVE_ALGORITHM);
            signer.Init(true, privateKey);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }

        public async Task<bool> SaveCredentialsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(ServerDeviceId) || ServerPublicKey == null || _clientKeyPair == null)
                {
                    return false;
                }

                // SecureStorageに保存
                await SecureStorage.SetAsync(DeviceIdKey, ServerDeviceId);
                await SecureStorage.SetAsync(ServerPubKeyKey, Convert.ToBase64String(ServerPublicKey));

                var clientPriKey = CryptoUtil.PriKeyToByte(_clientKeyPair.Private);
                var clientPubKey = CryptoUtil.PubKeyToByte(_clientKeyPair.Public);
                await SecureStorage.SetAsync(ClientPriKeyKey, Convert.ToBase64String(clientPriKey));
                await SecureStorage.SetAsync(ClientPubKeyKey, Convert.ToBase64String(clientPubKey));

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveCredentialsAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LoadCredentialsAsync()
        {
            try
            {
                var deviceId = await SecureStorage.GetAsync(DeviceIdKey);
                var serverPubKeyBase64 = await SecureStorage.GetAsync(ServerPubKeyKey);
                var clientPriKeyBase64 = await SecureStorage.GetAsync(ClientPriKeyKey);
                var clientPubKeyBase64 = await SecureStorage.GetAsync(ClientPubKeyKey);

                if (string.IsNullOrEmpty(deviceId) ||
                    string.IsNullOrEmpty(serverPubKeyBase64) ||
                    string.IsNullOrEmpty(clientPriKeyBase64) ||
                    string.IsNullOrEmpty(clientPubKeyBase64))
                {
                    IsRegistered = false;
                    return false;
                }

                ServerDeviceId = deviceId;
                ServerPublicKey = Convert.FromBase64String(serverPubKeyBase64);

                var clientPriKey = CryptoUtil.ByteToPriKey(Convert.FromBase64String(clientPriKeyBase64));
                var clientPubKey = CryptoUtil.ByteToPubKey(Convert.FromBase64String(clientPubKeyBase64));
                _clientKeyPair = new AsymmetricCipherKeyPair(clientPubKey, clientPriKey);

                IsRegistered = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCredentialsAsync error: {ex.Message}");
                IsRegistered = false;
                return false;
            }
        }

        public Task ClearCredentialsAsync()
        {
            SecureStorage.Remove(DeviceIdKey);
            SecureStorage.Remove(ServerPubKeyKey);
            SecureStorage.Remove(ClientPriKeyKey);
            SecureStorage.Remove(ClientPubKeyKey);

            ServerDeviceId = null;
            ServerPublicKey = null;
            _clientKeyPair = null;
            IsRegistered = false;

            return Task.CompletedTask;
        }
    }
}
