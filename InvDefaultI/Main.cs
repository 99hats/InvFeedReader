using UIKit;

namespace InvDefaultI
{
    public sealed class Program
    {
        static void Main(string[] args)
        {
            Inv.iOSShell.Run(InvDefault.Shell.Install);
        }
    }
}