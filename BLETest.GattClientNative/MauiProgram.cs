using Microsoft.Extensions.Logging;
using BLETest.GattClientNative.Services;
using BLETest.Common;
using ZXing.Net.Maui.Controls;

namespace BLETest.GattClientNative
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // BLEサービスをDIコンテナに登録
#if ANDROID
            builder.Services.AddSingleton<IBleService>(
                new Platforms.Android.BleService(
                    BLESettings.ServiceId,
                    BLESettings.WriteCharacteristic,
                    BLESettings.NotifyCharacteristic,
                    BLESettings.AuthCharacteristicWrite,
                    BLESettings.AuthCharacteristicRead));
#endif

            // 認証サービスを登録
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<QRScanPage>();

            return builder.Build();
        }
    }
}
