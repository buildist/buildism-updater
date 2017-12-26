using System.Threading;
using System.Windows.Forms;

namespace Updater
{
    class Program
    {
        private static Thread statusThread;
        private static UpdaterDialog d;
        public static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "server")
                Config.isServer = true;
            Application.EnableVisualStyles();
            statusThread = new Thread(new ThreadStart(UpdateStatus));
            statusThread.Start();
            Updater.Run();
        }
        private static void UpdateStatus()
        {
            while (true)
            {
                UpdaterStatus status = Updater.GetStatus();
                switch (status.status)
                {
                    case -1:
                        MessageBox.Show("An error occured while starting the game. Details on the problem have been saved to error.log.");
                        return;
                        break;
                    case 1:
                        if (d == null)
                        {
                            d = new UpdaterDialog();
                            d.Show();
                        }
                        d.progress.Value = (int)(100 * status.progress);
                        break;
                    case 2:
                        if (d != null)
                            d.Hide();
                        return;
                        break;
                }
                Thread.Sleep(100);
            }
        }
    }
}
