using System;
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
            win.Show();
            Application.Run();
        }
    }
}
