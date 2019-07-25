using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Gtk;

public partial class MainWindow : Gtk.Window
{
    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();

        Button btn1 = new Button(Stock.About);
        Button btn2 = new Button("Cool button");
        Button btn3 = new Button(Stock.Close);
        btn3.Pressed += (obj, args) =>
        {
            Application.Quit();
        };
        Fixed fx1 = new Fixed();
        fx1.Put(btn1, 0, 0);
        fx1.Put(btn2, 0, 100);
        fx1.Put(btn3, 10, 300);
        Add(fx1);
        fx1.ShowAll();
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }
}
