using Microsoft.Extensions.Logging;
using Plugin.MauiMtAdmob;
namespace The_Grocery_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMTAdmob()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Nunito-Regular.ttf", "NunitoRegular");
                    fonts.AddFont("Nunito-SemiBold.ttf", "NunitoSemiBold");
                    fonts.AddFont("Nunito-Bold.ttf", "NunitoBold");
                    fonts.AddFont("Nunito-ExtraBold.ttf", "NunitoExtraBold");
                });

            //Inputs sit inside rounded cards now, so drop the native underline/box
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
            });

            Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("Borderless", (handler, view) =>
            {
#if ANDROID
                var The_Context = handler.PlatformView.Context;
                int The_Plate_Id = The_Context?.Resources?.GetIdentifier("search_plate", "id", The_Context.PackageName) ?? 0;
                if (The_Plate_Id != 0)
                    handler.PlatformView.FindViewById(The_Plate_Id)?.SetBackgroundColor(Android.Graphics.Color.Transparent);
#elif IOS
                handler.PlatformView.SearchBarStyle = UIKit.UISearchBarStyle.Minimal;
#endif
            });

            // Enable debug logging via the global config property instead
            CrossMauiMTAdmob.Current.DebugMode = true;

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
