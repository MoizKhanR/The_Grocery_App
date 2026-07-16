using Foundation;
using AppTrackingTransparency;
using UIKit;
using Mscc.GenerativeAI.Types;

namespace The_Grocery_App
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        private bool _att_Requested = false;

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication The_Application, NSDictionary? Launch_Options)
        {
            bool The_Result = base.FinishedLaunching(The_Application, Launch_Options);
            return The_Result;
        }

        public override void OnActivated(UIApplication The_Application)
        {
            base.OnActivated(The_Application);

            if (!_att_Requested && UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
            {
                _att_Requested = true;
                ATTrackingManager.RequestTrackingAuthorization(Status => { });
            }
        }
    }
}