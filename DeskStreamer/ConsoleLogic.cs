using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Gtk;

namespace DeskStreamer
{
    static class ConsoleLogic
    {
        public static MainWindow main;
        private static Label txt;
        private static Dictionary<string, int> printedMessages = new Dictionary<string, int>();
        private static string lastMessage;

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
                lock(main)
                {
                    if(printedMessages.ContainsKey(text))
                    {
                        printedMessages[text]++;
                        if(lastMessage == text)
                        {
                            if (e != null)
                                txt.Text = "Error! " + text + " - " + printedMessages[text] + '\n' + e.Message
                                    + '\n' + e.StackTrace + '\n';
                            else
                                txt.Text = text + " - " + printedMessages[text] + '\n';
                        }
                        else
                        {
                            if (e != null)
                                txt.Text += "Error! " + text + " - " + printedMessages[text] + '\n' + e.Message
                                    + '\n' + e.StackTrace + '\n';
                            else
                                txt.Text += text + " - " + printedMessages[text] + '\n';
                            lastMessage = text;
                        }
                        
                        
                    }
                    else
                    {
                        printedMessages.Add(text, 1);
                        if (e != null)
                            txt.Text += "Error! " + text + " - " + printedMessages[text] + '\n' + e.Message
                                + '\n' + e.StackTrace + '\n';
                        else
                            txt.Text += text + " - " + printedMessages[text] + '\n';
                        lastMessage = text;
                        Thread.Sleep(5);
                    }
                    
                }

            }
            catch(Exception e2)
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
