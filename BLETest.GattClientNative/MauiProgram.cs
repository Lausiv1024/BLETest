using Microsoft.Extensions.Logging;
using BLETest.GattClientNative.Services;
using BLETest.Settings;

namespace BLETest.GattClientNative
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
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
                    BLESettings.NotifyCharacteristic));
#endif

            builder.Services.AddSingleton<MainPage>();

            return builder.Build();
        }
    }
}
