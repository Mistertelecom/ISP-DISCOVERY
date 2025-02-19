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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            using (Mutex mutex = new Mutex(true, "ISPDiscoveryByJpTools", out bool createdNew))
            {
                if (createdNew)
                {
                    try
                    {
                        Application.Run(new MainForm());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error starting application: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Another instance of ISP Discovery is already running.", "Application Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
