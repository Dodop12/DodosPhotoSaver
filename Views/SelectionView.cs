namespace DodosPhotoSaver.Views;

/// <summary>Schermata principale: anno, cartella, nome e tasto Scarica Foto. Non contiene logica di business.</summary>
public class SelectionView : UserControl
{
    private readonly Label _lblPhone = new() { AutoSize = true, Location = new Point(20, 15) };
    private readonly ComboBox _cmbYear = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _txtPath = new() { ReadOnly = true };
    private readonly Button _btnBrowse = new() { Text = "Sfoglia..." };
    private readonly TextBox _txtName = new() { MaxLength = 100 };
    private readonly ProgressBar _bar = new();
    private readonly Label _lblStatus = new() { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _btnDownload = new() { Text = "Scarica Foto" };
    private readonly Button _btnCancel = new() { Text = "Annulla", Visible = false };

    private int _lastYear;

    public event EventHandler? DownloadClicked;
    public event EventHandler? CancelClicked;

    public int Year => (int)_cmbYear.SelectedItem!;
    public string ParentDir => _txtPath.Text;
    public string FolderName => _txtName.Text.Trim();

    public SelectionView()
    {
        int currentYear = DateTime.Now.Year;
        for (int y = currentYear; y >= 2007; y--) _cmbYear.Items.Add(y); // 2007 = primo iPhone
        _cmbYear.SelectedIndex = 0;
        _lastYear = currentYear;

        _txtPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        _txtName.Text = $"Foto {currentYear}";

        Controls.Add(_lblPhone);
        AddRow("Anno", _cmbYear, 55, 120);
        AddRow("Salva in", _txtPath, 105, 370);
        AddRow("Nome cartella", _txtName, 155, 300);

        _btnBrowse.SetBounds(400, 129, 100, 28);
        _bar.SetBounds(20, 235, 480, 18);
        _lblStatus.SetBounds(20, 255, 480, 24);
        _btnDownload.SetBounds(20, 290, 480, 38);
        _btnCancel.SetBounds(410, 290, 90, 38);
        Controls.AddRange(new Control[] { _btnBrowse, _bar, _lblStatus, _btnDownload, _btnCancel });

        _cmbYear.SelectedIndexChanged += (_, _) => OnYearChanged();
        _btnBrowse.Click += (_, _) => BrowseFolder();
        _btnDownload.Click += (_, e) => DownloadClicked?.Invoke(this, e);
        _btnCancel.Click += (_, e) => CancelClicked?.Invoke(this, e);
    }

    // ---------- Comandi dalla form ----------

    public void SetDeviceName(string name) => _lblPhone.Text = $"Connesso: {name}";

    public void SetStatus(string text) => _lblStatus.Text = text;

    public void SetBusy(bool busy)
    {
        _cmbYear.Enabled = _txtName.Enabled = _btnBrowse.Enabled = _btnDownload.Enabled = !busy;
        _btnCancel.Visible = busy;
        _btnDownload.Width = busy ? 380 : 480;
        if (!busy)
        {
            _bar.Style = ProgressBarStyle.Blocks;
            _bar.Value = 0;
            _lblStatus.Text = "";
        }
    }

    public void ShowMarquee() => _bar.Style = ProgressBarStyle.Marquee;

    public void ShowProgress(int value, int max)
    {
        _bar.Style = ProgressBarStyle.Blocks;
        _bar.Maximum = Math.Max(max, 1);
        _bar.Value = Math.Clamp(value, 0, _bar.Maximum);
        _lblStatus.Text = $"Copia in corso... {value}/{max}";
    }

    // ---------- Interno ----------

    private void AddRow(string label, Control field, int top, int width)
    {
        Controls.Add(new Label { Text = label, AutoSize = true, Location = new Point(20, top) });
        field.SetBounds(20, top + 24, width, 28);
        Controls.Add(field);
    }

    private void OnYearChanged()
    {
        // Aggiorna il nome solo se l'utente non l'ha personalizzato
        if (_txtName.Text == $"Foto {_lastYear}") _txtName.Text = $"Foto {Year}";
        _lastYear = Year;
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
