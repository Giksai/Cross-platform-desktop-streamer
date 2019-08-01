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
            NetworkLogic.GetIPVBoxRef(win);
            ConsoleLogic.SendLabelRef(win.consoleTxt);
            ConsoleLogic.SendMainWindowRef(win);
            win.Show();
            Application.Run();
            
            
        }
    }
}
