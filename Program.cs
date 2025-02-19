using System;
using System.Windows.Forms;
using System.Threading;

namespace NetworkDiscovery
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            AppConfig.Load();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            
            using (Mutex mutex = new Mutex(true, "ISPDiscoveryByJpTools", out bool createdNew))
            {
                if (createdNew) Application.Run(new MainForm());
                else MessageBox.Show("Another instance is already running.");
            }
        }
    }
}