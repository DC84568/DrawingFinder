using DrawingFinder.Models;
using DrawingFinder.Services;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DrawingFinder;

public partial class MainForm : Form
{
    private UserSettings _settings = new();
    private TextBox txtSearch;
    private DataGridView dgvResults;
    private ContextMenuStrip rightClickMenu;
    private List<DrawingRecord> _allFiles = new();
    private List<string> _dragFiles = new();
    private bool _dragging;
    private Point _dragStartPoint;
    private Panel pnlSearch;
    private Label lblSearch;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatus;
    private string _rootFolder = string.Empty;
    private TextBox txtFolder;
    private Label lblFolder;
    private Button btnUp;
    private ComboBox cboFavorites;
    private Label lblFavorite;
    private Dictionary<string, string> _favoriteMap = new();
    
    public MainForm()
    {
        InitializeComponent();
        this.Text = $"Drawing Finder v1.1"; 
       
        string iconPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets",
            "DrawingFinder.ico");

        if (File.Exists(iconPath))
        {
            this.Icon = new Icon(iconPath);
        }

        StartPosition = FormStartPosition.Manual;
        Location = new Point(20, 20);
        Width = 730;
        Height = 900;
        MinimumSize = new Size(400, 400);

        lblFolder = new Label
        {
            Text = "Folder:",
            AutoSize = true,
            Location = new Point(5, 8)
        };

        txtFolder = new TextBox
        {
            Location = new Point(70, 4),
            Width = 585,
        };
        txtFolder.KeyDown += TxtFolder_KeyDown;
        
        btnUp = new Button
        {
            Text = "Up",
            Location = new Point(660, 3),
            Width = 50,
            Height = 29,
        };
        this.Resize += MainForm_Resize;

        btnUp.Click += BtnUp_Click;

        lblFavorite = new Label
        {
            Text = "Favorite:",
            AutoSize = true,
            Location = new Point(5, 35)
        };

        cboFavorites = new ComboBox
        {
            Location = new Point(70, 31),
            Width = 250,
            DropDownStyle = ComboBoxStyle.DropDownList,
            // TabStop = false
        };        

        LoadSettings();

        if (_settings.FavoriteFolders.Count == 0)
        {
            _settings.FavoriteFolders.Add(
                new FavoriteFolder
                {
                    Name = "Drawing Control",
                    Path = Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.UserProfile),
                        @"OneDrive - Novanta\Novanta Stockport - Drawing Control")
                });

            _settings.FavoriteFolders.Add(
                new FavoriteFolder
                {
                    Name = "CAD Exports",
                    Path = @"\\sto-srv-fs2\LQDATA\LASER QUANTUM\Design Files\CAD files\CAD file exports"
                });

            SaveSettings();
        }      

        foreach (var favorite in _settings.FavoriteFolders)
        {
            _favoriteMap[favorite.Name] = favorite.Path;

            if (!cboFavorites.Items.Contains(favorite.Name))
            {
                cboFavorites.Items.Add(favorite.Name);
            }
        }

        this.FormClosing += MainForm_FormClosing;        

        pnlSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88
        };

        lblSearch = new Label
        {
            Text = "Search:",
            AutoSize = true,
            Location = new Point(5, 63)
        };

        // txtSearch.Enter += TxtSearch_Enter;

        txtSearch = new TextBox
        {
            Location = new Point(70, 59),
            Width = 606,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        txtSearch.TextChanged += TxtSearch_TextChanged;
        // txtSearch.KeyDown += TxtSearch_KeyDown;

        dgvResults = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = true
        };

        dgvResults.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        dgvResults.AllowUserToAddRows = false;
        dgvResults.AllowUserToDeleteRows = false;
        dgvResults.AllowUserToResizeRows = false;
        dgvResults.RowHeadersVisible = false;
        dgvResults.DefaultCellStyle.Padding =
            new Padding(8, 0, 0, 0);
        dgvResults.SelectionChanged += DgvResults_SelectionChanged;

        Controls.Add(dgvResults);

        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel();
        statusStrip.Items.Add(lblStatus);
        Controls.Add(statusStrip);

        pnlSearch.Controls.Add(lblFolder);
        pnlSearch.Controls.Add(txtFolder);
        pnlSearch.Controls.Add(btnUp);

        pnlSearch.Controls.Add(lblFavorite);
        pnlSearch.Controls.Add(cboFavorites);

        pnlSearch.Controls.Add(lblSearch);
        pnlSearch.Controls.Add(txtSearch);

        Controls.Add(pnlSearch);

        rightClickMenu = new ContextMenuStrip();

        rightClickMenu.Items.Add(
            "Create SharePoint Link(s)",
            null,
            CreateRtfLinkList_Click);

        rightClickMenu.Items.Add(
            "Copy Filename(s)",
            null,
            CopyFilename_Click);

        rightClickMenu.Items.Add(
            "Open PDF(s)",
            null,
            OpenPdf_Click);  
        
        rightClickMenu.Items.Add(new ToolStripSeparator());

        rightClickMenu.Items.Add(
            "Open PDF Folder",
            null,
            OpenFolder_Click);

        rightClickMenu.Items.Add(
            "Refresh Files",
            null,
            RefreshFiles_Click);

        rightClickMenu.Items.Add(new ToolStripSeparator());

        rightClickMenu.Items.Add(
            "Add Current Folder to Favorites",
            null,
            AddCurrentFolderToFavorites_Click);

        rightClickMenu.Items.Add(
            "Edit Settings File",
            null,
            EditSettings_Click);

        dgvResults.MultiSelect = true;
        dgvResults.ContextMenuStrip = rightClickMenu;

        string serverFolder =
            @"L:\LASER QUANTUM\Design Files\CAD files\CAD file exports\PDFS";

        string sharePointFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            @"OneDrive - Novanta\Novanta Stockport - Drawing Control");

        if (!string.IsNullOrWhiteSpace(_settings.LastFolder) &&
            Directory.Exists(_settings.LastFolder))
        {
            _rootFolder = _settings.LastFolder;
        }
        else if (Directory.Exists(sharePointFolder))
        {
            _rootFolder = sharePointFolder;
        }
        else if (Directory.Exists(serverFolder))
        {
            _rootFolder = serverFolder;
        }
        else
        {
            MessageBox.Show(
                "Could not find a valid root folder.");

            Close();
            return;
        }

        cboFavorites.SelectedIndexChanged += CboFavorites_SelectedIndexChanged;

        _allFiles = IndexService.BuildIndex(_rootFolder);
        txtFolder.Text = _rootFolder;

        dgvResults.CellDoubleClick += DgvResults_CellDoubleClick;
        
        dgvResults.DataSource = _allFiles
            .OrderByDescending(x => x.ModifiedDate)
            .ThenBy(x => x.FileName)
            .ToList();

        dgvResults.ClearSelection();

        FormatGrid();

        if (dgvResults.Columns["SearchName"] != null)
        {
            dgvResults.Columns["SearchName"].Visible = false;
        }

        if (dgvResults.Columns["FullPath"] != null)
        {
            dgvResults.Columns["FullPath"].Visible = false;
        }

        dgvResults.KeyDown += DgvResults_KeyDown;
        dgvResults.MouseDown += DgvResults_MouseDown;
        dgvResults.MouseMove += DgvResults_MouseMove;
        dgvResults.MouseUp += DgvResults_MouseUp;
        dgvResults.CellMouseDown += DgvResults_CellMouseDown;    
        
        this.Shown += MainForm_Shown;
    }
    private void TxtSearch_TextChanged(object? sender, EventArgs e)
    {
        string search = txtSearch.Text.ToUpper();

        var results = _allFiles
            .Where(x =>
                MatchesSearch(
                    x.FileName.ToUpper(),
                    search))
            .OrderByDescending(x => x.IsFolder)
            .ThenBy(x => x.FileName)
            .ToList();


        dgvResults.DataSource = results;

        dgvResults.ClearSelection();
        dgvResults.CurrentCell = null;

        FormatGrid();
        UpdateStatusBar();
    }
    private void DgvResults_CellDoubleClick(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        var drawing =
            (DrawingRecord)dgvResults.Rows[e.RowIndex].DataBoundItem;

        if (drawing.IsFolder)
        {
            NavigateToFolder(drawing.FullPath);

            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = drawing.FullPath,
            UseShellExecute = true
        });
    }
    private void DgvResults_MouseDown(
        object? sender,
        MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var hit = dgvResults.HitTest(e.X, e.Y);

            if (hit.RowIndex >= 0 &&
                dgvResults.Rows[hit.RowIndex].Selected)
            {
                _dragging = true;
                _dragStartPoint = e.Location;

                _dragFiles = dgvResults.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Where(r => r.DataBoundItem is DrawingRecord)
                    .Select(r => ((DrawingRecord)r.DataBoundItem).FullPath)
                    .ToList();

                return;
            }
        }
    }
    private void DgvResults_MouseUp(
        object? sender,
        MouseEventArgs e)
    {
        _dragging = false;
    }
    private void DgvResults_MouseMove(
        object? sender,
        MouseEventArgs e)
    {
        if (!_dragging)
            return;

        int dx = Math.Abs(e.X - _dragStartPoint.X);
        int dy = Math.Abs(e.Y - _dragStartPoint.Y);

        if (dx < 5 && dy < 5)
            return;

        _dragging = false;

        if (dgvResults.SelectedRows.Count == 0)
            return;

        var filePaths = _dragFiles.ToArray();

        DataObject data = new DataObject(
            DataFormats.FileDrop,
            filePaths);

        Cursor.Current = Cursors.Hand;

        dgvResults.DoDragDrop(
            data,
            DragDropEffects.Copy);
    }
    private DrawingRecord? GetSelectedDrawing()
    {
        if (dgvResults.SelectedRows.Count == 0)
            return null;

        return dgvResults.SelectedRows[0].DataBoundItem as DrawingRecord;
    }
    private void OpenPdf_Click(
        object? sender,
        EventArgs e)
    {
        var drawings = dgvResults.SelectedRows
            .Cast<DataGridViewRow>()
            .Where(r => r.DataBoundItem is DrawingRecord)
            .Select(r => (DrawingRecord)r.DataBoundItem)
            .OrderBy(d => d.FileName)
            .ToList();

        if (drawings.Count == 0)
            return;

        if (drawings.Count > 10)
        {
            var result = MessageBox.Show(
                $"Open {drawings.Count} PDFs?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;
        }

        foreach (var drawing in drawings)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = drawing.FullPath,
                UseShellExecute = true
            });
        }
    }

    private void OpenFolder_Click(object? sender, EventArgs e)
    {
        var drawing = GetSelectedDrawing();

        if (drawing == null)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{drawing.FullPath}\""
        });
    }
    private void CopyFilename_Click(
        object? sender,
        EventArgs e)
    {
        var fileNames = dgvResults.SelectedRows
            .Cast<DataGridViewRow>()
            .Where(r => r.DataBoundItem is DrawingRecord)
            .Select(r => Path.GetFileNameWithoutExtension(
                ((DrawingRecord)r.DataBoundItem).FileName))
            .OrderBy(f => f)
            .ToList();

        if (fileNames.Count == 0)
            return;

        Clipboard.SetText(
            string.Join(Environment.NewLine, fileNames));
    }
    private void DgvResults_CellMouseDown(
        object? sender,
        DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        if (e.Button == MouseButtons.Right)
        {
            if (!dgvResults.Rows[e.RowIndex].Selected)
            {
                dgvResults.ClearSelection();
                dgvResults.Rows[e.RowIndex].Selected = true;
                dgvResults.CurrentCell =
                    dgvResults.Rows[e.RowIndex].Cells[0];
            }
        }
    }
    private void MainForm_Shown(object? sender, EventArgs e)
    {
        txtSearch.Focus();
        txtSearch.SelectAll();

        dgvResults.ClearSelection();
        dgvResults.CurrentCell = null;
    }
    private void DgvResults_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            var drawings = dgvResults.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.DataBoundItem is DrawingRecord)
                .Select(r => (DrawingRecord)r.DataBoundItem)
                .OrderBy(d => d.FileName);

            if (dgvResults.SelectedRows.Count > 10)
            {
                var result = MessageBox.Show(
                    $"Open {dgvResults.SelectedRows.Count} PDFs?",
                    "Confirm",
                    MessageBoxButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;
            }

            foreach (var drawing in drawings)
            {
                if (drawing.IsFolder)
                {
                    NavigateToFolder(drawing.FullPath);
                    break;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = drawing.FullPath,
                    UseShellExecute = true
                });
            }
         
        }
    }
    protected override bool ProcessCmdKey(
        ref Message msg,
        Keys keyData)
    {
        if (keyData == Keys.Down && txtSearch.Focused)
        {
            if (dgvResults.Rows.Count > 0)
            {
                dgvResults.Focus();

                dgvResults.ClearSelection();
                dgvResults.Rows[0].Selected = true;
                dgvResults.CurrentCell =
                    dgvResults.Rows[0].Cells[0];
            }

            return true;
        }

        if (keyData == Keys.Escape && txtSearch.Focused)
        {
            txtSearch.Clear();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
    private void RefreshFiles_Click(
        object? sender,
        EventArgs e)
    {
        if (!Directory.Exists(_rootFolder))
        {
            MessageBox.Show(
                $"Folder not found:\n\n{_rootFolder}",
                "Drawing Finder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        Cursor = Cursors.WaitCursor;

        try
        {
            _allFiles = IndexService.BuildIndex(_rootFolder);

            txtFolder.Text = _rootFolder;

            dgvResults.DataSource = _allFiles
                .OrderByDescending(x => x.ModifiedDate)
                .ThenBy(x => x.FileName)
                .ToList();
            dgvResults.ClearSelection();

            TxtSearch_TextChanged(null, EventArgs.Empty);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }
    private void CopyHyperlinks_Click(
        object? sender,
        EventArgs e)
    {
        var text = string.Join(
            Environment.NewLine + Environment.NewLine,
            dgvResults.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.DataBoundItem is DrawingRecord)
                .Select(r => (DrawingRecord)r.DataBoundItem)
                .OrderBy(d => d.FileName)
                .Select(d =>
                {
                    string url =
                        $"https://gsi.sharepoint.com/sites/LaserQuantumStockport/Drawing%20Control/{Uri.EscapeDataString(d.FileName)}";

                    return $"{d.FileName}{Environment.NewLine}{url}";
                }));

        Clipboard.SetText(text);
    }
    private void FormatGrid()
    {
        try
        {
            if (dgvResults.Columns["SearchName"] != null)
                dgvResults.Columns["SearchName"].Visible = false;

            if (dgvResults.Columns["FullPath"] != null)
                dgvResults.Columns["FullPath"].Visible = false;

            if (dgvResults.Columns["FileName"] != null)
                dgvResults.Columns["FileName"].FillWeight = 80;
            if (dgvResults.Columns["ModifiedDate"] != null)
                dgvResults.Columns["ModifiedDate"].FillWeight = 20;
            if (dgvResults.Columns["IsFolder"] != null)
                dgvResults.Columns["IsFolder"].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }

    }
    private void UpdateStatusBar()
    {
        int selectedCount = dgvResults.Rows
            .Cast<DataGridViewRow>()
            .Count(r => r.Selected);

        int displayedCount = dgvResults.Rows.Count;

        if (string.IsNullOrWhiteSpace(txtSearch.Text))
        {
            displayedCount = _allFiles.Count;
        }

        lblStatus.Text =
            $"{_allFiles.Count:N0} items | " +
            $"{displayedCount:N0} matches | " +
            $"{selectedCount:N0} selected | ";
    }
    private void DgvResults_SelectionChanged(
        object? sender,
        EventArgs e)
    {
        UpdateStatusBar();
    }
    private void CreateRtfLinkList_Click(
        object? sender,
        EventArgs e)
    {
        string tempFolder = Path.GetTempPath();

        // Clean up old link files
        foreach (string file in Directory.GetFiles(
            tempFolder,
            "DrawingLinks_*.rtf"))
        {
            try
            {
                DateTime age = File.GetCreationTime(file);

                if (age < DateTime.Now.AddDays(-7))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore open files
            }
        }  

        var drawings = dgvResults.SelectedRows
            .Cast<DataGridViewRow>()
            .Where(r => r.DataBoundItem is DrawingRecord)
            .Select(r => (DrawingRecord)r.DataBoundItem)
            .OrderBy(d => d.FileName)
            .ToList();

        if (drawings.Count == 0)
            return;

        StringBuilder rtf = new StringBuilder();

        rtf.AppendLine(@"{\rtf1\ansi\deff0");
        rtf.AppendLine(@"{\fonttbl{\f0 Calibri;}}");
        rtf.AppendLine(@"{\colortbl;\red0\green0\blue255;}");
        rtf.AppendLine(@"\fs22");

        foreach (var drawing in drawings)
        {
            string displayName =
                Path.GetFileNameWithoutExtension(
                    drawing.FileName);

            string url =
                $"https://gsi.sharepoint.com/sites/LaserQuantumStockport/Drawing%20Control/{Uri.EscapeDataString(drawing.FileName)}";

            rtf.AppendLine(
                $@"{{\field{{\*\fldinst{{HYPERLINK ""{url}""}}}}{{\fldrslt{{\ul\cf1 {displayName}}}}}}}\par");

            rtf.AppendLine(@"\par");
        }

        rtf.AppendLine("}");

        string rtfFile = Path.Combine(
            tempFolder,
            $"DrawingLinks_{Guid.NewGuid():N}.rtf");

        File.WriteAllText(rtfFile, rtf.ToString());
  
        Process.Start(new ProcessStartInfo
        {
            FileName = rtfFile,
            UseShellExecute = true
        });
    }
    private bool MatchesSearch(
        string fileName,
        string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        if (!search.Contains('*'))
            return fileName.Contains(search);

        var parts = search
            .Split('*', StringSplitOptions.RemoveEmptyEntries);

        int position = 0;

        foreach (var part in parts)
        {
            int index = fileName.IndexOf(
                part,
                position,
                StringComparison.OrdinalIgnoreCase);

            if (index < 0)
                return false;

            position = index + part.Length;
        }

        return true;
    }
    private void BtnUp_Click(
        object? sender,
        EventArgs e)
    {
        var parent = Directory.GetParent(_rootFolder);

        if (parent == null)
            return;

        NavigateToFolder(parent.FullName);
    }
    private void MainForm_Resize(
        object? sender,
        EventArgs e)
    {
        btnUp.Left = pnlSearch.ClientSize.Width - btnUp.Width - 5;

        txtFolder.Width = btnUp.Left - txtFolder.Left - 5;
    }
    private void TxtFolder_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;

        e.Handled = true;
        e.SuppressKeyPress = true;

        if (!Directory.Exists(txtFolder.Text))
        {
            MessageBox.Show(
                "Folder not found.",
                "Drawing Finder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        NavigateToFolder(txtFolder.Text);
    }
    private void CboFavorites_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (!_favoriteMap.TryGetValue(
                cboFavorites.Text,
                out string? targetFolder))
        {
            return;
        }

        if (!Directory.Exists(targetFolder))
        {
            MessageBox.Show(
                $"Folder not available:\n\n{targetFolder}",
                "Drawing Finder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        NavigateToFolder(targetFolder);
    }
    private static string GetSettingsFile()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "DrawingFinder");

        Directory.CreateDirectory(folder);

        return Path.Combine(folder, "settings.json");
    }
    private void LoadSettings()
    {
        string settingsFile = GetSettingsFile();

        if (!File.Exists(settingsFile))
            return;

        try
        {
            string json = File.ReadAllText(settingsFile);

            _settings =
                JsonSerializer.Deserialize<UserSettings>(json)
                ?? new UserSettings();
        }
        catch
        {
            _settings = new UserSettings();
        }
    }    
    private void SaveSettings()
    {
        try
        {
            string settingsFile = GetSettingsFile();

            string json =
                JsonSerializer.Serialize(
                    _settings,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText(settingsFile, json);
        }
        catch
        {
        }
    }    
    private void MainForm_FormClosing(
        object? sender,
        FormClosingEventArgs e)
    {
        _settings.LastFolder = _rootFolder;

        SaveSettings();
    }
    private void AddCurrentFolderToFavorites_Click(
        object? sender,
        EventArgs e)
    {
        if (_settings.FavoriteFolders.Any(
                f => f.Path.Equals(
                    _rootFolder,
                    StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                "This folder is already in Favorites.");

            return;
        }

        string name = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter a friendly name for this favorite:",
            "Add Favorite",
            Path.GetFileName(_rootFolder));

        if (string.IsNullOrWhiteSpace(name))
            return;

        _settings.FavoriteFolders.Add(
            new FavoriteFolder
            {
                Name = name,
                Path = _rootFolder
            });

        _favoriteMap[name] = _rootFolder;

        cboFavorites.Items.Add(name);
        cboFavorites.SelectedItem = name;

        SaveSettings();
    }
    private void EditSettings_Click(
        object? sender,
        EventArgs e)
    {
        string settingsFile = GetSettingsFile();

        if (!File.Exists(settingsFile))
        {
            SaveSettings();
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = $"\"{settingsFile}\"",
            UseShellExecute = true
        });
        }
    private void NavigateToFolder(string folder)
    {
        _rootFolder = folder;

        txtFolder.Text = folder;

        txtSearch.Clear();

        RefreshFiles_Click(null, EventArgs.Empty);

        txtSearch.Focus();
    }
    // private void TxtSearch_KeyDown(
    //     object? sender,
    //     KeyEventArgs e)
    // {
    //     if (e.KeyCode == Keys.Down)
    //     {
    //         MessageBox.Show("Down pressed");

    //         e.Handled = true;
    //         e.SuppressKeyPress = true;

    //         if (dgvResults.Rows.Count > 0)
    //         {
    //             dgvResults.Focus();

    //             dgvResults.ClearSelection();
    //             dgvResults.Rows[0].Selected = true;
    //             dgvResults.CurrentCell =
    //                 dgvResults.Rows[0].Cells[0];
    //         }
    //     }
    // }
    private void TxtSearch_Enter(
        object? sender,
        EventArgs e)
    {
        dgvResults.ClearSelection();
    }
}