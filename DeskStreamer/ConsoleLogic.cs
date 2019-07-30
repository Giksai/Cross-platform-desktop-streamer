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
        private static Label txt;

        public static void WriteConsole(string text, Exception e = null)
        {
            try
            {
                //lock(main)
                //{
                //    if (e != null)
                //        main.rightSide.Add(new Label("Error on " +
                //            DateTime.Now.ToShortDateString() + ": " +
                //            e.Message + "\n" + e.StackTrace + "\n" + text));
                //    else
                //        main.rightSide.Add(new Label(text));

                //    main.ShowAll();
                //}
                lock (main)
                {
                    if (e != null)
                        txt.Text += "Error! " + text + '\n' + e.Message
                            + '\n' + e.StackTrace + '\n';
                    else
                        txt.Text += text + '\n';

                    main.ShowAll();
                }

            }
            catch
            {

            }
            
        }

        public static void SendMainWindowRef(MainWindow mainRef)
        {
            main = mainRef;
        }
        public static void SendLabelRef(Label txtRef)
        {
            txt = txtRef;
        }

    }
}
