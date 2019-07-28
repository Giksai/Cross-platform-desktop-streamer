using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Gtk;

public partial class MainWindow : Gtk.Window
{
    public VBox rightSide = new VBox(false, 1);
    public VBox ipVBox = new VBox(true, 5);     //Left side
    public Label currIP = new Label("0.0.0.0"); //Right side
    public Label dataAmount = new Label("-");   //Right side
    public Image connectionStatus = new Image("default.jpg"); //right side

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();
        ModifyBg(StateType.Normal, new Gdk.Color(50, 50, 50));
        //Design
        VBox allContent = new VBox(false, 2);
        //Alignment hAllign = new Alignment(0, 0, 0, 0);
        //hBox.PackStart(hAllign, false, false, 1);
        connectionStatus.SetSizeRequest(50, 50);

        rightSide.Add(currIP);
        rightSide.Add(dataAmount);
        rightSide.Add(connectionStatus);
    
        HBox mainBox = new HBox(true, 2);
        mainBox.Add(ipVBox);
        mainBox.Add(rightSide);

        MenuBar menuBar = new MenuBar();
        Menu fileMenu = new Menu();
        MenuItem file = new MenuItem("File");
        file.Submenu = fileMenu;
        MenuItem exit = new MenuItem("Exit");
        exit.Activated += OnExitEvent;
        fileMenu.Append(exit);
        menuBar.Append(file);

        allContent.PackStart(menuBar, false, false, 0);
        allContent.PackEnd(mainBox, false, false, 0);

        Add(allContent);
        
        ShowAll();
        
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Environment.Exit(0);
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnExitEvent(object sender, EventArgs args)
    {

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