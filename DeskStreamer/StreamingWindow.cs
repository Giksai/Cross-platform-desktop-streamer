using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;

public partial class StreamingWindow : Window
{
    public Image img;
    public StreamingWindow() : base(WindowType.Toplevel)
    {
        img = new Image("default.jpg");
        Add(img);
        ShowAll();
        
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        a.RetVal = true;
    }
}

