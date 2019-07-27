using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Gtk;

public partial class MainWindow : Gtk.Window
{
    public VBox rightSide = new VBox(false, 1);
    public VBox ipVBox = new VBox(true, 5);

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();
        ModifyBg(StateType.Normal, new Gdk.Color(50, 50, 50));
        //Design
        VBox ipVBox = new VBox(true, 5);
        Label searchLabel = new Label("searching...");
        searchLabel.SetAlignment(0.5f, 1);
        ipVBox.Add(searchLabel);


        Button btn1 = new Button("Settings");
        btn1.SetSizeRequest(100, 10);
        HBox buttonsRow = new HBox(true, 1);
        //Alignment hAllign = new Alignment(0, 0, 0, 0);
        buttonsRow.Add(btn1);
        //hBox.PackStart(hAllign, false, false, 1);
        rightSide.Add(buttonsRow);
    
        HBox mainBox = new HBox(true, 2);
        mainBox.Add(ipVBox);
        mainBox.Add(rightSide);

        Add(mainBox);
        
        ShowAll();
        
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    
}

//Мусорка кода
//Table table = new Table(2, 2, false);
//table.Attach(ipVBox, 0, 1, 0, 1);
//table.Attach(hBox, 1, 2, 0, 1);
//table.Attach(new Label(), 0, 2, 2, 0);
//table.Attach(consoleBox, 0, 2, 0, 2);
//VBox vBox = new VBox(false, 5);
//vBox.PackStart(new Entry(), false, false, 0);
//vBox.PackEnd(table, true, true, 0);

//Fixed fx1 = new Fixed();
//fx1.Put(btn1, 0, 0);
//fx1.Put(btn2, 0, 100);
//fx1.Put(btn3, 10, 300);
//Add(fx1);
//fx1.ShowAll();