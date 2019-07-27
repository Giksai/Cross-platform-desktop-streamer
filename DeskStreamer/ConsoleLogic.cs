using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Gtk;

namespace DeskStreamer
{
    static class ConsoleLogic
    {
        public static MainWindow main;

        public static void WriteConsole(string text, Exception e = null)
        {
            try
            {
                if (e != null)
                    main.rightSide.Add(new Label("Error on " +
                        DateTime.Now.ToShortDateString() + ": " +
                        e.Message + "\n" + e.StackTrace + "\n" + text));
                else
                    main.rightSide.Add(new Label(text));

                main.ShowAll();
            }
            catch
            {

            }
            
        }

        public static void SendMainWindowRef(MainWindow mainRef)
        {
            main = mainRef;
        }

    }
}
