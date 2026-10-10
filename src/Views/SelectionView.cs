using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Views;

public class SelectionView : UserControl
{
    private const int FirstYear = 2007;
    private const int MaxFolderNameLength = 100;
    private const int ContentPadding = 15;
    private const int ActionButtonHeight = 40;
    
    private readonly Label _lblPhone = new() { AutoSize = true };

    private readonly ComboBox _cmbYear = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly CheckBox _chkMonth = new() { Text = "Mese specifico", AutoSize = true };
    private readonly ComboBox _cmbMonth = new() { DropDownStyle = ComboBoxStyle.DropDownList, Visible = false, Width = 150 };
    private readonly CheckBox _chkOtherMedia = new()
    {
        Text = "Includi immagini provenienti da app esterne",
        AutoSize = true,
        Visible = false // si mostra solo se il telefono è Android
    };

    private readonly TextBox _txtPath = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly Button _btnBrowse = new() { Text = "Sfoglia...", Dock = DockStyle.Fill, Margin = new Padding(5, 2, 0, 2) };
    private readonly TextBox _txtDestFolder = new() { MaxLength = MaxFolderNameLength, Width = 300 };

    private readonly ProgressBar _bar = new() { Visible = false, Dock = DockStyle.Fill };
    private readonly Label _lblStatus = new() { AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _btnDownload = new() { Text = "Scarica Foto", Height = 40, Dock = DockStyle.Fill };
    private readonly Button _btnCancel = new() { Text = "Annulla", Height = ActionButtonHeight, Visible = false, Dock = DockStyle.Fill };

    private string _lastDefaultName = string.Empty;

    public event EventHandler? DownloadClicked;
    public event EventHandler? CancelClicked;

    public Period SelectedPeriod => new((int)_cmbYear.SelectedItem!, _chkMonth.Checked ? _cmbMonth.SelectedIndex + 1 : null);
    public string ParentDir => _txtPath.Text;
    public string FolderName => _txtDestFolder.Text.Trim();
    public bool IncludeOtherMedia => _chkOtherMedia.Visible && _chkOtherMedia.Checked;

    public SelectionView()
    {
        InitializeData();
        BuildLayout();
        RegisterEvents();
    }

    private void InitializeData()
    {
        int currentYear = DateTime.Now.Year;
        for (int y = currentYear; y >= FirstYear; y--) _cmbYear.Items.Add(y);
        _cmbYear.SelectedIndex = 0;

        _cmbMonth.Items.AddRange(Period.MonthNames);
        _cmbMonth.SelectedIndex = DateTime.Now.Month - 1;

        _txtPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        _lastDefaultName = SelectedPeriod.DefaultFolderName;
        _txtDestFolder.Text = _lastDefaultName;
    }

    private void BuildLayout()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(ContentPadding),
            ColumnCount = 3,
            RowCount = 9,
            AutoSize = true
        };

        // Definizione Colonne: Colonna 1 (Campo Principale), Colonna 2 (Spazio/Opzioni), Colonna 3 (Pulsante/Azione)
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Prende tutto lo spazio disponibile
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F)); // Larghezza fissa per il pulsante Sfoglia

        _lblPhone.Margin = new Padding(0, 0, 0, 12);

        // Info Telefono
        mainLayout.Controls.Add(_lblPhone, 0, 0);
        mainLayout.SetColumnSpan(_lblPhone, 3);

        // Anno e Mese
        var periodPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 5, 0, 10)
        };
        _chkMonth.Margin = new Padding(15, 4, 5, 0); // Allinea verticalmente la checkbox con la combobox
        periodPanel.Controls.AddRange([_cmbYear, _chkMonth, _cmbMonth]);

        mainLayout.Controls.Add(new Label { Text = "Anno", AutoSize = true }, 0, 1);
        mainLayout.Controls.Add(periodPanel, 0, 2);
        mainLayout.SetColumnSpan(periodPanel, 3);

        // Solo Android: inclusione di media da altre app
        _chkOtherMedia.Margin = new Padding(3, 0, 3, 10);
        mainLayout.Controls.Add(_chkOtherMedia, 0, 3);
        mainLayout.SetColumnSpan(_chkOtherMedia, 3);

        // Percorso "Salva in" (TextBox + Pulsante Sfoglia)
        mainLayout.Controls.Add(new Label { Text = "Salva in", AutoSize = true }, 0, 4);
        mainLayout.Controls.Add(_txtPath, 0, 5);
        mainLayout.SetColumnSpan(_txtPath, 2); // Occupa prima e seconda colonna
        mainLayout.Controls.Add(_btnBrowse, 2, 5);
        
        // Cartella di destinazione
        mainLayout.Controls.Add(new Label { Text = "Nome cartella", AutoSize = true }, 0, 6);
        mainLayout.Controls.Add(_txtDestFolder, 0, 7);
        mainLayout.SetColumnSpan(_txtDestFolder, 3);

        // Barra e Pulsanti di Azione
        var actionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0, 15, 0, 0),
            AutoSize = true
        };
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        actionPanel.Controls.Add(_bar, 0, 0);
        actionPanel.SetColumnSpan(_bar, 2);

        actionPanel.Controls.Add(_lblStatus, 0, 1);
        actionPanel.SetColumnSpan(_lblStatus, 2);

        actionPanel.Controls.Add(_btnDownload, 0, 2);
        actionPanel.Controls.Add(_btnCancel, 1, 2);

        mainLayout.Controls.Add(actionPanel, 0, 8);
        mainLayout.SetColumnSpan(actionPanel, 3);

        Controls.Add(mainLayout);
    }

    private void RegisterEvents()
    {
        _cmbYear.SelectedIndexChanged += (_, _) => OnPeriodChanged();
        _cmbMonth.SelectedIndexChanged += (_, _) => OnPeriodChanged();
        _chkMonth.CheckedChanged += (_, _) => OnPeriodChanged();
        _btnBrowse.Click += (_, _) => BrowseFolder();
        _btnDownload.Click += (_, e) => DownloadClicked?.Invoke(this, e);
        _btnCancel.Click += (_, e) => CancelClicked?.Invoke(this, e);
    }

    public void SetDeviceName(DeviceInfo device)
    {
        _lblPhone.Text = $"Connesso: {device.Name}";
        // Se Android, mostra la spunta per gli altri media
        _chkOtherMedia.Visible = device.Platform == DevicePlatform.Android;
    }

    public void SetStatus(string text) => _lblStatus.Text = text;

    public void SetBusy(bool busy)
    {
        foreach (var c in new Control[] { _cmbYear, _chkMonth, _cmbMonth, _txtDestFolder, _btnBrowse })
            c.Enabled = !busy;

        _btnCancel.Visible = busy;
        _bar.Visible = busy;

        // Reset a fine processo
        if (!busy)
        {
            _bar.Style = ProgressBarStyle.Blocks;
            _bar.Value = 0;
            _lblStatus.Text = string.Empty;
        }
    }

    public void ShowMarquee() => _bar.Style = ProgressBarStyle.Marquee;

    public void ShowProgress(int value, int max)
    {
        _bar.Style = ProgressBarStyle.Blocks;
        _bar.Maximum = Math.Max(max, 1);
        _bar.Value = Math.Clamp(value, 0, _bar.Maximum); // Ignora eventuali valori della barra fuori range
        _lblStatus.Text = $"Copia in corso... {value}/{max}";
    }

    private void OnPeriodChanged()
    {
        _cmbMonth.Visible = _chkMonth.Checked;
        string newDefault = SelectedPeriod.DefaultFolderName;
        if (_txtDestFolder.Text == _lastDefaultName) _txtDestFolder.Text = newDefault;
        _lastDefaultName = newDefault;
    }

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Scegli dove salvare la cartella",
            SelectedPath = _txtPath.Text
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) _txtPath.Text = dlg.SelectedPath;
    }
}