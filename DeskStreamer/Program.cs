using System;
using System.Threading.Tasks;
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
            ConsoleLogic.WriteConsole("1");
            NetworkLogic.GetIPVBoxRef(win);
            ConsoleLogic.WriteConsole("2");
            win.Show();
            ConsoleLogic.WriteConsole("3");
            new Task(NetworkLogic.Search).Start();
            Application.Run();
            
            
        }
    }
}
