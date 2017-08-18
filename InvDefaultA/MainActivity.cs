using Android.App;
using Android.Widget;
using Android.OS;
using Application = Inv.Application;

namespace InvDefaultA
{
    [Activity(Label = "InvDefaultA", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Inv.AndroidActivity
    {
        public override void Install(Application application)
        {
            InvDefault.Shell.Install(application);
        }
    }
}

