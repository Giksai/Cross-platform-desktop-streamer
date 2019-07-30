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
            Application.Run();
            
            
        }
    }
}
