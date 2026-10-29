using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.MauiMtAdmob;

namespace The_Grocery_App
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? Saved_Instance_State)
        {
            base.OnCreate(Saved_Instance_State);

            CrossMauiMTAdmob.Current.Init(this, "ca-app-pub-3337756787254419~6164016845"); //Real Android App ID (matches AndroidManifest.xml)
        }
    }
}
