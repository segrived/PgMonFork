using System;
using System.Threading;
using System.Windows.Forms;

namespace PgMonFork
{
    internal static class Program
    {
        private const string appGuid = "6CA39404-B6BA-4984-8F8C-545A17D37AFB";

        [STAThread]
        private static void Main()
        {
            using (var mutex = new Mutex(false, "Global\\" + appGuid)) {
                if (! mutex.WaitOne(0, false)) {
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());
            }
        }
    }
}