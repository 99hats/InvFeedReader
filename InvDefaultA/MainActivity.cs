using Android.App;
using Android.Widget;
using Android.OS;
using Application = Inv.Application;

namespace InvDefaultA
{
    [Activity(Label = "FeedRead", MainLauncher = true, Icon = "@drawable/icon", ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize),
        ]
    public class MainActivity : Inv.AndroidActivity
    {
        protected override void Install(Application application)
        {
            InvDefault.Shell.Install(application);
        }
    }
}

