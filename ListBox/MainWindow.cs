using System;
using System.Text;
using System.IO;
using System.Data;
using System.ComponentModel;
using System.Collections.Generic;
using Gtk;
using Gdk;
using GLib;
using System.Messaging;
using System.Drawing;
using System.Drawing.Printing;

//using System.Windows.Forms;
public partial class MainWindow: Gtk.Window
{	
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
		OnOpenBox ();
	}

	/// beginn global things
	/// 
	private string stringToPrint;
	public string eof = "\n";
	public string tab = "\t";
	public char newline = '\n';
	public char newtab = '\t';
	public string t;
	public string[] cc;
	public int GetColumnAt;
	public int rowcount;
	public int selColumn = 0;
	public TreePath selRow;
	public int columncount;
	public bool DocumentChanged;
	public string mr;
	public string mrow;
	public string[] header;
	public string myheader;
	public string[] newheader;
	public string myfilename;
	public CellRendererText mycell;
	public FileNotFoundException exc = new FileNotFoundException ();
	public TreeIter myiter;
	public string rmessage;
	// Create
	ListStore mylist;

	protected void InitListStore (int a)
	{
		var types = new System.Type[ a];
		for (int i = 0; i < a; ++i) {
			types [i] = typeof(string);
		}
		mylist = new ListStore (types);
	}

	///begin string for adding new row with 32 Columns
	void makeNewRow ()
	{
		t = "new row";
		for (int a  = 0; a < lb.Columns.Length; a++) {
			t = t + (new StringBuilder ().Append (" ,"));
		}
	}

	/// end global things

	/// init 
	protected void OnOpenBox ()
	{

		StatusIcon statusicon = StatusIcon.NewFromStock ("gtk-dnd");
		statusicon.Visible = true;
		this.ModifyFont (Pango.FontDescription.FromString ("DroidSans 10"));
		Gdk.Color bg = new Gdk.Color (233, 233, 233);
		this.ModifyBg (StateType.Normal,bg);
		this.toolbar1.ShowArrow = false;
		this.toolbar1.BorderWidth = 0;
		this.SetSizeRequest (700, 650);
		this.Title = "Listbox";
		this.Move (0, 0);
		this.SetIconFromFile ("./256_myLB.png");
		InitListStore (63);
		toolbar1.IconSize = Gtk.IconSize.SmallToolbar;
		applyHeader.StockId = "gtk-media-record";
		DocumentChanged = false;
		lb.EnableSearch = true;
		lb.EnableGridLines = Gtk.TreeViewGridLines.Both;
		lb.EnableTreeLines = true;
		lb.Selection.Mode = SelectionMode.Single;
		lb.RulesHint = true;
		lb.ButtonPressEvent += lbRightClick;
		lb.KeyPressEvent += lbKeyPressEvent;
		lb.ShowAll ();
		lb.CursorChanged += OnCursorChanged;
		lb.Selection.Changed += OnSelectionChanged;
		lb.HeadersClickable = true;
		lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 10"));
		this.toolbar1.Hide ();
		string mt = "Row 1" + tab;
		PopulateView (mt);

	}
	// beginn application quit
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{

		if (DocumentChanged == true) {
			MessageDialog md = new MessageDialog (this, 
			                                      DialogFlags.Modal,
			                                      Gtk.MessageType.Warning, 
			                                      ButtonsType.YesNo, 
			                                      "Save Changes ?");

			md.DefaultResponse = ResponseType.Yes;

			ResponseType result = (ResponseType)md.Run ();

			if (result == ResponseType.No) {
				md.Destroy ();
				Gtk.Application.Quit ();
			} else {
				md.Destroy ();
				if (myfilename == this.Title) {
					mySave (myfilename);
				} else {
					OnSaveFile (null, null);
				}

				Gtk.Application.Quit ();
			}
		} else {
			Gtk.Application.Quit ();
		}
	}
	// end application quit
	// begin save file
	protected void OnSaveFile (object sender, EventArgs e)
	{
		if (myfilename == this.Title) {
			mySave (myfilename);
		} else {
			// Do the save
			FileFilter filter = new FileFilter ();
			FileChooserDialog chooser;
			bool confirm = true;

			chooser = new FileChooserDialog (
				"Filename to save ...",
				this,
				FileChooserAction.Save,
				"Cancel", ResponseType.Cancel,
				"Save", ResponseType.Accept);

			string mf;
			mf = System.Environment.GetFolderPath (
				System.Environment.SpecialFolder.MyDocuments);
			myfilename = mf + "/Documents/CSV/";

			filter.Name = ("CSV files");
			filter.AddPattern ("*.csv");
			filter.AddPattern ("*.txt");
			filter.AddPattern ("*.tsv");

			chooser.AddFilter (filter);
			chooser.SetFilename ("Calendar");
			chooser.SetCurrentFolder (myfilename);
			chooser.DoOverwriteConfirmation = confirm;

			if (chooser.Run () == (int)ResponseType.Accept) {
				mySave (chooser.Filename);
			}
			chooser.Destroy ();
		}
	}
	// end save file

	// begin new file
	protected void NewFile (object sender, EventArgs e)
	{
		if (DocumentChanged == true) {
			MessageDialog md = new MessageDialog (this, 
			                                      DialogFlags.Modal,
			                                      Gtk.MessageType.Warning, 
			                                      ButtonsType.YesNo, 
			                                      "Save Changes ?");

			ResponseType result = (ResponseType)md.Run ();

			if (result == ResponseType.No) {
				md.Destroy ();

			} else {
				md.Destroy ();
				if (myfilename == this.Title) {
					mySave (myfilename);
				} else {
					OnSaveFile (null, null);
				}
								}
		} else {
		}
		onClearTreeView ();

		string mt = "new row" + tab;
		PopulateView (mt);

		DocumentChanged = false;
	}
	// end new file

	/// end open file
	private void onOpenFile (object sender, EventArgs e)
	{

		if (DocumentChanged == true) {
			MessageDialog md = new MessageDialog (this, 
			                                      DialogFlags.Modal,
			                                      Gtk.MessageType.Warning, 
			                                      ButtonsType.YesNo, 
			                                      "Save Changes ?");

			ResponseType result = (ResponseType)md.Run ();

			if (result == ResponseType.No) {
				md.Destroy ();

			} else {
				md.Destroy ();
				if (myfilename == this.Title) {
					mySave (myfilename);
				} else {
					OnSaveFile (null, null);
				}

				//				onClearTreeView ();
			}
		} else {
		}
		OpenFile ();
	}

	/// end open file


	/// remove selected row
	protected void RemoveSelectedRows (object sender, EventArgs e)
	{
		TreeSelection selection = lb.Selection;
		TreeModel model;
		TreeIter iter;
		if (selection.GetSelected (out model, out iter)) {
			model.GetPath (iter);

			if (rowcount >= 1) {
				mylist.Remove (ref iter);
				rowcount = rowcount - 1;
				DocumentChanged = true;
			}

			lb.Selection.SelectIter (iter);

		} else {
			return;
		}
	}

	/// end remove selected row

	// save existing file
	protected void mySave (string mfile)
	{
		// Open the file for writing.
		System.IO.StreamWriter sw = new System.IO.StreamWriter (mfile);
		string myresult = TreeViewToString ();
		sw.Write (myresult);
		this.Title = mfile;
		myfilename = mfile;
		sw.Close ();
		DocumentChanged = false;
	}
	// // first Row as Header
	protected void onChangeIcon (object sender, EventArgs e)
	{
		if (applyHeader.Active == true) {
			applyHeader.StockId = "gtk-yes";
		} else {
			applyHeader.StockId = "gtk-media-record";
		}
//		FirstRowHeader ();
	}

	/// begin select row
	//[GLib.ConnectBefore] 
	void OnCursorChanged (object obj, EventArgs e)
	{

		//Console.WriteLine ("Cursor Changed");
		int col = selColumn;
		TreeModel model = mylist;
		TreeIter iter;
		TreeViewColumn colPath;

		if (lb.Selection.GetSelected (out model, out iter)) {

			model.GetPath (iter);
			myiter = iter;
			selRow = model.GetPath (myiter);
			lb.GetCursor (out selRow, out colPath);

			for (col = 0; col < lb.Columns.Length; ++col) {
				if (lb.Columns [col] == colPath) {
					break;
				}
			}			
			//// get cell data
			selColumn = col;
		}
	}

	/// end select row

	/// begin CellEdit
	void OnCellEdit (object obj, EditedArgs args)
	{
		// Store data
		mylist.SetValue (myiter, selColumn, args.NewText);

		if (mylist != null) {
			mylist.SetValue (myiter, selColumn, args.NewText);
		}
		DocumentChanged = true;
		lb.ShowAll ();
	}

	/// end CellEdit

	void OnSelectionChanged (object obj, EventArgs e)
	{
//		return;
	}

	/// begin add Row
	protected void onAddRow (object sender, EventArgs e)
	{
		makeNewRow ();
		lb.GrabFocus ();
		mylist.AppendValues (t.Split (','));
		DocumentChanged = true;
		TreeIter iter;
		int n = mylist.IterNChildren () - 1;
	
		if (mylist.GetIterFromString (out iter, n.ToString ()))
				// select new row
			lb.Selection.SelectIter (iter);
		rowcount = rowcount + 1;	
	}

	/// end add Row

	/// begin messagebox
	protected void msgbox (string mymessage)
	{
		MessageDialog md = new MessageDialog (this,
		                                      DialogFlags.Modal,
		                                      Gtk.MessageType.Info, 
		                                      ButtonsType.Ok, 
		                                      mymessage);
		ResponseType result = (ResponseType)md.Run ();

		if (result == ResponseType.Ok) {
			md.Destroy ();
		}
	}

	/// end messagebox

	/// begin Table Example
	public DataTable BuildDataTableSample ()
	{
		DataTable table = new DataTable ();

		int maxColumns = 5;
		int maxRows = 6;

		for (int i = 0; i < maxColumns; i++) {
			string columnName = String.Format ("Column{0}", i);
			table.Columns.Add (columnName);
		}
		int c;
		int r;

		for (r = 0; r < maxRows; r++) {
			DataRow row = table.NewRow ();
			for (c = 0; c < table.Columns.Count; c++) {
				string cellValue = String.Format ("Row{0}, Column{1}", r, c);
				row [c] = cellValue;
			}
			table.Rows.Add (row);
		}

		return table;
	}

	/// end Table Example

	/// begin Convert Table into ListStore
	public static string ConvertDataTableToString (DataTable dataTable)
	{
		var output = new StringBuilder ();
		// Write Column Numbers
		foreach (DataRow row in dataTable.Rows) {
			for (int i = 0; i < dataTable.Columns.Count; i++) {
				var text = row [i].ToString ();
				output.Append (text + "\t");
			}
			output.Append ("\n");
		}
		string t = output.ToString ();
		return t;
	}

	/// end Convert Table into ListStore

	/// begin PopulateView
	protected void PopulateView (string ms)
	{
		//// clear Treeview
		foreach (TreeViewColumn col in lb.Columns) {
			lb.RemoveColumn (col);
			lb.Data.Remove (col);
		} 
		// clear ListStore
		mylist.Clear ();
		ms = ms.Replace ('\r', ' ');
		ms = ms.Replace (";", tab);

		ms = ms.TrimEnd (newline);

		// count lines and tabs in string, dividing with rowcount
		rowcount = (ms.Split (newline).Length - 1) + 1;
		columncount = ((ms.Split (newtab).Length - 1) / rowcount) + 1;

		byte[] bytes = Encoding.Default.GetBytes (ms);
		ms = Encoding.UTF8.GetString (bytes);

		header = ms.Split (newline);
		myheader = header [0];
		newheader = myheader.Split (newtab);

		string headertitle = string.Empty;

		//// make columns
		for (int a  = 0; a < columncount; a++) {
			TreeViewColumn mycolumn = new TreeViewColumn ();
			int v = (a);
			mycolumn.Title = "";
			/// header on or off
			if (applyHeader.Active == true) {
				headertitle = newheader [v];
			} else {
				headertitle = "Column " + (a);
			}
			/// end header on or off
			mycolumn.Title = headertitle;
			mycolumn.Alignment = 0.0f;
			mycolumn.Clickable = true;
			lb.AppendColumn (mycolumn);

			mycell = new CellRendererText ();
			mycell.Editable = true;
			mycell.Edited += this.OnCellEdit;

			mycolumn.PackStart (mycell, false);
			mycolumn.AddAttribute (mycell, "text", a);
		}

		string newStr = string.Empty;
		var lines = ms.Split (newline);
		int count = 0;
		foreach (var line in lines) {
			count = count + 1;
		}

		if (count > 1) {
			newStr = ms.Substring (0, ms.IndexOf (Environment.NewLine));
		}

		// remove first line if used as header
		if (applyHeader.Active == true) {
			ms = ms.Replace (newStr + eof, "");
		}
		//// read line by line
		foreach (string row in ms.Split(newline)) {
			string[] cell = string.Join (tab, row).Split (newtab);
			mylist.AppendValues (cell);
		}

		rowcount = rowcount - 1;
		DocumentChanged = false;
		//  select first row
		lb.Model = mylist;
		lb.GrabFocus ();
		TreeIter firstiter;
		if (mylist.GetIterFromString (out firstiter, "0"))
			lb.Selection.SelectIter (firstiter);

		selRow = mylist.GetPath (firstiter);

		// end select first row
	}

	/// end PopulateView

	/// begin Row reordered Event
	protected void onReordered (object o, DragEndArgs args)
	{
		DocumentChanged = true;
	}

	/// end Row reordered Event

	/// begin Node Selection Event
	protected void onNodeSelectionChanged (object o, EventArgs args)
	{
		ITreeNode node = lb.NodeSelection.SelectedNode;
	}

	/// end Node Selection Event

	/// end Node Selection Event
	/// 
	protected void onClearTreeView ()
	{
		//// begin clear Treeview
		foreach (TreeViewColumn col in lb.Columns) {
			lb.RemoveColumn (col);
		} 
		this.mylist.Clear ();
		myfilename = "";
		this.Title = "New Document";
		columncount = 0;
		rowcount = 0;
		// end clear Treeview
	}
	//begin row text to clipboard
	protected void onCopyRow (object sender, EventArgs e)
	{
		TreeSelection selection = lb.Selection;

		TreeModel model;
		TreeIter iter;
		string myresult;
		string msg = "";
		int a;

		if (selection.GetSelected (out model, out iter)) {
			model.GetPath (iter);
			for (a  = 0; a < lb.Columns.Length; a++) {
				// get Row Values
				msg = msg + (new StringBuilder ().Append 
				             (model.GetValue (iter, a) + tab));
			}
			// get row text tab delimited
			myresult = msg.ToString ();
			//Console.WriteLine (myresult);
			//remove last tab
			string newStr = myresult.Substring (0, myresult.LastIndexOf (newtab));
			myresult = myresult.Replace (newStr + eof, "");

			//copy text to clipboard
			Clipboard clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = myresult;
		}
	}
	//end copy row to clipboard
	//begin paste from clipboard
	protected void onPasteRow (object sender, EventArgs e)
	{
		TreeSelection selection = lb.Selection;

		TreeModel model;
		TreeIter iter;
		int a;

//		makeNewRow ();

		Gtk.Clipboard clippy = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
		var clip = clippy.WaitForText ();
		// remove last tab
		string newStr = clip.Substring (0, clip.LastIndexOf (newtab));
		clip = clip.Replace (newStr + eof, "").ToString ();

		if (selection.GetSelected (out model, out iter)) {
			model.GetPath (iter);
			for (a  = 0; a < lb.Columns.Length; a++) {
				mylist.SetValues (iter, clip.Split (newtab));
			}
		}
		DocumentChanged = true;
	}
	//end paste from clipboard
	protected void onCutRow (object o, EventArgs args)
	{
		onCopyRow (o, args);
		RemoveSelectedRows (o, args);
	}

	protected void onAddColumn (object o, EventArgs args)
	{		
		lb.GrabFocus ();
		TreeViewColumn newcolumn = new TreeViewColumn ();
		CellRendererText mcell = new CellRendererText ();

		newcolumn.Title = "Column " + (columncount).ToString ();
		newcolumn.Alignment = 0.0f;
		newcolumn.Clickable = true;

		lb.AppendColumn (newcolumn);

		mcell.Text = string.Empty;
		mcell.Editable = true;
		mcell.Visible = true;
		mcell.Edited += OnCellEdit;

		newcolumn.PackStart (mcell, false);
		newcolumn.SetAttributes (mcell, "text", columncount);

		TreeIter iter;
		mylist.GetIterFirst (out iter);
		DocumentChanged = true;

		// empty text into new column rows (to prevent crashing on edit text)
		for (int a  = 0; a < mylist.NColumns; a++) {
			mylist.SetValue (iter, columncount, string.Empty);
			mylist.IterNext (ref iter);
		}
		columncount = columncount + 1;
	}

	protected void OpenFile ()
	{
		// Do the open
		string mf;
		mf = System.Environment.GetFolderPath (
			System.Environment.SpecialFolder.MyDocuments);
		mf = mf + "/Documents/";

		FileFilter filter = new FileFilter ();
		FileChooserDialog chooser;

		chooser = new FileChooserDialog (
			"Open File ...",
			this,
			FileChooserAction.Open,
			"Cancel", ResponseType.Cancel,
			"Open", ResponseType.Accept);

		filter.Name = ("CSV files");
		filter.AddPattern ("*.csv");
		filter.AddPattern ("*.txt");
		filter.AddPattern ("*.tsv");

		chooser.AddFilter (filter);
		chooser.SetCurrentFolder (mf);

		if (chooser.Run () == (int)ResponseType.Accept) {

			// Open the file for writing.
			StreamReader file = File.OpenText (chooser.Filename);
			string ms = file.ReadToEnd ();
			ms = ms.Replace (';','\t');
			PopulateView (ms);
			lb.ShowAll ();
			myfilename = chooser.Filename.ToString ();
			this.Title = myfilename;
			file.Close ();
			DocumentChanged = false;
			chooser.Destroy ();
		} else {
			chooser.Destroy ();
		}
	}

	protected void onSaveAs (object sender, EventArgs e)
	{
		// Do the save
		FileFilter filter = new FileFilter ();
		FileChooserDialog chooser;
		bool confirm = true;

		chooser = new FileChooserDialog (
			"Filename to save ...",
			this,
			FileChooserAction.Save,
			"Cancel", ResponseType.Cancel,
			"Save", ResponseType.Accept);

		string mf;
		mf = System.Environment.GetFolderPath (
			System.Environment.SpecialFolder.MyDocuments);
		myfilename = mf + "/Documents/CSV";

		filter.Name = ("CSV files");
		filter.AddPattern ("*.csv");

		chooser.AddFilter (filter);
		chooser.SetFilename ("Calendar");
		chooser.SetCurrentFolder (myfilename);
		chooser.DoOverwriteConfirmation = confirm;

		if (chooser.Run () == (int)ResponseType.Accept) {
			mySave (chooser.Filename);
		}
		chooser.Destroy ();
	}

	protected void onTest (object sender, EventArgs e)
	{
		msgdialog ("OK");
		//Console.WriteLine (rmessage);
		mylist.SetValue (myiter, selColumn, rmessage);
	}

	protected void onRemoveLastColumn (object o, EventArgs args)
	{
		if (columncount > 0) {
			lb.RemoveColumn (lb.Columns [columncount - 1]);
			columncount = columncount - 1;
			DocumentChanged = true;
		}
	}

	protected void onRemoveSelColumn (object o, EventArgs args)
	{
		lb.RemoveColumn (lb.Columns [selColumn]);
		columncount = columncount - 1;
		DocumentChanged = true;
	}

	/// TreeView To String
	public string TreeViewToString ()
	{
		string line = string.Empty;
		string mstr = string.Empty;
		string all = string.Empty;
		string headers = string.Empty;
		int col;
		TreeModel model = lb.Model;
		TreeIter iter;
		model.GetIterFirst (out iter);
		// ListStore to string
		for (int i = 0; i < model.IterNChildren(); i++) {
			for (col = 0; col < lb.Columns.Length; ++col) {

				if (mylist.GetValue (iter, col).ToString () == "") {
					line = "" + ";";
				} else {
					line = (mylist.GetValue (iter, col).ToString () + ";");
				}

				mstr = (mstr + line);
			}		
			// add new line
			mstr = mstr.TrimEnd (';') + eof;

			// read next line
			model.IterNext (ref iter); 
		}
		// get Column Headers
		for (int i = 0; i < lb.Columns.Length; i++) {
			headers = headers + lb.GetColumn (i).Title + ";";
			headers = headers.TrimEnd (';');
		}
		// headers or not headers
		if (applyHeader.Active == false) {
			all = mstr;
		} else {
			all = headers + eof + mstr;
		}
		// remove last endofline
		return (all.TrimEnd (newline)).Replace (";", tab);
	}

	public void TestButton ()
	{

	}
	// right mouseclick
	[GLib.ConnectBefore] 
	protected void lbRightClick (object o, ButtonPressEventArgs args)
	{
		if (args.Event.Button == 3) {
			MakeContextMenu ();
		}
	}

	protected void onChangeFont (object sender, EventArgs e)
	{
		ChangeFont ();
	}
	// align center
	protected void onCenter (object sender, EventArgs e)
	{
		onColumnAlignment (0.5f);
	}
	// align left
	protected void onLeft (object sender, EventArgs e)
	{
		onColumnAlignment (0.0f);
	}
	// align right
	protected void onRight (object sender, EventArgs e)
	{
		onColumnAlignment (1.0f);
	}
	// Treeview Context Menu
	protected void MakeContextMenu ()
	{
		Menu m = new Menu ();
//
//		MenuItem empty = new MenuItem ("Menu");
//		m.Add (empty);

		m.Add (new SeparatorMenuItem ());

		MenuItem Medit = new MenuItem ("edit Text");
		Medit.ButtonReleaseEvent += new ButtonReleaseEventHandler 
			(onEditNow);
		m.Add (Medit);

		m.Add (new SeparatorMenuItem ());

		MenuItem Hedit = new MenuItem ("edit Header");
		Hedit.ButtonReleaseEvent += new ButtonReleaseEventHandler 
			(onEditHeaderNow);
		m.Add (Hedit);

		m.Add (new SeparatorMenuItem ());

		MenuItem first = new MenuItem ("First Row As Header");
		first.ButtonReleaseEvent += new ButtonReleaseEventHandler 
			(onMakeFirstRowHeader);
		m.Add (first);

		m.Add (new SeparatorMenuItem ());

		MenuItem deleteItem = new MenuItem ("Remove selected Row");
		deleteItem.RenderIcon ("gtk-cut", IconSize.Menu, "Löschen");
		deleteItem.ButtonReleaseEvent += new ButtonReleaseEventHandler
			(RemoveSelectedRows);
		m.Add (deleteItem);

		m.Add (new SeparatorMenuItem ());

		MenuItem addItem = new MenuItem ("Add Row");
		addItem.RenderIcon ("gtk-add", IconSize.Menu, "Hinzufügen");
		addItem.ButtonReleaseEvent += new ButtonReleaseEventHandler
			(onAddRow);
		m.Add (addItem);

		MenuItem addCol = new MenuItem ("Add Column");
		addCol.ButtonReleaseEvent += new ButtonReleaseEventHandler
			(onAddColumn);
		m.Add (addCol);

		m.Add (new SeparatorMenuItem ());

		MenuItem remselCol = new MenuItem ("Remove selected Column");
		remselCol.ButtonReleaseEvent += new ButtonReleaseEventHandler
			(onRemoveSelColumn);
		m.Add (remselCol);

		MenuItem remCol = new MenuItem ("Remove last Column");
		remCol.ButtonReleaseEvent += new ButtonReleaseEventHandler
			(onRemoveLastColumn);
		m.Add (remCol);

		m.Add (new SeparatorMenuItem ());


		// submenu 
		Menu align = new Menu ();

		MenuItem alignment = new MenuItem ("Alignment");
		alignment.Submenu = align;

		MenuItem alignL = new MenuItem ("Align Left");
		alignL.ButtonReleaseEvent += new ButtonReleaseEventHandler
			(onLeft);
		align.Append (alignL);

		MenuItem alignC = new MenuItem ("Align Center");
		alignC.ButtonReleaseEvent += new ButtonReleaseEventHandler
			(onCenter);
		align.Append (alignC);

		MenuItem alignR = new MenuItem ("Align Right");
		alignR.ButtonReleaseEvent += new ButtonReleaseEventHandler
			(onRight);
		align.Append (alignR);

		m.Append (alignment);
		// end submenu 1


		m.ShowAll ();

		m.Popup ();
	}
	// set Column Alignment for selected Column
	protected void onColumnAlignment (float align)
	{

		TreeModel model = lb.Model;
		TreeIter iter;
		model.GetIterFirst (out iter);

		lb.Columns [selColumn].Clear ();
		lb.Columns [selColumn].Alignment = align;
		CellRendererText cell = new CellRendererText ();

		for (int i = 0; i < model.IterNChildren(); i++) {

			lb.Columns [selColumn].ClearAttributes (cell);
			cell.Xalign = align;
			cell.Editable = true;
			cell.Edited += this.OnCellEdit;
			lb.Columns [selColumn].SetAttributes (cell,
			                                      "text",
			                                      selColumn);
			lb.Columns [selColumn].PackStart (cell, false);
			model.IterNext (ref iter);
		}
	}
	// Font 9 or 11
	protected void ChangeFont ()
	{
		if (selectFontAction.Active == true) {
			lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 11"));
		} else {
			lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 9"));
		}
	}

	[ConnectBefore]
	void lbKeyPressEvent (object o, KeyPressEventArgs args)
	{
		//Console.WriteLine ("KeyValue: " + args.Event.KeyValue);
		if (args.Event.KeyValue == 65535)
		{
			RemoveSelectedRows (o, args);
		}
	}

	protected void onInfo (object sender, EventArgs e)
	{
		msgbox ("ListBox" + eof + eof + "Axel Schneider" + eof 
			+ "© 2016" + eof + eof 
			+ "made with Mono on Linux");
	}

	/// dialog for edit Cell text
	protected void msgdialog (string mymessage)
	{
		Dialog md = new Dialog ("set new Cell Text",
		                        this,
		                        DialogFlags.Modal,
		                        mymessage
		);

		md.AddButton ("Cancel", ResponseType.Cancel);
		md.AddButton ("OK", ResponseType.Ok).GrabDefault ();
		//.CanDefault = true;

		md.SetSizeRequest (250, 100);
		Entry ml = new Entry ();
		ml.SetSizeRequest (80, 24);
		ml.ActivatesDefault = true;

//		ml.Activated += void onCloseDialog (md);

		string mline = mylist.GetValue (myiter, selColumn).ToString ();
		if (mline != string.Empty) {
			ml.Text = mline;	
		}

		md.VBox.Add (ml);
		ml.Show ();


		ResponseType result = (ResponseType)md.Run ();

		if (result == ResponseType.Ok) {
			rmessage = ml.Text;
			mylist.SetValue (myiter, selColumn, rmessage);
			DocumentChanged = true;
			md.Destroy ();
		} else {
			md.Destroy ();
		}
	}

	/// end messagebox

	// Dialog for edit Header Title
	protected void headerDialog (string mymessage)
	{
		Dialog md = new Dialog ("set new Header Title",
		                        this,
		                        DialogFlags.Modal,
		                        mymessage);

		md.AddButton ("Cancel", ResponseType.Cancel);
		md.AddButton ("OK", ResponseType.Ok).GrabDefault ();
		md.SetSizeRequest (250, 100);
		Entry ml = new Entry ();
		ml.SetSizeRequest (80, 24);
		ml.ActivatesDefault = true;
		ml.Text = lb.Columns [selColumn].Title;

		ml.Show ();
		md.VBox.Add (ml);

		ResponseType result = (ResponseType)md.Run ();

		if (result == ResponseType.Ok) {
			rmessage = ml.Text;
			lb.Columns [selColumn].Title = rmessage;
			applyHeader.Active = true;
			DocumentChanged = true;
			md.Destroy ();
		} else {
			md.Destroy ();
		}
	}
	// edit Cell text
	protected void onEditNow (object sender, EventArgs e)
	{
		msgdialog ("OK");

	}
	// edit Header title
	protected void onEditHeaderNow (object sender, EventArgs e)
	{
		headerDialog ("OK");

	}
	// make Header Title from first Row and delete first row
	protected void  onMakeFirstRowHeader (object sender, EventArgs e)
	{
		TreeModel model = lb.Model;
		TreeIter iter;
		model.GetIterFirst (out iter);


		for (int col = 0; col < lb.Columns.Length; ++col) {
			lb.Columns [col].Title = mylist.GetValue (iter, col).ToString ();
			//lb.Columns [col].Resizable = true;
			applyHeader.Active = true;
		}
		mylist.Remove (ref iter);
		rowcount = rowcount - 1;
	}

	protected void onPrinting (object sender, EventArgs e)
	{
		stringToPrint = string.Empty;
		stringToPrint = TreeViewToString ()
			.ToString ().Replace (tab, "    ").Replace (eof, eof + eof);

		PrintDocument doc = new PrintDocument ();
		doc.DocumentName = "Liste";


		var printFont = new System.Drawing.Font ("Arial", 9);

		//prev.RenderPage (0);

		doc.PrintPage += (s, ev) => {
			ev.Graphics.DrawString (stringToPrint,
			                        printFont, 
			                        Brushes.Black, 
			                        ev.MarginBounds.Left,
			                        ev.MarginBounds.Top);
			//ev.HasMorePages = false;
		};
		doc.Print ();
		Console.Write (stringToPrint);
	}


	protected void onQuit (object sender, EventArgs e)
	{
		Application.Quit ();
	}

	protected void onSize_nine (object sender, EventArgs e)
	{
		lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 9"));
	}

	protected void onSize_ten (object sender, EventArgs e)
	{
		lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 10"));
	}


	protected void onSize_eleven (object sender, EventArgs e)
	{
		lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 11"));
	}


	protected void onSize_twelve (object sender, EventArgs e)
	{
		lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 12"));
	}


	protected void onSize_thirteen (object sender, EventArgs e)
	{
		lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 13"));
	}
		

	protected void onSize_fourteen (object sender, EventArgs e)
	{
		lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 14"));
	}
	protected void onSize_eighteen (object sender, EventArgs e)
	{
		lb.ModifyFont (Pango.FontDescription.FromString ("DroidSans 18"));
	}
		
	protected void onHideToolbar (object sender, EventArgs e)
	{
		if (this.toolbar1.Visible == true) {
			this.toolbar1.Hide ();
		} else {
			this.toolbar1.Show ();
		}
	}
	protected void onClearListbox (object sender, EventArgs e)
	{
		mylist.Clear();
		DocumentChanged = true;
	}
		
	protected void onHeaderClicked (object sender, EventArgs e)
	{
		mylist.SetSortColumnId (selColumn, 0);
	}
}
