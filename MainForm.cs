using DrawingFinder.Models;
using DrawingFinder.Services;
using System.Diagnostics;
using System.Text;

namespace DrawingFinder;

public partial class MainForm : Form
{
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
    private bool _usingServerFolder;
    
    public MainForm()
    {
        InitializeComponent();
        this.Text = $"Drawing Finder v1.0"; 

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
        Width = 700;
        Height = 900;
        MinimumSize = new Size(400, 400);

        pnlSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28
        };

        lblSearch = new Label
        {
            Text = "Search:",
            AutoSize = true,
            Location = new Point(5, 8)
        };

        txtSearch = new TextBox
        {
            Location = new Point(60, 4),
            Width = 606
        };

        txtSearch.TextChanged += TxtSearch_TextChanged;

        pnlSearch.Controls.Add(lblSearch);
        pnlSearch.Controls.Add(txtSearch);

        Controls.Add(pnlSearch);
 
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

        dgvResults.MultiSelect = true;
        dgvResults.ContextMenuStrip = rightClickMenu;

        string serverFolder =
            @"L:\LASER QUANTUM\Design Files\CAD files\CAD file exports\PDFS";

        string sharePointFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            @"OneDrive - Novanta\Novanta Stockport - Drawing Control");

        if (Directory.Exists(serverFolder))
        {
            _rootFolder = serverFolder;
            _usingServerFolder = true;
        }
        else if (Directory.Exists(sharePointFolder))
        {
            _rootFolder = sharePointFolder;
            _usingServerFolder = false;
        }
        else
        {
            MessageBox.Show(
                "Could not find either the server PDF folder or the SharePoint PDF folder.");

            Close();
            return;
        }

        _allFiles = IndexService.BuildIndex(_rootFolder);

        dgvResults.CellDoubleClick += DgvResults_CellDoubleClick;
        
        dgvResults.DataSource = _allFiles
            .OrderByDescending(x => x.ModifiedDate)
            .ThenBy(x => x.FileName)
            .ToList();

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
            .Where(x => MatchesSearch(
                x.FileName.ToUpper(),
                search))
            .OrderByDescending(x => x.ModifiedDate)
            .ToList();

        dgvResults.DataSource = results;
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
        if (keyData == Keys.Enter && txtSearch.Focused)
        {
            if (dgvResults.Rows.Count > 0)
            {
                var drawing =
                    (DrawingRecord)dgvResults.Rows[0].DataBoundItem;

                Process.Start(new ProcessStartInfo
                {
                    FileName = drawing.FullPath,
                    UseShellExecute = true
                });
            }

            return true;
        }

        if (keyData == Keys.Down && txtSearch.Focused)
        {
            if (dgvResults.Rows.Count > 0)
            {
                dgvResults.Focus();

                dgvResults.ClearSelection();
                dgvResults.Rows[1].Selected = true;
                dgvResults.CurrentCell =
                    dgvResults.Rows[1].Cells[0];

                dgvResults.BeginEdit(false);
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
        Cursor = Cursors.WaitCursor;

        try
        {
            _allFiles = IndexService.BuildIndex(_rootFolder);

                dgvResults.DataSource = _allFiles
                .OrderByDescending(x => x.ModifiedDate)
                .ThenBy(x => x.FileName)
                .ToList();

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
                dgvResults.Columns["FileName"].FillWeight = 82;
            if (dgvResults.Columns["ModifiedDate"] != null)
                dgvResults.Columns["ModifiedDate"].FillWeight = 18;
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
            $"{_allFiles.Count:N0} drawings indexed | " +
            $"{displayedCount:N0} matches | " +
            $"{selectedCount:N0} selected | " +
            $"{(_usingServerFolder ? "PDF Exports" : "SharePoint")}";
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

}