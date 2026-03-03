namespace FileExplorer;
using Codex;
using static Codex.Transmutation;
using static Codex.Incantation;
using static Codex.Incantation_TREEVIEW;
using static Codex.Incantation_LISTVIEW;
using static Codex.Incantation_DIALOG;
using static Codex.Conjuration;
using System; 
using System.Text.Json;
using System.IO;
using System.IO.Compression;

public class Form_DATA {
    public int Width { get; set; }
    public int Height { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
	public List<string> Directories { get; set; } = new List<string>();
	public List<string> Shortcuts { get; set; } = new List<string>();
	public bool Docking { get; set; }
	public bool HideTitleBar { get; set; }
	public string Filters { get; set; }
    public Form_DATA() {
        Width = 800;
        Height = 600;
        X = 20;
        Y = 20;
		Directories = new List<string>();
		Shortcuts = new List<string>();
		Docking = false;
		HideTitleBar = false;
		Filters = "";
    }
}

public partial class FileExplorer_Form : Form { 
	private Form_DATA data;
	private SplitContainer main_panel;
	private DarkTabControl tabs;
	private readonly float dock_perc_active = 0.8f;
	private readonly float dock_perc_inactive = 0.08f;
	// explorer 
	private TabPage explorer_page;
	private TableLayoutPanel explorer_panel;
	private TextBox textbox_filters;
	private DarkTreeView explorer;
	private ContextMenuStrip explorer_context_menu;
	private readonly HashSet<TreeNode> dirty_nodes = new();
	private ListView shortcuts;
	private ContextMenuStrip shortcuts_context_menu;
	private List<string> filters = new List<string>();
	private TreeNode? last_pointed_node = null;
	// selected files 
	private TabPage selected_page;
	private TableLayoutPanel selected_panel;
	private ListView selected_files;
	private FlowLayoutPanel selected_buttons_panel;
	private ContextMenuStrip selected_context_menu;
	// log
	private ListView log_list;
	private ContextMenuStrip log_list_context_menu;
	private string last_log_text = "";
	// -- 
	private bool long_op_topmost;
	// -- 
    public FileExplorer_Form() { 
		InitializeComponent(); // designer
		// 
		this.data = new_object_from_json<Form_DATA>(join(get_exec_dir(), "FileExplorer.sav"));
		_components(); 
		_layout(); 
		_logic(); 
		_theme(); 
	}
	private void _components() {
		this.tabs = new_dark_tabs();
		SBR_selected_tab_components();
		SBR_explorer_tab_components();
		SBR_log_components();
		add_log(this.log_list,"FileExplorer Started");
	}
	private void _layout() {
		this.StartPosition = FormStartPosition.Manual;
		this.Size = new Size(this.data.Width, this.data.Height);
		this.Location = new Point(this.data.X, this.data.Y);
		// 
		this.explorer_panel.Dock = DockStyle.Fill;
		set_as_vertical_flow_layout(this.explorer_panel, new List<float>{4F,80F,16F});
		this.explorer_panel.Controls.Add( this.textbox_filters );
		this.explorer_panel.Controls.Add( this.explorer );
		this.explorer_panel.Controls.Add( this.shortcuts );
		//
		this.selected_panel.Dock = DockStyle.Fill;
		set_as_vertical_flow_layout(this.selected_panel, new List<float>{0F, 100F});
		this.selected_panel.Controls.Add(this.selected_buttons_panel, 0, 0);
		this.selected_files.Dock = DockStyle.Fill;
		this.selected_panel.Controls.Add(this.selected_files, 0, 1);
		// 
		this.main_panel = new_vertical_split(this.tabs, this.log_list);
		this.main_panel.Dock = DockStyle.Fill;
		this.Controls.Add(this.main_panel);
	}
	private void _logic() {
		// form 
		this.DoubleBuffered = true;
		this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		this.FormClosing += (s, e) => {
			SBR_undock();
			this.data.Width = this.Width;
			this.data.Height = this.Height;
			this.data.X = this.Location.X;
			this.data.Y = this.Location.Y;
			this.data.Filters = this.textbox_filters.Text;
			save(
				join(get_exec_dir(), "FileExplorer.sav"), 
				JsonSerializer.Serialize(this.data)
			);
		};
		this.Activated += (s,e) => {
			this.Opacity = 0.85;
			SBR_refresh_last_pointed_node();
			SBR_dock_active(); 
		};
		this.Deactivate += (s,e) => {
			this.Opacity = 0.35;
			// collapse(this.explorer,1); // this.explorer.CollapseAll();
			SBR_dock_inactive();
		};
		// main_panel.Panel1
		drag_window(this.main_panel.Panel1);
		on_double_click(this.main_panel.Panel1, ()=>{
			SBR_toggle_title_bar();
		});
		// filters 
		this.textbox_filters.TextChanged += (s,e) => {
			this.filters = get_tokens(this.textbox_filters);
		};
		this.textbox_filters.KeyDown += (s,e) => {
			if (
				e.KeyCode == Keys.Space || 
				e.KeyCode == Keys.Enter || 
				e.KeyCode == Keys.Tab
			) {
				SBR_refresh_last_pointed_node();
			} 
		};
		// explorer 
		// this.explorer_panel.GotFocus += (s, e) => {			
			// Console.WriteLine("Focused: " + this.explorer.Focused);
			// Console.WriteLine("SelectedNode: " + this.explorer.SelectedNode);
			// if (this.explorer.SelectedNode == null)
			// {
				// if (this.last_pointed_node != null)
					// this.explorer.SelectedNode = this.last_pointed_node;
				// else if (this.explorer.Nodes.Count > 0)
					// this.explorer.SelectedNode = this.explorer.Nodes[0];
			// }
		// };
		set_as_filesystem_tree(this.explorer);
		this.explorer.KeyDown += (s,e) => {
			if (e.KeyCode == Keys.Enter) {
				SBR_select_files();
				return ;
			}
			if (e.KeyCode == Keys.Delete) {
				SBR_rem_selected_dir_to_exp();
				return ;
			}
			if (e.KeyCode == Keys.Insert) {
				SBR_add_selected_dir_to_exp();
				return ;
			}
			if (e.KeyCode == Keys.Right) {
				var nodes = this.explorer.GetSelectedNodes();
				if (nodes.Count == 1) {
					var node = nodes[0];
					if (node != null && node.Tag is string path) {
						if ( is_dir(path) ) {
							filter_filesystem_node(node, this.filters);
						} 
					}
					SBR_copy_file_to_clipboard(node);
					SBR_log_selected_pointed_file_dir();
				}
			}
		}; 
		var exp_menu = this.explorer_context_menu;
		set_action(exp_menu, "Select Files", (s,e) => {
			SBR_select_files();
		});
		set_action(exp_menu, "Move", (s,e) => {
			SBR_move_selected_files();
		});
		set_action(exp_menu, "Paste", (s,e) => {
			SBR_paste_copy_selected_files();
		});
		set_action(exp_menu, "Add Selected Directory", (s,e) => {
			SBR_add_selected_dir_to_exp();
		});
		set_action(exp_menu, "Remove Selected Directory", (s,e) => {
			SBR_rem_selected_dir_to_exp();
		});
		set_action(exp_menu, "Open/Execute", (s,e) => {
			string path = get_path_from_selected_node();
			if (string.IsNullOrEmpty(path)) return ;
			string ext = get_extension(path);
			bool is_exe = ( ext==".exe" || ext==".bat" );
			if (is_exe && !confirmation_dialog("Confirm","Are You sure to Execute/Open :"+path)) return ;
			SBR_open_execute(path);
		});
		set_action(exp_menu, "Edit", (s,e)=> {
			string path = get_path_from_selected_node();
			if (string.IsNullOrEmpty(path)) return ;
			string ext = get_extension(path);
			bool is_exe = ( ext==".exe" || ext==".bat" );
			if (is_exe) return ;
			if ( !confirmation_dialog("Confirm","Are You sure to Edit this File > "+path)) return ;
			SBR_edit(path);
		});
		set_action(exp_menu, "Open CMD", (s,e) => {
			string path = get_path_from_selected_node();
			if (string.IsNullOrEmpty(path)) return ;
			if ( !is_dir(path) && !is_file(path) ) return ;
			open_in_cmd(path);
		});
		set_action(exp_menu, "Add Executable to Shortcuts", (s,e) => {
			string path = get_path_from_selected_node();
			if ( string.IsNullOrEmpty(path) ) return ;
			string ext = get_extension(path);
			if (string.IsNullOrWhiteSpace(ext)) return ;
			if (
				ext!=".bat" 
				&& ext!=".exe" 
				&& ext!=".lnk" 
				&& ext!=".msc" 
			) return ;
			add_to_list_filesystem_view_icons(this.shortcuts, path);
			this.data.Shortcuts.Add(path);
		});
		set_action(exp_menu, "Docking Toggle", (s,e)=>{
			SBR_dock_toggle();
		});
		set_action(exp_menu, "Toggle Title Bar", (s,e)=>{
			SBR_toggle_title_bar();
		});
		set_action(exp_menu, "Create a Zip Backup", (s,e)=>{
			BEGIN_LO();
			var nodes = this.explorer.GetSelectedNodes();
			if (nodes.Count != 1) {
				MessageBox.Show("Please Select Only One File or Folder");
				END_LO(); return;
			}

			// -- 
			if (nodes[0].Tag is string path && (Directory.Exists(path) || File.Exists(path) )) { 
				// MessageBox.Show(path);
				if (string.IsNullOrEmpty(path)) {
					END_LO();
					MessageBox.Show("Invalid Path");
					return;
				}
				if ( !is_dir(path) && !is_file(path) ) {
					END_LO();
					MessageBox.Show("Is not a Directory or Folder");
					return ;
				}
				if ( confirmation_dialog(
					"Confirmation", 
					"Are you Sure to Create this Backup from: "+path
				) ) {  
					if ( create_backup_zip(path) ) {
						dirty_nodes.Add(nodes[0].Parent);
						add_log_color(this.log_list, "Backup Created > "+path, Color.Cyan);
					} else {
						MessageBox.Show("Failed to Create a Backup");
					}
				}
			}
			END_LO(); 
		});
		set_action(exp_menu, "Rename File", (s,e)=>{
			var nodes = this.explorer.GetSelectedNodes();
			if (nodes.Count != 1) {
				MessageBox.Show("Please Select One and Only One File to Rename");
				return;
			}
			string path = get_path_from_selected_node();
			string old_filename = get_filename(path);
			if (string.IsNullOrEmpty(path)) return ;
			string new_filename = input_dialog(
				this, 
				"Rename File", 
				"New Filename :", 
				old_filename
			);
			if (string.IsNullOrEmpty(new_filename)) return ;
			if ( !rename_file(path, new_filename) ) {
				MessageBox.Show("Failed to Rename File"); 
				return ;
			}
			add_log_color( this.log_list, old_filename+" > "+new_filename,Color.Magenta );
		});
		this.explorer.MouseClick += (s,e) => {
			SBR_refresh_pointed_dir();
			SBR_copy_file_to_clipboard();
			SBR_log_selected_pointed_file_dir();
		};
		// shortcuts 
		var short_menu = this.shortcuts_context_menu;	
		set_action(short_menu, "Open", (s,e) => {
			var item = get_first_selected(this.shortcuts);
			if (item==null) return ;
			if (item.Tag is string path) {
				if (string.IsNullOrEmpty(path)) return ;
				if (!confirmation_dialog("Confirm","Are You sure to Execute/Open :"+path)) return ;
				SBR_open_execute(path);
			}
		});
		set_action(short_menu, "Remove From Shortcuts", (s,e) => {
			var item = get_first_selected(this.shortcuts);
			if (item==null) return ;
			this.shortcuts.Items.Remove(item);
			if (item.Tag is string path) this.data.Shortcuts.Remove(path);
		});
		this.shortcuts.DoubleClick += (s,e) => {
			var item = get_first_selected(this.shortcuts);
			if (item==null) return ;
			if (item.Tag is string path) {
				if (string.IsNullOrEmpty(path)) return ;
				SBR_open_execute(path);
			}
		};
		// selected 
		this.selected_files.MultiSelect = true;
		var buttons = get_list<Button>(this.selected_buttons_panel);
		var button_remove_selected = buttons.Get(0);
		if(button_remove_selected!=null) {
			button_remove_selected.Click += (s,e) => {
				SBR_remove_selected_selected_files();
			};
		}
		var button_clear = buttons.Get(1);
		if(button_clear!=null) {
			button_clear.Click += (s,e) => {
				SBR_clear_selected_files();
			};
		}
		var button_op_mul = buttons.Get(2);
		if(button_op_mul!=null) {
			button_op_mul.Click += (s,e) => {
				SBR_button_op_mul();
			};
		}
		set_action(this.selected_context_menu, "Trash", (s,e)=>{
			SBR_delete_selected_files();
		});
		set_action(this.selected_context_menu, "Open Multiple Selected", (s,e) => {
			SBR_open_multiple_files();
		});
		// log_list 
		set_action(this.log_list_context_menu, "Clear", (s,e)=>{
			this.log_list.Items.Clear();
			add_log(
				this.log_list, 
				"Log Cleared"
			);
		});
	}
	private void _theme() {
		Color background = Color.FromArgb(10, 10, 15);
		Color text = Color.FromArgb(255, 255, 255);
		// Set colors for the form
		this.Opacity = 0.85;
		apply_theme_recursive(this, background, text);
		set_dark_theme(this.selected_files);
		set_dark_theme(this.log_list);
	}
	// -- subroutines 
	private void SBR_explorer_tab_components() {
		this.explorer_page = add_tab<TableLayoutPanel>(this.tabs, "Explorer");
		this.explorer_panel = get_first<TableLayoutPanel>(explorer_page);
		this.textbox_filters = new_text_box();
		this.textbox_filters.Text = this.data.Filters;
		this.filters = get_tokens(this.textbox_filters);
		this.explorer = new_dark_tree("Explorer");
		foreach( string path in get_drives() ){
			join( this.explorer, new_multiselection_tree(path) );
		}
		join(this.explorer, new_dummy_tree("*"));
		foreach( string path in this.data.Directories ){
			if (! is_dir(path)) continue ;
			join( this.explorer, new_multiselection_tree(path) );
		}
		this.explorer_context_menu = add_context_menu(this.explorer, new List<object>{
			"Select Files",
			new_submenu("Explorer Operations", new List<object>{
				"Add Selected Directory to Explorer",
				"Remove Selected Directory from Explorer", 
				"Add Executable to Shortcuts",
			}),
			new_submenu("File Operations", new List<object>{
				"Open/Execute",
				// "Edit",
				"Open CMD",
				"Create a Zip Backup",
				"Rename File",
				"Paste Copy of Selected Files",
				"Move Selected Files"		
			}),
			"Docking Toggle",
			"Toggle Title Bar"
		});
		this.shortcuts = new_file_list_icons();
		foreach(var path in this.data.Shortcuts){
			if (!is_file(path) ) continue ;
			add_to_list_filesystem_view_icons(this.shortcuts, path);
		}
		this.shortcuts.MultiSelect = false;
		this.shortcuts_context_menu = add_context_menu(this.shortcuts, new List<object>{
			"Open",
			"Remove From Shortcuts"
		});
	}
	private void SBR_selected_tab_components() {
		this.selected_page = add_tab<TableLayoutPanel>(this.tabs, "Selected");
		this.selected_panel = get_first<TableLayoutPanel>(selected_page);
		var button_labels = new List<string> {
			"Remove Selected", 
			"Clear",
			"Open Multiple Files"
		};
		var buttons = new_dark_button_list(button_labels);
		this.selected_buttons_panel = new_horizontal_panel(buttons);
		this.selected_files = new_file_list();
		this.selected_context_menu = add_context_menu(this.selected_files, new List<object>{
			"Open Multiple Selected",
			"Send to Trash/Delete Files"
		});
	}
	private void SBR_log_components(){
		this.log_list = new_log_list();
		this.log_list_context_menu = add_context_menu(this.log_list, new List<object>{
			"Clear"
		});
	}
	private void SBR_select_files(){
		var str_list = get_fullpath( this.explorer.GetSelectedNodes() );
		if (str_list.Count == 0) return ;
		foreach (var path in str_list) {
			add_to_list_filesystem_view(this.selected_files, path);
		}
		
		// Mark immediate parent folders of the selected nodes as dirty
		foreach (var node in this.explorer.GetSelectedNodes()) {
			if (node?.Parent != null && node.Parent.Tag is string parentPath && Directory.Exists(parentPath)) {
				dirty_nodes.Add(node.Parent);
			}
		}
		
		add_log(this.log_list, to_string(str_list.Count)+" selected files");
	}
	private void SBR_move_selected_files(){
		BEGIN_LO();
		// destination selection 
		var nodes = this.explorer.GetSelectedNodes();
		if (nodes.Count != 1) {
			MessageBox.Show("Please Select Only One Destination Folder");
			END_LO();
			return;
		}
		
		// check if there is selected files
		var str_list = get_fullpath( this.selected_files );
		if (str_list == null || str_list.Count == 0){ 
			MessageBox.Show("No Selected Files"); 
			END_LO();
			return ; 
		}
		
		// set destination 
		string? dest = null;
		if (nodes[0].Tag is string path && Directory.Exists(path)) dest = path;
		if (dest == null) {
			MessageBox.Show("Selected node is not a folder.");
			END_LO();
			return;
		}
		
		if (string.IsNullOrWhiteSpace(dest)) {
			MessageBox.Show("Selected node is not a valid folder.");
			END_LO();
			return;
		}
		
		// move 
		try {
			if ( !move_to_dialog(str_list, dest) ) {
				END_LO();
				return;
			}
		} catch (Exception ex) {
			string errorMessage = ex.Message;
			MessageBox.Show("Error :"+errorMessage);
		}
		dirty_nodes.Add(nodes[0]);
		add_log_color(this.log_list, "Files Moved to > "+dest,rgb(0,255,255));
		SBR_refresh_dirty_nodes();
		SBR_clear_selected_files();
		END_LO();
	} 
	private void SBR_paste_copy_selected_files(){
		BEGIN_LO();
		// destination selection 
		var nodes = this.explorer.GetSelectedNodes();
		if (nodes.Count != 1) {
			MessageBox.Show("Please Select Only One Destination Folder");
			END_LO();
			return;
		}
		
		// check if there is selected files
		var str_list = get_fullpath( this.selected_files );
		if (str_list == null || str_list.Count == 0) { 
			MessageBox.Show("No Selected Files"); 
			END_LO();
			return ; 
		}
		
		// set destination 
		string? dest = null;
		if (nodes[0].Tag is string path && Directory.Exists(path)) dest = path;
		if (dest == null) {
			MessageBox.Show("Selected node is not a folder.");
			END_LO();
			return;
		}
		
		if (string.IsNullOrWhiteSpace(dest)) {
			MessageBox.Show("Selected node is not a valid folder.");
			END_LO();
			return;
		}
		
		// copy 
		try {
			if ( !copy_to_dialog(str_list, dest) ){
				END_LO();
				return;
			}
		} catch (Exception ex) {
			string errorMessage = ex.Message;
			MessageBox.Show("Error :"+errorMessage);
		}
		dirty_nodes.Add(nodes[0]);
		add_log_color(this.log_list, "Files Copied to > "+dest, rgb(255,100,0));
		SBR_refresh_dirty_nodes();
		SBR_clear_selected_files();
		END_LO();
	}
	private void SBR_refresh_dirty_nodes() {
		// update dirty nodes 
		foreach (var node in dirty_nodes.ToList()) {
			refresh_filesystem_node(node);
		}
		dirty_nodes.Clear();
	}
	private void SBR_clear_selected_files(){
		this.selected_files.Items.Clear();
		add_log(
			this.log_list, 
			"clear selected files"
		);
	}
	private void SBR_remove_selected_selected_files() {
		remove_selected(this.selected_files);
		add_log(
			this.log_list, 
			to_string(this.selected_files.Items.Count)+" selected files update"
		);
	}
	private string? get_path_from_selected_node(){
		TreeNode node = this.explorer.SelectedNode; 
		if (node == null) return null;
		if (node.Tag == null) return null;
		if (node.Tag is string path){
			return path;
		} else {
			return null;
		}
	}
	private void SBR_open_execute(string path){
		if (string.IsNullOrEmpty(path)) return ;
		if (is_dir(path)) {
			open_in_windows_explorer(path);
			add_log_color(
				this.log_list,
				"Directory Opened > "+path,
				rgb(0,255,0)
			);
		} else if (is_file(path)) {
			default_program_start(path);
			add_log_color(
				this.log_list,
				"Executed/Opened > "+path,
				Color.Yellow
			);
		}
		
	}
	private void SBR_edit(string path){
		if (is_file(path)) {
			if ( !default_program_edit(path) ) {
				MessageBox.Show("Error");
				return; 
			}
			add_log_color(
				this.log_list,
				"Open to Edit > "+path,
				Color.Yellow
			);
		}
	}
	private void SBR_dock_active() {
		if (!this.data.Docking) return ;
		this.TopMost = true;
		this.FormBorderStyle = FormBorderStyle.None;
		var rect = get_dock_rect(this, 0.8f, this.dock_perc_active, "north" );
		animated_resize(this, rect, 10, 5);
	}
	private void SBR_dock_inactive() {
		if (!this.data.Docking) return ;
		this.TopMost = true;
		this.FormBorderStyle = FormBorderStyle.None;
		var rect = get_dock_rect(this, 0.8f, this.dock_perc_inactive, "north" );
		animated_resize(this, rect, 10, 1);
	}
	private void SBR_undock() {
		this.TopMost = false;
		this.FormBorderStyle = FormBorderStyle.Sizable;
		animated_resize(this, new Rectangle(
			this.data.X, 
			this.data.Y, 
			this.data.Width, 
			this.data.Height
		), 10, 5);
	}
	private void SBR_dock_toggle(){
		if (this.data.Docking) {
			this.data.Docking = false;
			SBR_undock();
		} else {
			this.data.Docking = true;
			SBR_dock_active();
		}
	}
	private void BEGIN_LO(){
		this.long_op_topmost = this.TopMost;
		this.TopMost = false; 
	}
	private void END_LO(){
		this.TopMost = this.long_op_topmost;
	}
	private void SBR_delete_selected_files() {
		BEGIN_LO();
		var paths = get_fullpath_selected(this.selected_files);
		if (!delete_dialog(paths)){ 
			END_LO();
			return; 
		}
		add_log_color(
			this.log_list, 
			to_string(this.selected_files.SelectedItems.Count)+" files deleted/moved to trash",
			rgb(255,0,0)
		);
		SBR_remove_selected_selected_files();
		SBR_refresh_dirty_nodes();
		END_LO();
	}
	private bool create_backup_zip(string f_path) {
		try {
			// 1) Check if folder exists
			if (string.IsNullOrWhiteSpace(f_path)){
				MessageBox.Show("Invalid Path");
				return false;
			}
			
			bool isDir = is_dir(f_path);
			bool isFile = is_file(f_path);
			
			if (! (isDir || isFile) ) {
				MessageBox.Show("Invalid File or Folder");
				return false;
			}

			// 2) Get parent folder
			var parentDir = get_dir(f_path);
			if (parentDir == null) {
				MessageBox.Show("Invalid Parent Directory: "+parentDir);
				return false;
			}

			if (!is_dir(parentDir))
				return false;

			// 3) Create zip name BACKUP_YYMMDD_HHMMSS.zip
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
			string zipName = $"BACKUP_{timestamp}.zip";
			string zipPath = Path.Combine(parentDir, zipName);
			
			if (File.Exists(zipPath))
				return false;

			// 4) Create zip in parent folder
			if (isDir) {
				ZipFile.CreateFromDirectory(
					f_path,
					zipPath,
					CompressionLevel.Optimal,
					includeBaseDirectory: true
				);
			} else if (isFile) {
				using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
					archive.CreateEntryFromFile(
						f_path,
						Path.GetFileName(f_path),
						CompressionLevel.Optimal
					);
				}
			} else {
				MessageBox.Show("Failed to use ZipFile Utility");
				return false;
			}
			
			return true;
		} catch {
			MessageBox.Show("Exception");
			return false;
		}
	}
	private void SBR_open_multiple_files() {
		if (! confirmation_dialog("Dialog","This will open multiple files highlighted from this list, procceed?") ) return ;
		var paths = get_fullpath_selected(this.selected_files);
		if ( has_extension(paths, ".bat") ) return ;
		if ( has_extension(paths, ".exe") ) return ;
		foreach( var path in paths ){
			if (string.IsNullOrEmpty(path)) continue ;
			if ( !is_file(path) ) continue ;
			if ( is_dir(path) ) continue ;
			SBR_open_execute(path); 
		}
		SBR_remove_selected_selected_files();
		SBR_refresh_dirty_nodes();
	}
	private void SBR_button_op_mul() {
		if (! confirmation_dialog("Dialog","This will open multiple files from this list, procceed?") ) return ;
		var paths = get_fullpath(this.selected_files);
		if ( has_extension(paths, ".bat") ) return ;
		if ( has_extension(paths, ".exe") ) return ;
		if ( has_extension(paths, ".dll") ) return ;
		foreach( var path in paths ){
			if (string.IsNullOrEmpty(path)) continue ;
			if ( !is_file(path) ) continue ;
			if ( is_dir(path) ) continue ;
			SBR_open_execute(path); 
		}
		// SBR_remove_selected_selected_files();
		// SBR_refresh_dirty_nodes();
	}
	private void SBR_toggle_title_bar() {
		if (this.FormBorderStyle == FormBorderStyle.Sizable ) {
			hide_titlebar(this);
		} else if (this.FormBorderStyle == FormBorderStyle.None) {
			show_titlebar(this);
		}
	}
	private void SBR_add_selected_dir_to_exp() {
		var str_list = get_fullpath( this.explorer.GetSelectedNodes() );
		if (str_list.Count == 0) return ;
		foreach( string path in str_list ){
			if (! is_dir(path)) continue ;
			join( this.explorer, new_multiselection_tree(path) );
			this.data.Directories.Add(path);
		}
	}
	private void SBR_rem_selected_dir_to_exp() {
		TreeNode node = this.explorer.SelectedNode; 
		if (node == null) return ;
		if (node.Tag is string path && Directory.Exists(path)) {
			if (this.data.Directories.Contains(path)){					
				this.data.Directories.Remove(path);
				this.explorer.Nodes.Remove(node);
			}
		}
	}
	private TreeNode? get_pointed_node(TreeView tree) {
		TreeNode? node = Incantation_TREEVIEW.get_pointed_node(tree);
		if (node != null) this.last_pointed_node = node;
		return node;
	}
	private void SBR_log_selected_pointed_file_dir() {
		var nodes = this.explorer.GetSelectedNodes();
		if (nodes.Count == 1) {
			string? target_path = null;
			if (nodes[0].Tag is string path && Directory.Exists(path)) target_path = path;
			if (target_path != null && is_dir(target_path)) {
				string log_text = "--> "+target_path;
				if ( log_text != this.last_log_text ) {
					add_log(this.log_list, log_text);
					this.last_log_text = log_text;
				}
			} else {
				string log_text = "--> None";
				if ( log_text != this.last_log_text ) {
					add_log(this.log_list, log_text);
					this.last_log_text = log_text;
				}
			}
		}
	}
	private void SBR_refresh_pointed_dir() {
		var node = get_pointed_node(this.explorer);
		SBR_refresh_dir(node);
	}
	private void SBR_refresh_dir(TreeNode node) {
		if (node == null) return;
		if (!node.IsExpanded) return;
		if (node.Tag is string path) {
			if ( is_dir(path) ) {
				filter_filesystem_node(node, this.filters);
			} 
		}
	}
	private void SBR_copy_file_to_clipboard() {
		var node = get_pointed_node(this.explorer);
		SBR_copy_file_to_clipboard(node);
	}
	private void SBR_copy_file_to_clipboard(TreeNode node) {
		if (node == null) return;		
		if (node.Tag is string path) {
			if (is_dir(path)) return ;
			if (is_file(path)) Clipboard.SetText(path);
		}
	}
	private void SBR_refresh_last_pointed_node() {
		var node = this.last_pointed_node;
		if (node != null) {
			if (node.Tag is string path) {
				if ( is_dir(path) && node.IsExpanded ) {
					filter_filesystem_node(node, this.filters);
				} else {
					var parent_node = get_parent_node(node);
					if (parent_node!=null) {
						if (parent_node.IsExpanded) filter_filesystem_node(parent_node, this.filters);
					}
				}
			}
		}
	}
}

// -- END 
