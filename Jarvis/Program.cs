using System;
using System.Windows.Forms;

namespace Jarvis
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // TODO: Add my profile info to about
            // TODO: Move JsonFile to another project (MyCommon)
            try {
                var performanceMonitorExec = new PerformanceMonitorExec();
                performanceMonitorExec.Start();

                Application.Run();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message, "Error Message From Program.Main" );
            }
        }
    }
}
