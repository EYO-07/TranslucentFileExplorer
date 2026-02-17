// Codex.cs 
namespace Codex;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
// using System.Management;
using System.Linq;
using MethodInvoker = System.Windows.Forms.MethodInvoker;
using Microsoft.VisualBasic.FileIO;
using static Codex.Transmutation;
using static Codex.Incantation;
using static Codex.Incantation_TREEVIEW;
using static Codex.Incantation_LISTVIEW;
using static Codex.Incantation_DIALOG;

// using Timer = System.Windows.Forms.Timer;

// ===================================== transmutation 
// ... data manipulation 
public static class Transmutation {
	// Standart input and output 
	public static void print(string text) {
		Console.WriteLine(text);
	}
	public static string input(string display) {
		Console.Write(display);
        return Console.ReadLine();
	} 
	
	// Conversion methods 
	public static float to_float(object obj)	{
		return Convert.ToSingle(obj);
	}
	public static int to_int(object obj)	{
		return Convert.ToInt32(obj);
	}
	public static double to_double(object obj) {
		return Convert.ToDouble(obj);
	}
	public static string to_string(object obj) {
		return obj?.ToString() ?? string.Empty;
	}

	// Save and Load
	public static void save(string filename, string content) {
        try
        {
            string tempPath = filename + ".tmp";
            File.WriteAllText(tempPath, content);       // Write to temp
            File.Copy(tempPath, filename, overwrite: true); // Atomic replacement
            File.Delete(tempPath);                      // Clean temp
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error saving file: " + ex.Message);
        }
    }
	public static string? load(string filename) {
        try
        {
            if (!File.Exists(filename))
                return null;

            return File.ReadAllText(filename);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error loading file: " + ex.Message);
            return null;
        }
    }
	public static T new_object_from_json<T>(string filename) where T : class, new() {
		try {
			string? data_json = load(filename);
			if (string.IsNullOrWhiteSpace(data_json)) return new T();
			T? obj = JsonSerializer.Deserialize<T>(data_json);
			return obj ?? new T();
		} catch {
			// Logar o erro pode ser útil aqui também
			return new T();
		}
	}
	
	// Filesystem 
	public static string get_exec_dir() {
        return AppContext.BaseDirectory;
    }
	public static string join(string dir, string filename) {
        return Path.Combine(dir, filename);
    }
	public static string get_dir(string fullfilename) {
        if (string.IsNullOrEmpty(fullfilename)) return null;
        return Path.GetDirectoryName(fullfilename);
    }
    public static string get_filename(string fullfilename) {
        if (string.IsNullOrEmpty(fullfilename)) return null;
        return Path.GetFileName(fullfilename);
    }
    public static string get_parent_dir(string fullpath) {
        if (string.IsNullOrEmpty(fullpath)) return null;
        var dir = Path.GetDirectoryName(fullpath);
        if (string.IsNullOrEmpty(dir)) return null;
        return Directory.GetParent(dir)?.FullName;
    }
    public static bool file_exists(string fullpath) {
        if (string.IsNullOrEmpty(fullpath)) return false;
        return File.Exists(fullpath);
    }
    public static bool is_dir(string path) {
        if (string.IsNullOrEmpty(path)) return false;
        if (!Directory.Exists(path)) return false;
        var attr = File.GetAttributes(path);
        return attr.HasFlag(FileAttributes.Directory);
    }
    public static bool is_file(string path) {
        if (string.IsNullOrEmpty(path)) return false;
        if (!File.Exists(path)) return false;
        var attr = File.GetAttributes(path);
        return !attr.HasFlag(FileAttributes.Directory);
    }
	public static List<string> get_drives() {
		return DriveInfo.GetDrives()
			.Where(d => d.IsReady)
			.Select(d => d.RootDirectory.FullName.TrimEnd('\\'))
			.ToList();
	}
	public static bool? is_file_size_at_least(string fullpath, int kilobytes) {
		try {
			var fileInfo = new FileInfo(fullpath);
			if (!fileInfo.Exists)
				return null;

			long sizeInBytes = fileInfo.Length;
			long thresholdInBytes = kilobytes * 1024L;

			return sizeInBytes >= thresholdInBytes;
		} catch {
			return null;
		}
	}
	public static string get_extension(string fullpath) {
		if (string.IsNullOrWhiteSpace(fullpath))
			return "";

		try {
			return Path.GetExtension(fullpath);
		} catch {
			return "";
		}
	}
	public static bool has_extension(List<string> paths, string ext) {
		if ( paths.Count == 0 ) return false; 
		foreach( var path in paths){
			if (get_extension(path) == ext) return true;
		}
		return false;
	}

	// List 
	public static T? Get<T>(this List<T> list, int index) {
		return (index >= 0 && index < list.Count) ? list[index] : default;
	}
	
}

// ===================================== incantation 
// ... graphical user interface and user interaction 
/* incantation 
1. new_ ; constructors 
2. add_ ; add child component 
3. on_ ; event listener setup 
*/

public static class Incantation {
	
	public static void register_icon(Form mainForm, string icon_name, string namespace_str){
		
		var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(namespace_str+"."+icon_name+".ico");
        if (stream != null)
        {
            mainForm.Icon = new Icon(stream);
        }
	}
	
	// utils
	public static T? get_first<T>(Control parent) where T : Control {
		foreach (Control child in parent.Controls) {
			if (child is T match)
				return match;
		}
		return null;
	}
	public static List<T> get_list<T>(Control parent) where T : Control {
		List<T> result = new List<T>();
		foreach (Control child in parent.Controls) {
			if (child is T match) result.Add(match);
		}
		return result;
	}
	
	// Size and position
	public static void size(Form form, int width, int height) {
        form.Size = new Size(width, height);
    }
    public static void size(Form form, float width_screen_percentage, float height_screen_percentage) {
        Screen screen = Screen.FromControl(form);
        Rectangle bounds = screen.WorkingArea;

        int newWidth = (int)(bounds.Width * width_screen_percentage);
        int newHeight = (int)(bounds.Height * height_screen_percentage);

        form.Size = new Size(newWidth, newHeight);
    }
	public static void center(Form form) {
        Screen screen = Screen.FromControl(form); // Gets the screen where the form is
        Rectangle bounds = screen.WorkingArea;    // Excludes taskbar and docked items

        int x = bounds.X + (bounds.Width - form.Width) / 2;
        int y = bounds.Y + (bounds.Height - form.Height) / 2;

        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(x, y);
    }
	public static void set_as_vertical_flow_layout(TableLayoutPanel panel,List<float> percentages) {
		if (panel == null || percentages == null || percentages.Count == 0)
			return;

		panel.SuspendLayout();
		panel.Controls.Clear();
		panel.RowStyles.Clear();
		panel.ColumnStyles.Clear();
		panel.ColumnCount = 1;
		panel.RowCount = percentages.Count;
		panel.AutoSize = false;
		panel.Dock = DockStyle.Fill;
		panel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;

		// Set the single column to 100% stretch
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

		for (int i = 0; i < percentages.Count; i++) {
			float p = percentages[i];
			if (p <= 0f) {
				// AutoSize
				panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			}
			else {
				panel.RowStyles.Add(new RowStyle(SizeType.Percent, p));
			}
		}

		panel.ResumeLayout();
	}
	public static async void animated_resize(Form form, int x, int y, int width, int height, int steps, int delayMs) {
        if (form == null) return;
        // Current form properties
        int startX = form.Location.X;
        int startY = form.Location.Y;
        int startWidth = form.Width;
        int startHeight = form.Height;
        // Calculate step increments
        double deltaX = (x - startX) / (double)steps;
        double deltaY = (y - startY) / (double)steps;
        double deltaWidth = (width - startWidth) / (double)steps;
        double deltaHeight = (height - startHeight) / (double)steps;
        // Perform animation
        for (int i = 1; i <= steps; i++) {
            // Calculate new values
            int newX = startX + (int)(deltaX * i);
            int newY = startY + (int)(deltaY * i);
            int newWidth = startWidth + (int)(deltaWidth * i);
            int newHeight = startHeight + (int)(deltaHeight * i);
            // Update form properties on the UI thread
            form.Invoke((Action)(() => {
                form.Location = new System.Drawing.Point(newX, newY);
                form.Size = new System.Drawing.Size(newWidth, newHeight);
            }));
            // Delay for smooth animation
            await Task.Delay(delayMs);
        }
        // Ensure final position and size are exact
        form.Invoke((Action)(() =>
        {
            form.Location = new System.Drawing.Point(x, y);
            form.Size = new System.Drawing.Size(width, height);
        }));
    }
	public static void animated_resize(Form form, int x, int y, int width, int height){
		animated_resize(form, x, y, width, height, 20, 5);
	}
	public static void animated_resize(Form form, Rectangle rect) {
		if (form == null || rect.IsEmpty) return;
		int x = rect.X;       
        int y = rect.Y;       
        int width = rect.Width;   
        int height = rect.Height; 
		animated_resize(form, x,y, width, height);
	}
	public static void animated_resize(Form form, Rectangle rect, int steps, int delayMs) {
		if (form == null || rect.IsEmpty) return;
		int x = rect.X;       
        int y = rect.Y;       
        int width = rect.Width;   
        int height = rect.Height; 
		animated_resize(form, x,y, width, height, steps, delayMs);
	}
	public static Rectangle get_dock_rect(Form form, float w_scr_perc, float h_scr_perc, string pos) {
		// Returns a Rectangle for docking the form to a screen edge based on width and height percentages 
		// pos: "north", "south", "east", or "west" for docking position
		// w_scr_perc: Width as a percentage of the screen's width (0.0 to 1.0)
		// h_scr_perc: Height as a percentage of the screen's height (0.0 to 1.0)
		
        // Validate inputs
        if (form == null) throw new ArgumentNullException(nameof(form));
        if (w_scr_perc < 0 || w_scr_perc > 1 || h_scr_perc < 0 || h_scr_perc > 1)
            throw new ArgumentOutOfRangeException("Width and height percentages must be between 0.0 and 1.0");
        if (string.IsNullOrEmpty(pos)) throw new ArgumentNullException(nameof(pos));
        // Get the screen where the form is located
        Screen screen = Screen.FromControl(form);
        Rectangle workingArea = screen.WorkingArea;
        // Calculate dimensions based on percentages
        int width = (int)(workingArea.Width * w_scr_perc);
        int height = (int)(workingArea.Height * h_scr_perc);
        // Initialize position
        int x = 0;
        int y = 0;
        // Determine position based on docking
        switch (pos.ToLower())
        {
            case "north":
                x = workingArea.X + (workingArea.Width - width) / 2; // Center horizontally
                y = workingArea.Y; // Top edge
                break;
            case "south":
                x = workingArea.X + (workingArea.Width - width) / 2; // Center horizontally
                y = workingArea.Y + workingArea.Height - height; // Bottom edge
                break;
            case "east":
                x = workingArea.X + workingArea.Width - width; // Right edge
                y = workingArea.Y + (workingArea.Height - height) / 2; // Center vertically
                break;
            case "west":
                x = workingArea.X; // Left edge
                y = workingArea.Y + (workingArea.Height - height) / 2; // Center vertically
                break;
            default:
                throw new ArgumentException("Position must be 'north', 'south', 'east', or 'west'", nameof(pos));
        }
        // Return the calculated Rectangle
        return new Rectangle(x, y, width, height);
    }
	public static Rectangle get_centered_relative_to_screen_rect(Form form, float wScrPerc, float hScrPerc) {
        // Validate input percentages
        wScrPerc = Math.Clamp(wScrPerc, 0.1f, 1.0f);
        hScrPerc = Math.Clamp(hScrPerc, 0.1f, 1.0f);
        // Get the screen where the form is primarily located 
        Screen screen = Screen.FromControl(form);
        // Calculate rectangle dimensions
        int width = (int)(screen.WorkingArea.Width * wScrPerc);
        int height = (int)(screen.WorkingArea.Height * hScrPerc);
        // Calculate position to center the rectangle
        int x = screen.WorkingArea.X + (screen.WorkingArea.Width - width) / 2;
        int y = screen.WorkingArea.Y + (screen.WorkingArea.Height - height) / 2;
        return new Rectangle(x, y, width, height);
    }
	
	// Panel
	public static FlowLayoutPanel new_horizontal_panel(List<Control> list) {
        var panel = new FlowLayoutPanel {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        foreach (var control in list) {
            panel.Controls.Add(control);
        }
        return panel;
    }
    public static FlowLayoutPanel new_vertical_panel(List<Control> list) {
        var panel = new FlowLayoutPanel {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        foreach (var control in list) {
            panel.Controls.Add(control);
        }
        return panel;
    }
	public static List<Control> new_button_list(List<string> labels)	{
		var buttons = new List<Control>();
		foreach (var label in labels)
		{
			var btn = new Button
			{
				Text = label,
				AutoSize = true // Optional: automatically size button to text
			};
			buttons.Add(btn);
		}
		return buttons;
	}
	public static List<Control> new_dark_button_list(List<string> labels) {
		var buttons = new List<Control>();
		foreach (var label in labels)
		{
			var btn = new Button
			{
				Text = label,
				AutoSize = true,
				BackColor = Color.FromArgb(30, 30, 40),
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Margin = new Padding(4),
				Padding = new Padding(6, 4, 6, 4)
			};
			btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 90);
			btn.FlatAppearance.BorderSize = 1;

			buttons.Add(btn);
		}
		return buttons;
	}
	public static SplitContainer new_horizontal_split(Control ctrl1, Control ctrl2) {
        var split = new SplitContainer {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 50,
            Panel2MinSize = 50,
            SplitterDistance = 150 // adjust as needed
        };

        split.Panel1.Controls.Add(ctrl1);
        split.Panel2.Controls.Add(ctrl2);

        ctrl1.Dock = DockStyle.Fill;
        ctrl2.Dock = DockStyle.Fill;

        return split;
    }
    public static SplitContainer new_vertical_split(Control ctrl1, Control ctrl2) {
        var split = new SplitContainer {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel1MinSize = 50,
            Panel2MinSize = 50,
            SplitterDistance = 200 // adjust as needed
        };

        split.Panel1.Controls.Add(ctrl1);
        split.Panel2.Controls.Add(ctrl2);

        ctrl1.Dock = DockStyle.Fill;
        ctrl2.Dock = DockStyle.Fill;

        return split;
    }
	
	// Tabs 
	public static TabControl new_tabs() {
		var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Multiline = true, 
        };
        return tabControl;
	}
	public static TabPage? add_tab<T>(TabControl tabs, string title) where T : Control, new() {
		if (tabs == null || string.IsNullOrEmpty(title)) return null;
		try	{
			var tabPage = new TabPage(title);
			var control = new T
			{
				Dock = DockStyle.Fill
			};
			tabPage.Controls.Add(control);
			tabs.TabPages.Add(tabPage);
			return tabPage;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error creating tab with {typeof(T).Name}: {ex.Message}");
			return null;
		}
	}
	public static TabPage? get_pointed_tab(TabControl tabs, Point location)	{
		for (int i = 0; i < tabs.TabPages.Count; i++)
		{
			Rectangle tabRect = tabs.GetTabRect(i);
			if (tabRect.Contains(location))
			{
				return tabs.TabPages[i];
			}
		}
		return null;
	}
	public static bool close_tab(TabControl tabs, TabPage page)	{
		if (tabs == null || page == null) return false;
		if (!tabs.TabPages.Contains(page)) return false;

		tabs.TabPages.Remove(page);
		page.Dispose(); // optional: free resources associated with the tab
		return true;
	}
	
	// TextBox 
	public static TextBox new_multiline_text_box() {
        return new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 8),
        };
    }
	public static TabControl new_multiline_text_tabs() { // deprecated 
		// --> new_tabs() 
        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Multiline = true, // allows multiple rows of tabs if needed
        };
        return tabControl;
    }
    public static DarkTabControl new_multiline_text_dark_tabs() {
		var tabControl = new DarkTabControl
        {
            Dock = DockStyle.Fill,
            Multiline = true, // allows multiple rows of tabs if needed
        };
        return tabControl;
	}
	public static DarkTabControl new_dark_tabs() {
		var tabControl = new DarkTabControl
        {
            Dock = DockStyle.Fill,
            Multiline = true, // allows multiple rows of tabs if needed
        };
        return tabControl;
	}
	public static TabPage? add_multiline_text_tab(TabControl tabs, string title) {
		return add_multiline_text_tab(tabs, title, new Font("Consolas", 10) );
    }
	public static TabPage? add_multiline_text_tab(TabControl tabs, string title, Font font) {
        if (tabs == null) return null;

        var newTab = new TabPage(title);
        var textBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Font = font,
            AcceptsTab = true,
            AcceptsReturn = true,
            WordWrap = true,
        };

        newTab.Controls.Add(textBox);
        tabs.TabPages.Add(newTab);
		return newTab;
    }
	
	// Events 
	public static void key_shortcut(Control control, string modifiers, string key, Action action) {
		if (control == null || action == null) return;

		// Ensure form captures key events before child controls
		if (control is Form form)
		{
			form.KeyPreview = true;
		}

		// Normalize modifier string to lowercase and split by "+"
		var requiredModifiers = modifiers?.ToLower().Split('+', StringSplitOptions.RemoveEmptyEntries)
								 ?? Array.Empty<string>();

		control.KeyDown += (sender, e) =>
		{
			bool ctrl = requiredModifiers.Contains("ctrl");
			bool alt = requiredModifiers.Contains("alt");
			bool shift = requiredModifiers.Contains("shift");

			// Check if current key state matches required modifiers
			bool modifiersMatch = (!ctrl || e.Control) &&
								  (!alt || e.Alt) &&
								  (!shift || e.Shift);

			if (modifiersMatch)
			{
				// Parse key (ignore modifiers in string)
				if (Enum.TryParse<Keys>(key, true, out Keys parsedKey))
				{
					if (e.KeyCode == parsedKey)
					{
						action.Invoke();
						e.Handled = true;
					}
				}
			}
		};
	}
	private static Dictionary<Control, DateTime> _lastClickTimes = new();
	public static void on_double_click(Control control, Action action) {
		if (control == null || action == null) return;

		control.MouseDown += (sender, e) => {
			if (e.Button != MouseButtons.Left) return;

			DateTime now = DateTime.Now;

			if (_lastClickTimes.TryGetValue(control, out DateTime lastClick)) {
				double diff = (now - lastClick).TotalMilliseconds;
				if (diff <= SystemInformation.DoubleClickTime) {
					action.Invoke();
				}
			}

			_lastClickTimes[control] = now;
		};
	}
	
	[DllImport("user32.dll")]
	private static extern bool ReleaseCapture();
	[DllImport("user32.dll")]
	private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
	private const int WM_NCLBUTTONDOWN = 0xA1;
	private const int HTCAPTION = 0x2;
	public static void drag_window(Control control) {
		control.MouseDown += (sender, e) =>
		{
			Form? form = control.FindForm();
			if (form == null) return;
			if (form.WindowState == FormWindowState.Maximized) return;

			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
			}
		};
	}
	
	// Themed
	public static void hide_titlebar(Form form) {
		form.SuspendLayout();
		form.Visible = false;
		form.FormBorderStyle = FormBorderStyle.None;
		form.Visible = true;
		form.ResumeLayout();
	}
	public static void show_titlebar(Form form) {
		form.SuspendLayout();
		form.Visible = false;
		form.FormBorderStyle = FormBorderStyle.Sizable;
		form.Visible = true;
		form.ResumeLayout();
	}
	public static void apply_theme_recursive(Form form, Color background, Color foreground)	{
		if (form == null) return;

		form.BackColor = background;
		form.ForeColor = foreground;

		apply_theme_recursive((Control)form, background, foreground);
	}
	public static void apply_theme_recursive(Control control, Color background, Color foreground) {
		if (control == null) return;

		control.BackColor = background;
		control.ForeColor = foreground;

		foreach (Control child in control.Controls)
		{
			apply_theme_recursive(child, background, foreground);
		}
	}
	public static Color rgb(int r, int g, int b) {
		return Color.FromArgb(r,g,b);
	}
	
	// Context Menu 
	public static ContextMenuStrip add_context_menu(Control? control, List<object> items) {
		var menu = new ContextMenuStrip();
		foreach (var item in items) {
			if (item is string text) {
				// Simple menu item with text
				var menuItem = new ToolStripMenuItem(text);
				menu.Items.Add(menuItem);
			} 
			else if (item is ToolStripMenuItem submenu) {
				// Already a submenu, just add it
				menu.Items.Add(submenu);
			}
			else if (item is (string subText, List<object> subItems)) {
				// Tuple: submenu label + submenu items (recursive)
				var subMenuItem = new ToolStripMenuItem(subText);
				foreach (var subItem in subItems) {
					if (subItem is string st)
						subMenuItem.DropDownItems.Add(new ToolStripMenuItem(st));
					else if (subItem is ToolStripMenuItem sti)
						subMenuItem.DropDownItems.Add(sti);
					// Can extend for more nested types
				}
				menu.Items.Add(subMenuItem);
			}
			// Extend with more types as needed
		}
		if (control != null) control.ContextMenuStrip = menu; 
		return menu;
	}
	public static ToolStripMenuItem new_submenu(string label, List<object> items) {
		var submenu = new ToolStripMenuItem(label);
		foreach (var item in items) {
			if (item is string text) {
				submenu.DropDownItems.Add(new ToolStripMenuItem(text));
			}
			else if (item is ToolStripMenuItem menuItem) {
				submenu.DropDownItems.Add(menuItem);
			}
			else if (item is (string subText, List<object> subItems)) {
				var nestedSub = new ToolStripMenuItem(subText);
				foreach (var subItem in subItems) {
					if (subItem is string st)
						nestedSub.DropDownItems.Add(new ToolStripMenuItem(st));
					else if (subItem is ToolStripMenuItem sti)
						nestedSub.DropDownItems.Add(sti);
				}
				submenu.DropDownItems.Add(nestedSub);
			}
		}
		return submenu;
	}
	public static ToolStripMenuItem? get(ContextMenuStrip menu, string text) {
		foreach (ToolStripItem item in menu.Items) {
			if (item is ToolStripMenuItem menuItem) {
				if (menuItem.Text.Contains(text)) return menuItem;
				// Recursive search in submenus
				var found = get(menuItem.DropDown, text);
				if (found != null)
					return found;
			}
		}
		return null;
	}
	public static ToolStripMenuItem? get(ToolStripDropDown dropdown, string text) {
		foreach (ToolStripItem item in dropdown.Items) {
			if (item is ToolStripMenuItem menuItem) {
				if (menuItem.Text.Contains(text)) return menuItem;
				var found = get(menuItem.DropDown, text);
				if (found != null)
					return found;
			}
		}
		return null;
	}
	/* public static void set_action(this ContextMenuStrip menu, string text, EventHandler handler){
		var item = get(menu, text);
		if (item==null) return ;
		item.Click += handler; 
	} */
	public static void set_action(ContextMenuStrip menu, string text, EventHandler handler){
		var item = get(menu, text);
		if (item==null) return ;
		item.Click += handler; 
	}

	// tootip
	public static ToolTip add_tooltip(Control control, string text) {
        ToolTip tooltip = new ToolTip();

        tooltip.AutoPopDelay = 5000;
        tooltip.InitialDelay = 1000;
        tooltip.ReshowDelay = 500;
        tooltip.ShowAlways = true;

        tooltip.SetToolTip(control, text);

        return tooltip;
    }

	// 
	private static readonly Dictionary<Form, System.Windows.Forms.Timer> animationTimers = new();
	private static readonly Dictionary<Form, bool> dock_active = new();
	private static readonly float collapsedPerc = 0.1f;
	public static void set_as_dock_window(Form form, string dock, float focus_screen_perc) {
		// F12 toggles between docked and free mode.
		// Dock options: "north", "south", "east", "west"
		form.KeyPreview = true; // Ensure form gets key events
		dock_active[form] = false;
		Rectangle screen = Screen.FromControl(form).WorkingArea;
		int expandedSize = (dock == "north" || dock == "south")
			? (int)(screen.Height * focus_screen_perc)
			: (int)(screen.Width * focus_screen_perc);
		int collapsedSize = (dock == "north" || dock == "south")
			? (int)(screen.Height * collapsedPerc)
			: (int)(screen.Width * collapsedPerc);

		// Animate expand/collapse
		void Animate(bool expand) {
			if (!dock_active[form]) return;

			form.TopMost = true;
			form.FormBorderStyle = FormBorderStyle.None;

			// 🔧 Re-evaluate screen now, based on actual form position
			Rectangle screen = Screen.FromControl(form).WorkingArea;

			// Set starting dock position
			switch (dock.ToLower()) {
				case "north":
					form.Bounds = new Rectangle(screen.Left, screen.Top, screen.Width, collapsedSize);
					break;
				case "south":
					form.Bounds = new Rectangle(screen.Left, screen.Bottom - collapsedSize, screen.Width, collapsedSize);
					break;
				case "west":
					form.Bounds = new Rectangle(screen.Left, screen.Top, collapsedSize, screen.Height);
					break;
				case "east":
					form.Bounds = new Rectangle(screen.Right - collapsedSize, screen.Top, collapsedSize, screen.Height);
					break;
			}

			if (animationTimers.ContainsKey(form))
				animationTimers[form].Stop();

			var timer = new System.Windows.Forms.Timer { Interval = 15 };
			animationTimers[form] = timer;

			timer.Tick += (s, e) => {
				var bounds = form.Bounds;
				bool done = false;

				switch (dock.ToLower()) {
					case "north":
						int newH_N = expand ? bounds.Height + 20 : bounds.Height - 20;
						newH_N = Math.Clamp(newH_N, collapsedSize, expandedSize);
						form.Bounds = new Rectangle(screen.Left, screen.Top, screen.Width, newH_N);
						done = (newH_N == expandedSize && expand) || (newH_N == collapsedSize && !expand);
						break;

					case "south":
						int newH_S = expand ? bounds.Height + 20 : bounds.Height - 20;
						newH_S = Math.Clamp(newH_S, collapsedSize, expandedSize);
						form.Bounds = new Rectangle(screen.Left, screen.Bottom - newH_S, screen.Width, newH_S);
						done = (newH_S == expandedSize && expand) || (newH_S == collapsedSize && !expand);
						break;

					case "west":
						int newW_W = expand ? bounds.Width + 20 : bounds.Width - 20;
						newW_W = Math.Clamp(newW_W, collapsedSize, expandedSize);
						form.Bounds = new Rectangle(screen.Left, screen.Top, newW_W, screen.Height);
						done = (newW_W == expandedSize && expand) || (newW_W == collapsedSize && !expand);
						break;

					case "east":
						int newW_E = expand ? bounds.Width + 20 : bounds.Width - 20;
						newW_E = Math.Clamp(newW_E, collapsedSize, expandedSize);
						form.Bounds = new Rectangle(screen.Right - newW_E, screen.Top, newW_E, screen.Height);
						done = (newW_E == expandedSize && expand) || (newW_E == collapsedSize && !expand);
						break;
				}

				if (done) timer.Stop();
			};

			timer.Start();
		}

		// Use form activation instead of focus
		form.Activated += (s, e) => Animate(true);
		form.Deactivate += (s, e) => Animate(false);
		form.KeyDown += (s, e) => {
			if (e.KeyCode == Keys.F12) {
				if (dock_active[form]) {
					dock_active[form] = false;
					form.FormBorderStyle = FormBorderStyle.Sizable;
					form.TopMost = false;
					form.Bounds = new Rectangle(screen.Width / 4, screen.Height / 4, 800, 600);
				} else {
					dock_active[form] = true; 
					MessageBox.Show("Docking Activated");
				}
			}
		};
	}
}

public static class Incantation_TREEVIEW {
	private static string dummy_node = "Loading...";
	public static TreeView new_dummy_tree(string display_text){
		var tree = new TreeView { Dock = DockStyle.Fill };
		string label = display_text; 
		var rootNode = new TreeNode {
			Text = label,
			Tag = null
		};
		tree.Nodes.Add(rootNode);
		return tree;
	}
	public static TreeView new_tree(string root) {
		bool isDir = Directory.Exists(root);
		var tree = new TreeView { Dock = DockStyle.Fill };
		string label = Path.GetFileName(root.TrimEnd('\\'));
		if (!isDir || string.IsNullOrWhiteSpace(label) ) label = root; 
		var rootNode = new TreeNode {
			Text = label,
			Tag = isDir ? root : null
		};
		if (isDir) rootNode.Nodes.Add(dummy_node);
		tree.Nodes.Add(rootNode);
		return tree;
	}
	public static DarkTreeView new_dark_tree(string root) {
		bool isDir = Directory.Exists(root);
		var tree = new DarkTreeView { Dock = DockStyle.Fill };
		string label = Path.GetFileName(root.TrimEnd('\\'));
		if (!isDir || string.IsNullOrWhiteSpace(label) ) label = root; 
		var rootNode = new TreeNode {
			Text = label,
			Tag = isDir ? root : null
		};
		if (isDir) rootNode.Nodes.Add(dummy_node);
		tree.Nodes.Add(rootNode);
		return tree;
	}
	public static MultiSelectTreeView new_multiselection_tree(string root){
		bool isDir = Directory.Exists(root);
		var tree = new MultiSelectTreeView { Dock = DockStyle.Fill };
		string label = Path.GetFileName(root.TrimEnd('\\'));
		if (!isDir || string.IsNullOrWhiteSpace(label) ) label = root; 
		var rootNode = new TreeNode {
			Text = label,
			Tag = isDir ? root : null
		};
		if (isDir) rootNode.Nodes.Add(dummy_node);
		tree.Nodes.Add(rootNode);
		return tree;
	}
	public static void join(TreeView master, TreeView tree)	{
		if (master == null || tree == null) return;
		if (master.Nodes.Count == 0 || tree.Nodes.Count == 0) return;

		// Get the first root node of each
		TreeNode masterRoot = master.Nodes[0];
		TreeNode childNode = (TreeNode)tree.Nodes[0].Clone();

		// Add the root of 'tree' as a child of the master root
		masterRoot.Nodes.Add(childNode);
	}
	public static List<TreeNode> get_toplevel_nodes(TreeView tree) {
		if (tree == null || tree.Nodes.Count == 0)
			return new List<TreeNode>();

		return tree.Nodes.Cast<TreeNode>().ToList();
	}
	public static List<TreeNode> get_toplevel_nodes_of_rootnode(TreeView tree){
		if (tree ==null) return new List<TreeNode>();
		if (tree.Nodes.Count == 0) return new List<TreeNode>();
		TreeNode root = tree.Nodes[0];
		if (root.Nodes.Count == 0) return new List<TreeNode>();
		return root.Nodes.Cast<TreeNode>().ToList();
	}
	public static void set_as_filesystem_tree(TreeView tree) {
		if (tree == null) return;
		tree.BeforeExpand += (sender, e) => {
			var node = e.Node;
			string? path = node.Tag as string;
			if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
			// Refresh only if dummy node is present
			if (node.Nodes.Count == 1 && node.Nodes[0].Text == dummy_node)
			{
				refresh_filesystem_node(node);
			}
		};
	}
	public static void refresh_filesystem_node(TreeNode node) {
		string? path = node.Tag as string;
		if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
		node.Nodes.Clear();
		try
		{
			// Add directories
			foreach (var dir in get_directories(path))
			{
				try
				{
					string dirName = Path.GetFileName(dir);
					var dirNode = new TreeNode(dirName) { Tag = dir };

					try
					{
						if (get_directories(dir).Length > 0 || get_files(dir).Length > 0)
							dirNode.Nodes.Add(dummy_node);
					}
					catch { }

					node.Nodes.Add(dirNode);
				}
				catch { }
			}

			// Add files
			foreach (var file in get_files(path))
			{
				try
				{
					string fileName = Path.GetFileName(file);
					var fileNode = new TreeNode(fileName) { Tag = file };
					node.Nodes.Add(fileNode);
				}
				catch { }
			}
		}
		catch (Exception ex)
		{
			node.Nodes.Add(new TreeNode($"Error: {ex.Message}"));
		}
	}
	private static string[] get_directories(string path) {
		try {
			// Fix paths like "G:" to "G:\"
			if (!string.IsNullOrEmpty(path) && path.Length == 2 && path[1] == ':') {
				path += @"\";
			}
			return Directory.GetDirectories(path);
		}
		catch {
			// Skip unreadable/inaccessible directories
			return Array.Empty<string>();
		}
	}
	private static string[] get_files(string path) {
		try {
			// Fix paths like "G:" to "G:\"
			if (!string.IsNullOrEmpty(path) && path.Length == 2 && path[1] == ':') {
				path += @"\";
			}
			return Directory.GetFiles(path);
		}
		catch {
			return Array.Empty<string>();
		}
	}
	public static List<string> get_fullpath(List<TreeNode> list) {
		var result = new List<string>();
		foreach (var node in list)
		{
			string path = node.FullPath;
			// Optional: If your tree stores custom paths in .Tag
			if (node.Tag is string tagPath) path = tagPath;
			if (File.Exists(path) || Directory.Exists(path)) result.Add(path);
		}
		return result;
	}
	public static TreeNode? get_pointed_node(TreeView tree) {
		if (tree == null) return null;
		// Convert the current screen mouse position to tree-relative coordinates
		Point localPos = tree.PointToClient(Control.MousePosition); 
		// Get the node at that point
		return tree.GetNodeAt(localPos);
	}
	public static void collapse(TreeView tree, int level) {
        if (tree == null) return;

        // Begin update to prevent flickering
        tree.BeginUpdate();

        // Helper method to collapse nodes recursively
        void CollapseNodes(TreeNodeCollection nodes, int currentLevel)
        {
            foreach (TreeNode node in nodes)
            {
                // Collapse the node if its level is greater than or equal to the specified level
                if (currentLevel >= level)
                {
                    node.Collapse();
                }
                // Recursively process child nodes
                CollapseNodes(node.Nodes, currentLevel + 1);
            }
        }

        // Start collapsing from the top-level nodes (level 0)
        CollapseNodes(tree.Nodes, 0);

        // End update to refresh the TreeView
        tree.EndUpdate();
    }
}

public static class Incantation_LISTVIEW {
	public static ListView new_file_list() {
		ListView fileList = new ListView();
		// View settings
		fileList.View = View.Details;
		fileList.FullRowSelect = true;
		fileList.GridLines = true;
		fileList.MultiSelect = false;
		fileList.Dock = DockStyle.Fill;
		// Columns: Name, Full Path, Type
		fileList.Columns.Add("Name", 200, HorizontalAlignment.Left);
		fileList.Columns.Add("Full Path", 400, HorizontalAlignment.Left);
		fileList.Columns.Add("Type", 100, HorizontalAlignment.Left);
		// Optional: Enable small icon view with default icons (Windows-like)
		fileList.SmallImageList = new ImageList();
		fileList.SmallImageList.ImageSize = new Size(16, 16);
		fileList.SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
		return fileList;
	}
	public static void add_to_list_filesystem_view(ListView view, string fullpath) {
		if (string.IsNullOrWhiteSpace(fullpath)) return;
		foreach (ListViewItem item in view.Items) {
			if (string.Equals(item.SubItems[1].Text, fullpath, StringComparison.OrdinalIgnoreCase)) {
				return; // already exists, skip
			}
		}
		if (Directory.Exists(fullpath)) {
			// It's a folder
			string name = Path.GetFileName(fullpath.TrimEnd(Path.DirectorySeparatorChar));
			ListViewItem item = new ListViewItem(name);
			item.SubItems.Add(fullpath);
			item.SubItems.Add("Folder");
			view.Items.Add(item);
		}
		else if (File.Exists(fullpath)) {
			// It's a file
			string name = Path.GetFileName(fullpath);
			string ext = Path.GetExtension(fullpath).TrimStart('.').ToUpper();
			if (string.IsNullOrWhiteSpace(ext)) ext = "File";
			ListViewItem item = new ListViewItem(name);
			item.SubItems.Add(fullpath);
			item.SubItems.Add(ext);
			view.Items.Add(item);
		}
	}
	public static ListView new_dark_file_list()	{
		var fileList = new ListView();

		// Basic View setup
		fileList.View = View.Details;
		fileList.FullRowSelect = true;
		fileList.GridLines = true;
		fileList.MultiSelect = false;
		fileList.Dock = DockStyle.Fill;

		// Theme colors
		fileList.BackColor = Color.FromArgb(20, 20, 25);         // Dark background
		fileList.ForeColor = Color.White;                        // White text
		fileList.BorderStyle = BorderStyle.None;
		fileList.HideSelection = false;

		// Optional highlight colors for selected items
		fileList.OwnerDraw = true;
		fileList.DrawColumnHeader += (s, e) =>
		{
			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 35)), e.Bounds);
			TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, Color.White);
		};
		fileList.DrawItem += (s, e) => e.DrawDefault = true;
		fileList.DrawSubItem += (s, e) => e.DrawDefault = true;

		// Columns
		fileList.Columns.Add("Name", 200, HorizontalAlignment.Left);
		fileList.Columns.Add("Full Path", 400, HorizontalAlignment.Left);
		fileList.Columns.Add("Type", 100, HorizontalAlignment.Left);

		// Optional: enable icon support
		fileList.SmallImageList = new ImageList
		{
			ImageSize = new Size(16, 16),
			ColorDepth = ColorDepth.Depth32Bit
		};

		return fileList;
	}
	public static void set_dark_theme(ListView fileList){
		// Theme colors
		fileList.BackColor = Color.FromArgb(10, 10, 15);         // Dark background
		fileList.ForeColor = Color.White;                        // White text
		fileList.BorderStyle = BorderStyle.None;
		fileList.HideSelection = false;
		fileList.GridLines = false;

		// Optional highlight colors for selected items
		fileList.OwnerDraw = true;
		fileList.DrawColumnHeader += (s, e) =>
		{
			e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 35)), e.Bounds);
			TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, Color.White);
		};
		fileList.DrawItem += (s, e) => e.DrawDefault = true;
		fileList.DrawSubItem += (s, e) => e.DrawDefault = true;
		
		// Optional: enable icon support
		fileList.SmallImageList = new ImageList
		{
			ImageSize = new Size(16, 16),
			ColorDepth = ColorDepth.Depth32Bit
		};
	}
	public static void add_from_dir_to_list_filesystem_view(ListView view, string folderPath) {
		if (!Directory.Exists(folderPath)) return;

		// Cache existing full paths from the ListView
		HashSet<string> existingPaths = new HashSet<string>(
			view.Items.Cast<ListViewItem>()
			.Select(item => item.SubItems[1].Text),
			StringComparer.OrdinalIgnoreCase
		);

		// Add folders
		foreach (string dir in Directory.GetDirectories(folderPath)) {
			if (!existingPaths.Contains(dir)) {
				add_to_list_filesystem_view(view, dir);
				existingPaths.Add(dir);
			}
		}

		// Add files
		foreach (string file in Directory.GetFiles(folderPath)) {
			if (!existingPaths.Contains(file)) {
				add_to_list_filesystem_view(view, file);
				existingPaths.Add(file);
			}
		}
	}
	public static ListView new_log_list() {
		var list = new ListView {
			View = View.Details,
			FullRowSelect = true,
			GridLines = true,
			Dock = DockStyle.Fill,
			HeaderStyle = ColumnHeaderStyle.Nonclickable
		};
		list.ShowItemToolTips = true;
		list.Columns.Add("Time", 100);
		list.Columns.Add("Message", 400);
		return list;
	}
	public static void add_log(ListView log_list, string label) {
		var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
		item.SubItems.Add(label); 
		item.ToolTipText = label;
		log_list.Items.Add(item); 
		log_list.EnsureVisible(log_list.Items.Count - 1); // Scroll to latest
	}
	
	// >>>
	public static void add_log_color(ListView log_list, string label, Color Fore, Color Back) {
		var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
		item.BackColor = Back;
		item.ForeColor = Fore;
		item.SubItems.Add(label); 
		item.ToolTipText = label;
		log_list.Items.Add(item); 
		log_list.EnsureVisible(log_list.Items.Count - 1); // Scroll to latest
	}
	public static void add_log_color(ListView log_list, string label, Color Fore) {
		var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
		item.ForeColor = Fore;
		item.SubItems.Add(label); 
		item.ToolTipText = label;
		log_list.Items.Add(item); 
		log_list.EnsureVisible(log_list.Items.Count - 1); // Scroll to latest
	}
	// <<< 
	
	public static void remove_selected(ListView list) {
		if (list == null || list.SelectedItems.Count == 0) return;

		// Make a copy to avoid modifying the collection while iterating
		var selected = list.SelectedItems.Cast<ListViewItem>().ToList();
		foreach (var item in selected) {
			list.Items.Remove(item);
		}
	}
	public static void clear(ListView list){
		list.Items.Clear();
	}
	public static List<string> get_fullpath(ListView filelist) {
		List<string> paths = new List<string>();

		foreach (ListViewItem item in filelist.Items) {
			// Make sure the item has at least 2 subitems (Name, Full Path)
			if (item.SubItems.Count >= 2) {
				string fullPath = item.SubItems[1].Text;
				if (!string.IsNullOrWhiteSpace(fullPath)) {
					paths.Add(fullPath);
				}
			}
		}

		return paths;
	}
	public static List<string> get_fullpath_selected(ListView filelist) {
		List<string> paths = new List<string>();
		foreach (ListViewItem item in filelist.SelectedItems) {
			// Make sure the item has at least 2 subitems (Name, Full Path)
			if (item.SubItems.Count >= 2) {
				string fullPath = item.SubItems[1].Text;
				if (!string.IsNullOrWhiteSpace(fullPath)) {
					paths.Add(fullPath);
				}
			}
		}
		return paths;
	}
	
	public static ListView new_file_list_icons() {
		ListView fileList = new ListView();
		fileList.View = View.LargeIcon;
		fileList.MultiSelect = false;
		fileList.FullRowSelect = true;
		fileList.Dock = DockStyle.Fill;
		// Setup LargeImageList
		ImageList imageList = new ImageList();
		imageList.ImageSize = new Size(32, 32); // Explorer-style size
		imageList.ColorDepth = ColorDepth.Depth32Bit;
		fileList.LargeImageList = imageList;
		return fileList;
	}
	public static void add_to_list_filesystem_view_icons(ListView view, string fullpath) {
		if (string.IsNullOrWhiteSpace(fullpath)) return;
		// Prevent duplicates
		foreach (ListViewItem item in view.Items) {
			if (string.Equals(item.Tag as string, fullpath, StringComparison.OrdinalIgnoreCase)) {
				return;
			}
		}
		string name = Path.GetFileName(fullpath.TrimEnd(Path.DirectorySeparatorChar));
		if (string.IsNullOrWhiteSpace(name)) name = fullpath;

		// Get or create image index
		int imageIndex = add_system_icon(view.LargeImageList, fullpath);
		var newItem = new ListViewItem(name, imageIndex) {
			Tag = fullpath // Store full path for later use
		};
		view.Items.Add(newItem);
	}
	[DllImport("Shell32.dll")]
	private static extern IntPtr SHGetFileInfo(
		string pszPath,
		uint dwFileAttributes,
		out SHFILEINFO psfi,
		uint cbFileInfo,
		uint uFlags);
	[StructLayout(LayoutKind.Sequential)]
	private struct SHFILEINFO {
		public IntPtr hIcon;
		public int iIcon;
		public uint dwAttributes;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	}
	private const uint SHGFI_ICON = 0x000000100;
	private const uint SHGFI_LARGEICON = 0x000000000;
	private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
	private static int add_system_icon(ImageList imageList, string path) {
		const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
		const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

		uint attributes = Directory.Exists(path) ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;

		SHFILEINFO shinfo;
		SHGetFileInfo(path, attributes, out shinfo, (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
			SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);

		if (shinfo.hIcon != IntPtr.Zero) {
			Icon icon = Icon.FromHandle(shinfo.hIcon).Clone() as Icon;
			DestroyIcon(shinfo.hIcon); // Cleanup native icon
			imageList.Images.Add(path, icon);
			return imageList.Images.IndexOfKey(path);
		}
		return -1;
	}
	[DllImport("User32.dll")]
	private static extern bool DestroyIcon(IntPtr handle);
	public static ListViewItem? get_first_selected(ListView view){
		if (view.SelectedItems.Count == 0) return null;
		return view.SelectedItems[0];
	}
}

public static class Incantation_DIALOG {
	public static string? open_dialog(string extension) {
        using (var openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = $"{extension} files (*.{extension})|*.{extension}|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
        }
        return null;
    }
	public static string? open_dialog(string extension, string starting_dir) {
		using (var openFileDialog = new OpenFileDialog())
		{
			openFileDialog.Filter = $"{extension} files (*.{extension})|*.{extension}|All files (*.*)|*.*";
			openFileDialog.RestoreDirectory = true;

			if (!string.IsNullOrEmpty(starting_dir) && Directory.Exists(starting_dir))
			{
				openFileDialog.InitialDirectory = starting_dir;
			}

			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				return openFileDialog.FileName;
			}
		}
		return null;
	}
	public static string? save_dialog(string extension)	{
		using (var saveFileDialog = new SaveFileDialog())
		{
			saveFileDialog.Filter = $"{extension} files (*.{extension})|*.{extension}|All files (*.*)|*.*";
			saveFileDialog.RestoreDirectory = true;
			saveFileDialog.DefaultExt = extension;

			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				return saveFileDialog.FileName;
			}
		}
		return null;
	}
	public static string? save_dialog(string extension, string starting_dir) {
		using (var saveFileDialog = new SaveFileDialog())
		{
			saveFileDialog.Filter = $"{extension} files (*.{extension})|*.{extension}|All files (*.*)|*.*";
			saveFileDialog.RestoreDirectory = true;
			saveFileDialog.DefaultExt = extension;

			if (!string.IsNullOrEmpty(starting_dir) && Directory.Exists(starting_dir))
			{
				saveFileDialog.InitialDirectory = starting_dir;
			}

			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				return saveFileDialog.FileName;
			}
		}
		return null;
	}
	public static bool move_to_dialog(List<string> fullpaths, string destination) {
        if (fullpaths.Count ==0 ) return false;
		if (!is_dir(destination)) {
			MessageBox.Show("Please select a valid destination folder.");
			return false; 
		}
		if (!confirmation_dialog("Confirmation","Are You Sure to Move/Overwrite these files? This operation will overwrite same filenames, use windows file explorer for full control.")) return false;
		LaunchExplorerCopy(fullpaths, destination, move: true);
		return true;
    }
	public static bool copy_to_dialog(List<string> fullpaths, string destination) {
		if (fullpaths.Count ==0 ) return false;
		if (!is_dir(destination)) {
			MessageBox.Show("Please select a valid destination folder.");
			return false; 
		}
		if (!confirmation_dialog("Confirmation","Are You Sure to Copy/Overwrite these files? This operation will overwrite same filenames, use windows file explorer for full control.")) return false;
        LaunchExplorerCopy(fullpaths, destination, move: false);
		return true;
    }
	public static bool delete_dialog(List<string> fullpaths){
		if (fullpaths.Count ==0 ) return false;
		if (!confirmation_dialog("Confirmation","Are You Sure to Delete These Files?")) return false;
		MessageBox.Show("If the media supports the Files Will be Moved to Trash Bin");
		LaunchExplorerMoveToBinOrDelete(fullpaths);
		return true;
	}
	private static void LaunchExplorerCopy_OLD(List<string> fullpaths, string destination, bool move) {
        if (fullpaths == null || fullpaths.Count == 0)
            throw new ArgumentException("File list cannot be null or empty.", nameof(fullpaths));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Destination path cannot be null or empty.", nameof(destination));
        if (!Directory.Exists(destination))
            throw new ArgumentException($"Destination folder does not exist: {destination}", nameof(destination));

        string tempScript = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ps1");
        string copyMethod = move ? "MoveHere" : "CopyHere";

        try
        {
            // Escape special characters for PowerShell (double quotes and backticks)
            var psFilePaths = fullpaths.Select(f => f.Replace("\"", "`\"").Replace("`", "``"));

            // Build PowerShell array of file paths: @("C:\path\file1", "C:\path\file2", ...)
            string filesArray = "@(" + string.Join(", ", psFilePaths.Select(f => $"\"{f}\"")) + ")";

            // Escape destination path
            string escapedDestination = destination.Replace("\"", "`\"").Replace("`", "``");

            string psScript = $@"
$ErrorActionPreference = 'Stop'
$shell = New-Object -ComObject Shell.Application
$dest = $shell.NameSpace(""{escapedDestination}"")
if ($null -eq $dest) {{
    Write-Error ""Destination folder not found: {escapedDestination}""
    exit 1
}}
$files = {filesArray}
foreach ($file in $files) {{
    $folderPath = Split-Path $file
    $fileName = Split-Path $file -Leaf
    $folder = $shell.NameSpace($folderPath)
    if ($null -eq $folder) {{
        Write-Warning ""Folder not found: $folderPath""
        continue
    }}
    $item = $folder.ParseName($fileName)
    if ($null -eq $item) {{
        Write-Warning ""File not found: $fileName in $folderPath""
        continue
    }}
    $dest.{copyMethod}($item, 16) # 16 = Suppress overwrite prompt
}}
";

            File.WriteAllText(tempScript, psScript, new UTF8Encoding(true));

            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{tempScript}\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true  
                }
            })
            {
                process.Start();
                string errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"PowerShell script failed: {errorOutput}");
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to execute file {copyMethod.ToLower()} operation: {ex.Message}", ex);
        }
        finally
        {
            // Clean up temporary script
            if (File.Exists(tempScript))
            {
                try
                {
                    File.Delete(tempScript);
                }
                catch (Exception ex)
                {
                    // Log cleanup failure if needed, but don't throw
                    Console.WriteLine($"Failed to delete temporary script {tempScript}: {ex.Message}");
                }
            }
        }
    }
	private static void LaunchExplorerCopy(List<string> fullpaths, string destination, bool move) {
		if (fullpaths == null || fullpaths.Count == 0)
			throw new ArgumentException("File list cannot be null or empty.", nameof(fullpaths));

		if (!Directory.Exists(destination))
			throw new ArgumentException("Destination folder does not exist.", nameof(destination));

		Type shellType = Type.GetTypeFromProgID("Shell.Application");
		dynamic shell = Activator.CreateInstance(shellType);
		dynamic destFolder = shell.NameSpace(destination);

		foreach (var path in fullpaths)
		{
			string folderPath = Path.GetDirectoryName(path);
			string fileName = Path.GetFileName(path);

			dynamic folder = shell.NameSpace(folderPath);
			dynamic item = folder?.ParseName(fileName);

			if (item != null)
			{
				if (move)
					destFolder.MoveHere(item, 16);
				else
					destFolder.CopyHere(item, 16);
			}
		}
	}
	private static void LaunchExplorerMoveToBinOrDelete_OLD(List<string> fullpaths)	{
		if (fullpaths == null || fullpaths.Count == 0) throw new ArgumentException("File list cannot be null or empty.", nameof(fullpaths));
		string tempScript = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ps1");
		try {
			// Escape special characters
			var psFilePaths = fullpaths.Select(f => f.Replace("\"", "`\"").Replace("`", "``"));
			string filesArray = "@(" + string.Join(", ", psFilePaths.Select(f => $"\"{f}\"")) + ")";
			// powershell script 
			string psScript = $@"
				$ErrorActionPreference = 'Stop'
				Add-Type -AssemblyName Microsoft.VisualBasic
				$shell = New-Object -ComObject Shell.Application
				$files = {filesArray}

				foreach ($file in $files) {{
					if (-not (Test-Path $file)) {{
						Write-Warning ""File not found: $file""
						continue
					}}

					# Attempt to delete to Recycle Bin using .NET (with UI and fallback)
					try {{
						if ((Get-Item $file).PSIsContainer) {{
							[Microsoft.VisualBasic.FileIO.FileSystem]::DeleteDirectory(
								$file,
								'OnlyErrorDialogs', 
								'SendToRecycleBin'  
							)
						}} else {{
							[Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile(
								$file,
								'OnlyErrorDialogs', 
								'SendToRecycleBin'  
							)
						}}
					}} catch {{
						Write-Warning ""Delete failed: $file. $_""
					}}
				}}
			";

			File.WriteAllText(tempScript, psScript);

			using (var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "powershell.exe",
					Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{tempScript}\"",
					UseShellExecute = false,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			})
			{
				process.Start();
				string errorOutput = process.StandardError.ReadToEnd();
				process.WaitForExit();

				if (process.ExitCode != 0)
				{
					throw new InvalidOperationException($"PowerShell script failed: {errorOutput}");
				}
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to send files to Recycle Bin or delete: {ex.Message}", ex);
		}
		finally
		{
			if (File.Exists(tempScript))
			{
				try { File.Delete(tempScript); }
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to delete temporary script {tempScript}: {ex.Message}");
				}
			}
		}
	}
	private static void LaunchExplorerMoveToBinOrDelete(List<string> fullpaths) {
		if (fullpaths == null || fullpaths.Count == 0)
			throw new ArgumentException("File list cannot be null or empty.", nameof(fullpaths));

		foreach (var path in fullpaths)
		{
			if (!File.Exists(path) && !Directory.Exists(path))
			{
				Console.WriteLine($"File not found: {path}");
				continue;
			}

			try
			{
				if (Directory.Exists(path))
				{
					FileSystem.DeleteDirectory(
						path,
						UIOption.OnlyErrorDialogs,
						RecycleOption.SendToRecycleBin
					);
				}
				else
				{
					FileSystem.DeleteFile(
						path,
						UIOption.OnlyErrorDialogs,
						RecycleOption.SendToRecycleBin
					);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Delete failed: {path}. {ex.Message}");
			}
		}
	}
	public static bool confirmation_dialog(string title, string message) {
		DialogResult result = MessageBox.Show(
			message,
			title,
			MessageBoxButtons.YesNo,
			MessageBoxIcon.Question,
			MessageBoxDefaultButton.Button2
		);

		return result == DialogResult.Yes;
	}
	// >>>
	public static string? input_dialog(Form owner,string title, string message, string default_text) {
		using (Form form = new Form())
		using (Label label = new Label())
		using (TextBox textBox = new TextBox())
		using (Button buttonOk = new Button())
		using (Button buttonCancel = new Button()) {
			form.Text = title;
			form.StartPosition = FormStartPosition.CenterParent;
			form.FormBorderStyle = FormBorderStyle.FixedDialog;
			form.MinimizeBox = false;
			form.MaximizeBox = false;
			form.ShowInTaskbar = false;
			form.ClientSize = new Size(400, 140);

			label.Text = message;
			label.AutoSize = false;
			label.SetBounds(10, 10, 380, 30);

			textBox.SetBounds(10, 45, 380, 23);
			textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
			textBox.Text = default_text;

			buttonOk.Text = "OK";
			buttonOk.DialogResult = DialogResult.OK;
			buttonOk.SetBounds(220, 80, 80, 30);

			buttonCancel.Text = "Cancel";
			buttonCancel.DialogResult = DialogResult.Cancel;
			buttonCancel.SetBounds(310, 80, 80, 30);

			form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
			form.AcceptButton = buttonOk;
			form.CancelButton = buttonCancel;
			
			form.Shown += (s, e) => {
				textBox.Focus();
				textBox.SelectAll();
			};
			
			var result = form.ShowDialog(owner);

			return result == DialogResult.OK ? textBox.Text : null;
		}
	}
	// <<< 
}

// custom widget classes 
public class DarkTabControl : TabControl {
	public DarkTabControl()
	{
		// Enable custom drawing and optimize painting
		this.SetStyle(ControlStyles.UserPaint, true);
		this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		this.DrawMode = TabDrawMode.OwnerDrawFixed;
		this.ItemSize = new Size(100, 24); // Fixed tab size
	}

	protected override void OnDrawItem(DrawItemEventArgs e)
	{
		TabPage tab = this.TabPages[e.Index];
		bool selected = (e.Index == this.SelectedIndex);

		// Define colors to match Notepad_Form theme
		Color tabBackColor = selected ? Color.FromArgb(10, 10, 15) : Color.FromArgb(0, 0, 0);
		Color textColor = Color.FromArgb(220, 220, 220); // Matches Notepad_Form text color
		Color borderColor = Color.FromArgb(0, 0, 255); // Thin border color

		// Draw tab background
		using (SolidBrush brush = new SolidBrush(tabBackColor))
		{
			e.Graphics.FillRectangle(brush, e.Bounds);
		}

		// Draw tab text
		TextRenderer.DrawText(
			e.Graphics,
			tab.Text,
			this.Font,
			e.Bounds,
			textColor,
			TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
		);

		// Draw a thin border around the selected tab
		if (selected)
		{
			using (Pen pen = new Pen(borderColor, 1))
			{
				Rectangle borderRect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
				e.Graphics.DrawRectangle(pen, borderRect);
			}
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		// Clear the entire control with the background color
		using (SolidBrush brush = new SolidBrush(Color.FromArgb(10, 10, 15))) // Matches Notepad_Form background
		{
			e.Graphics.FillRectangle(brush, this.ClientRectangle);
		}

		// Draw the content area (below tabs) with the same background
		Rectangle contentRect = new Rectangle(0, this.ItemSize.Height, this.Width, this.Height - this.ItemSize.Height);
		using (SolidBrush brush = new SolidBrush(Color.FromArgb(10, 10, 15)))
		{
			e.Graphics.FillRectangle(brush, contentRect);
		}

		// Draw a thin border around the content area
		using (Pen pen = new Pen(Color.FromArgb(100, 100, 100), 1))
		{
			Rectangle borderRect = new Rectangle(0, this.ItemSize.Height, this.Width - 1, this.Height - this.ItemSize.Height - 1);
			e.Graphics.DrawRectangle(pen, borderRect);
		}

		// Draw each tab
		for (int i = 0; i < this.TabCount; i++)
		{
			Rectangle tabRect = this.GetTabRect(i);
			DrawItemEventArgs args = new DrawItemEventArgs(
				e.Graphics,
				this.Font,
				tabRect,
				i,
				this.SelectedIndex == i ? DrawItemState.Selected : DrawItemState.Default
			);
			OnDrawItem(args);
		}
	}

	protected override void OnPaintBackground(PaintEventArgs pevent)
	{
		// Do nothing to prevent default background painting (avoids white parts)
	}
}

public class DarkTreeView : MultiSelectTreeView {
	public DarkTreeView() {
		// Use owner-drawn mode for full custom rendering
		this.DrawMode = TreeViewDrawMode.OwnerDrawText;
		this.BackColor = Color.FromArgb(10, 10, 15);
		this.ForeColor = Color.FromArgb(255, 255, 255);
		this.BorderStyle = BorderStyle.None;
		this.HideSelection = false;
		this.FullRowSelect = true;

		this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		this.SetStyle(ControlStyles.ResizeRedraw, true);
	}
	public override void ClearSelectedNodes() {
        foreach (var node in selectedNodes) {
			// InvalidateNode(node);
            node.BackColor = this.BackColor;
            node.ForeColor = DetermineNodeColor(node);
        }
        selectedNodes.Clear();
    }
	protected override void OnDrawNode(DrawTreeNodeEventArgs e)	{
		TreeNode node = e.Node;
		bool isSelected = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected;
		//bool isSelected = SelectedNodes.Contains(e.Node);
		bool isFocused = (e.State & TreeNodeStates.Focused) == TreeNodeStates.Focused;

		// Respect node-assigned colors first
		Color backColor = node.BackColor.IsEmpty ? this.BackColor : node.BackColor;
		Color textColor = node.ForeColor.IsEmpty ? DetermineNodeColor(node) : node.ForeColor;

		// Background
		using (SolidBrush bgBrush = new SolidBrush(backColor))
			e.Graphics.FillRectangle(bgBrush, e.Bounds);

		// Text
		TextRenderer.DrawText(
			e.Graphics,
			node.Text,
			this.Font,
			e.Bounds,
			textColor,
			TextFormatFlags.VerticalCenter | TextFormatFlags.Left
		);

		// Optional: border around focused node
		if (isSelected && isFocused)
		{
			using (Pen pen = new Pen(Color.FromArgb(0, 0, 255)))
			{
				var rect = new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
				e.Graphics.DrawRectangle(pen, rect);
			}
		}
	}
	protected override void RestoreNodeColor(TreeNode node) {
		node.BackColor = this.BackColor;
		node.ForeColor = DetermineNodeColor(node);
	}
	private Color DetermineNodeColor(TreeNode node) {
		var media_color = Color.Red;
		var exe_color = Color.Yellow;
		var special_file_color = Color.Cyan;
		var editable_color = Color.Magenta;
		if (node.Tag is string path)
		{
			if (Directory.Exists(path))
				return Color.FromArgb(0,255,0); // Folder
			if (File.Exists(path)) {
				string ext = get_extension(path);
				switch (ext) { 
					case ".exe": return exe_color;
					case ".ini": return special_file_color; 
					case ".json": return special_file_color;
					case ".bat": return special_file_color;
					case ".dll": return special_file_color; 
					case ".txt": return editable_color; 
					case ".pdf": return media_color; 
					case ".mp3": return media_color;
					case ".mp4": return media_color;
					case ".mkv": return media_color;
				}
				return Color.FromArgb(255,255,255);     // File
			} 
		}
		// Default
		return this.ForeColor;
	}
	protected override void OnPaintBackground(PaintEventArgs pevent) {
		// Paint custom background to prevent default flicker
		using (SolidBrush bg = new SolidBrush(this.BackColor))
		{
			pevent.Graphics.FillRectangle(bg, this.ClientRectangle);
		}
	}
}

public class MultiSelectTreeView : TreeView {
	protected bool suppressSelection = false;
    protected readonly List<TreeNode> selectedNodes = new List<TreeNode>();
    protected TreeNode? lastSelectedNode = null;
	protected Color selected_back_color = Color.Blue;

    public IReadOnlyList<TreeNode> SelectedNodes => selectedNodes.AsReadOnly();

    public MultiSelectTreeView() {
        this.HideSelection = false;
    }

    protected override void OnBeforeSelect(TreeViewCancelEventArgs e) {
        if (suppressSelection) e.Cancel = true;
        base.OnBeforeSelect(e);
    }

    protected override void OnMouseDown(MouseEventArgs e) {
		if (e.Button != MouseButtons.Left) {
			base.OnMouseDown(e);
			return;
		}		
		suppressSelection = true;
        TreeNode? clickedNode = this.GetNodeAt(e.Location);
        if (clickedNode == null) return;

        if (ModifierKeys.HasFlag(Keys.Shift) && lastSelectedNode != null) {
            // SHIFT: Select range
            var range = GetNodeRange(lastSelectedNode, clickedNode);
            ClearSelectedNodes();
            foreach (var node in range) {
                AddNodeToSelection(node);
            }
        }
        else if (ModifierKeys.HasFlag(Keys.Control)) {
            // CTRL: Toggle selection
            if (selectedNodes.Contains(clickedNode)) {
                this.RemoveNodeFromSelection(clickedNode);
            } else {
                AddNodeToSelection(clickedNode);
                lastSelectedNode = clickedNode;
            }
        }
        else {
            // No modifier: Single selection
            ClearSelectedNodes();
            AddNodeToSelection(clickedNode);
            lastSelectedNode = clickedNode;
        }

        base.OnMouseDown(e);
		suppressSelection = false;
    }

    public virtual void ClearSelectedNodes() {
        foreach (var node in selectedNodes) {
			// InvalidateNode(node);
            node.BackColor = this.BackColor;
            node.ForeColor = this.ForeColor;
        }
        selectedNodes.Clear();
    }

    private void AddNodeToSelection(TreeNode node) {
        if (!selectedNodes.Contains(node)) {
            selectedNodes.Add(node);
            node.BackColor = this.selected_back_color; 
            node.ForeColor = SystemColors.HighlightText;
        }
    }

    protected virtual void RestoreNodeColor(TreeNode node) {
		node.BackColor = this.BackColor;
		node.ForeColor = this.ForeColor; 
	}

	protected virtual void RemoveNodeFromSelection(TreeNode node) {
		if (selectedNodes.Remove(node)) {
			this.RestoreNodeColor(node);
		}
	}

    private List<TreeNode> GetNodeRange(TreeNode start, TreeNode end) {
        List<TreeNode> allNodes = GetAllNodes(visibleOnly: true);
        int iStart = allNodes.IndexOf(start);
        int iEnd = allNodes.IndexOf(end);

        if (iStart < 0 || iEnd < 0) return new List<TreeNode>();

        if (iStart > iEnd) {
            var tmp = iStart;
            iStart = iEnd;
            iEnd = tmp;
        }

        return allNodes.GetRange(iStart, iEnd - iStart + 1);
    }

    private List<TreeNode> GetAllNodes(bool visibleOnly) {
        List<TreeNode> list = new List<TreeNode>();
        foreach (TreeNode root in this.Nodes) {
            CollectNodes(root, list, visibleOnly);
        }
        return list;
    }

    private void CollectNodes(TreeNode node, List<TreeNode> list, bool visibleOnly) {
        list.Add(node);
        if (node.IsExpanded || !visibleOnly) {
            foreach (TreeNode child in node.Nodes) {
                CollectNodes(child, list, visibleOnly);
            }
        }
    }
	
	protected override void OnAfterSelect(TreeViewEventArgs e) {
		// Do NOT modify custom selection on keyboard input
		// Let TreeView.SelectedNode update naturally
		base.OnAfterSelect(e);
	}
	
	protected override void OnKeyDown(KeyEventArgs e) {
		bool isArrowKey = e.KeyCode == Keys.Up || e.KeyCode == Keys.Down;

		if (!isArrowKey) {
			base.OnKeyDown(e);
			return;
		}

		TreeNode? anchor = lastSelectedNode ?? this.SelectedNode;

		// Let TreeView process the key normally first
		base.OnKeyDown(e);

		// Delay custom logic to run *after* TreeView updates SelectedNode
		this.BeginInvoke((MethodInvoker)delegate {
			TreeNode? focused = this.SelectedNode;
			if (focused == null) return;

			if (e.Shift && anchor != null) {
				// SHIFT: range from anchor to new focus
				var range = GetNodeRange(anchor, focused);
				ClearSelectedNodes();
				foreach (var node in range)
					AddNodeToSelection(node);
			}
			else if (!e.Control && !e.Shift) {
				// No modifiers: reset selection to focused node
				ClearSelectedNodes();
				AddNodeToSelection(focused);
				lastSelectedNode = focused;
			}
			else if (!e.Shift) {
				// CTRL only: update anchor but don't select
				lastSelectedNode = focused;
			}
		});
	}

	public List<TreeNode> GetSelectedNodes() {
		return new List<TreeNode>(selectedNodes);
	}
}

public class FilePreviewTooltip : Form {
	// if an image file show a tooltip (the form) of that image 
	// else show a tooltip of an icon 
	public FilePreviewTooltip(int pixel_area){}
}

public class ToggleablePanel : Panel {
    private int current_index = 0;
    private List<Control> toggleable_controls = new List<Control>();
	public event EventHandler<Control> ControlChanged;
    public void SetControls(List<Control> controls) {
        toggleable_controls.Clear();
        this.Controls.Clear();
        current_index = 0;

        bool first = true;
        foreach (var control in controls) {
            control.Dock = DockStyle.Fill;
            this.Controls.Add(control);
            toggleable_controls.Add(control);
            control.Visible = first;
            first = false;
        }
    }
    public void Toggle() {
        if (toggleable_controls.Count == 0) return;
        current_index = (current_index + 1) % toggleable_controls.Count;
        ShowControl(current_index);
    }
    public void ShowControl(int index) {
        if (index < 0 || index >= toggleable_controls.Count) return;
        current_index = index;
        ShowControl(toggleable_controls[index]);
    }
    public void ShowControl(Control controlToShow) {
		if (!toggleable_controls.Contains(controlToShow)) return;
        foreach (Control ctrl in toggleable_controls) {
            ctrl.Visible = (ctrl == controlToShow);
        }
		int last_index = current_index;
		current_index = toggleable_controls.IndexOf(controlToShow);
        controlToShow.BringToFront();
		if (last_index != current_index) OnControlChanged(controlToShow); 
    }
    public void Previous() {
        if (toggleable_controls.Count == 0) return;
        current_index = (current_index - 1 + toggleable_controls.Count) % toggleable_controls.Count;
        ShowControl(current_index);
    }
	public void Next() => Toggle();
	protected virtual void OnControlChanged(Control control) {
		ControlChanged?.Invoke(this, control);
	}
}

// ===================================== conjuration 
// ... system integration  
public static class Conjuration {
	public static bool default_program_start(string filename) {
		if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
			return false;

		try {
			ProcessStartInfo psi = new ProcessStartInfo {
				FileName = filename,
				UseShellExecute = true // Required to launch with default program
			};
			Process.Start(psi);
			return true;
		}
		catch {
			return false; // Handle errors like no association, permissions, etc.
		}
	}
	public static bool open_in_windows_explorer(string filename) {
		if (string.IsNullOrWhiteSpace(filename))
			return false;

		try {
			if (Directory.Exists(filename)) {
				// It's a folder: open it directly
				Process.Start("explorer.exe", $"\"{filename}\"");
			}
			else if (File.Exists(filename)) {
				// It's a file: select it in Explorer
				Process.Start("explorer.exe", $"/select,\"{filename}\"");
			}
			else {
				return false; // File or folder does not exist
			}
			return true;
		}
		catch {
			return false;
		}
	}
	public static bool open_in_cmd(string filename) {
		if (string.IsNullOrWhiteSpace(filename))
			return false;

		try {
			string? directory = null;

			if (Directory.Exists(filename)) {
				directory = filename;
			}
			else if (File.Exists(filename)) {
				directory = Path.GetDirectoryName(filename);
			}

			if (directory != null) {
				Process.Start(new ProcessStartInfo {
					FileName = "cmd.exe",
					Arguments = $"/K cd /d \"{directory}\"",
					UseShellExecute = true
				});
				return true;
			}

			return false; // File or folder doesn't exist
		}
		catch {
			return false;
		}
	}
	public static bool default_program_edit(string filename) {
		if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename)) return false;
		try {
			ProcessStartInfo psi = new ProcessStartInfo {
				FileName = filename,
				Verb = "edit",
				UseShellExecute = true // Required to launch with default program
			};
			Process.Start(psi);
			// return true;
			// var psi = new ProcessStartInfo(filename) {
				// UseShellExecute = true
			// };
			// if (!psi.Verbs.Contains("edit", StringComparer.OrdinalIgnoreCase)) return false;
			// psi.Verb = "edit";
			// Process.Start(psi);
			return true;
		}
		catch (Exception ex) {
			MessageBox.Show(
				$"Failed to open file for editing.\n\nFile:\n{filename}\n\nError:\n{ex.Message}",
				"Edit Error",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error
			);
			return false;
		}

			
	}
	
	// >>> 
    /*
	public static void toggle_internet(string interfaceName) {
		try
		{
			string query = $"SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID = '{interfaceName.Replace("'", "''")}'";

			using (var searcher = new ManagementObjectSearcher(query))
			using (var collection = searcher.Get())
			{
				var adapter = collection.Cast<ManagementObject>().FirstOrDefault();

				if (adapter == null)
				{
					Console.WriteLine($"Interface '{interfaceName}' not found.");
					return;
				}

				bool isEnabled = adapter["NetEnabled"] != null && (bool)adapter["NetEnabled"];

				uint result = (uint)adapter.InvokeMethod(isEnabled ? "Disable" : "Enable", null);

				if (result != 0)
					throw new InvalidOperationException($"Operation failed. Error code: {result}");

				// 🔎 WAIT UNTIL STATE ACTUALLY CHANGES
				WaitForAdapterState(adapter, !isEnabled);

				Console.WriteLine($"Interface '{interfaceName}' {(isEnabled ? "disabled" : "enabled")}.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error toggling interface: {ex.Message}");
		}
	}
	private static void WaitForAdapterState(ManagementObject adapter, bool expectedState) {
		const int timeoutMs = 8000; // 8 seconds max
		const int pollInterval = 300;

		int waited = 0;

		while (waited < timeoutMs)
		{
			adapter.Get(); // Refresh WMI object

			bool currentState = adapter["NetEnabled"] != null && (bool)adapter["NetEnabled"];

			if (currentState == expectedState)
				return;

			Thread.Sleep(pollInterval);
			waited += pollInterval;
		}

		throw new TimeoutException("Adapter state change timed out.");
	}
	public static List<string> get_network_interfaces() {
		var interfaces = new List<string>();
		string query = @"
			SELECT NetConnectionID 
			FROM Win32_NetworkAdapter 
			WHERE NetConnectionID IS NOT NULL 
			AND (AdapterTypeID = 0 OR AdapterTypeID = 9)
			AND PhysicalAdapter = True";

		using (var searcher = new ManagementObjectSearcher(query))
		using (var results = searcher.Get())
		{
			foreach (ManagementObject adapter in results)
			{
				string name = adapter["NetConnectionID"]?.ToString();
				if (!string.IsNullOrWhiteSpace(name))
					interfaces.Add(name);
			}
		}

		return interfaces;
	}
    public static bool ask_for_elevation(Form main_form) {
        using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent()) {
            var principal = new System.Security.Principal.WindowsPrincipal(identity);

            if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                return true;
        }
        try {
            var psi = new System.Diagnostics.ProcessStartInfo {
                FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                UseShellExecute = true,
                Verb = "runas"
            };
            System.Diagnostics.Process.Start(psi);
            main_form.Close();
            return false;
        }
        catch
        {
            return false; // User cancelled UAC
        }
    }
	*/
	// <<< 
	
	// >>>
	public static bool rename_file(string filename, string new_name) {
		if (string.IsNullOrWhiteSpace(filename)) return false;
		if (string.IsNullOrWhiteSpace(new_name)) return false;
		try {
			string fullPath = Path.GetFullPath(filename);
			if (!File.Exists(fullPath)) return false; 
			string extension = Path.GetExtension(fullPath);
			if ( !Path.HasExtension(new_name) ) new_name += extension;
			if (new_name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
			string directory = Path.GetDirectoryName(fullPath)!;
			string newFullPath = Path.Combine(directory, new_name);
			if (File.Exists(newFullPath)) return false;
			File.Move(fullPath, newFullPath);
			return true;
		} catch (UnauthorizedAccessException) {
			return false;
		} catch (IOException) {
			return false;
		} catch {
			return false;
		}
	}
	// <<<
}








// -- END