using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inv;

namespace InvDefaultW
{
    public sealed class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            Inv.WpfShell.CheckRequirements(() =>
            {
#if DEBUG
                Inv.WpfShell.PreventDeviceEmulation = false;
                Inv.WpfShell.DeviceEmulation = Inv.WpfDeviceEmulation.iPad_Mini;
#endif
                Inv.WpfShell.FullScreenMode = false;
                Inv.WpfShell.DefaultWindowWidth = 1920;
                Inv.WpfShell.DefaultWindowHeight = 1080;
                Inv.WpfShell.Run(InvDefault.Shell.Install);
            });
        }
    }

    
}
