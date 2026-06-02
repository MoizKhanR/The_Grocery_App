using Microsoft.Extensions.Logging;

namespace The_Grocery_App
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
#if ANDROID
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("CenterText", (handler, view) =>
            {
                handler.PlatformView.TextAlignment = Android.Views.TextAlignment.Center;
                handler.PlatformView.Gravity = Android.Views.GravityFlags.Center;
            });
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
