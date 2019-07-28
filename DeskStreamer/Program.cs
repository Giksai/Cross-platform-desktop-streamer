using System;
using System.Threading.Tasks;
using System.Threading;
using Gtk;

namespace DeskStreamer
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Application.Init();
            MainWindow win = new MainWindow();
            ConsoleLogic.SendMainWindowRef(win);
            NetworkLogic.GetIPVBoxRef(win);
            win.Show();
            NetworkLogic.Search();
            NetworkLogic.Listen();
            Thread thr1 =  new Thread(NetworkLogic.ConnectionCheckLoop);
            thr1.Start();
            Application.Run();
            
            
        }
    }
}
