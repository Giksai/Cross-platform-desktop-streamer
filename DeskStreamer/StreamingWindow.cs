using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;

public partial class StreamingWindow : Window
{
    public Image img = new Image();
    public StreamingWindow() : base(WindowType.Toplevel)
    {
        Add(img);
        ShowAll();
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        a.RetVal = true;
    }
}

