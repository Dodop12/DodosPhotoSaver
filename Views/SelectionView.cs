using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Views;

public class SelectionView : UserControl
{
    private readonly Label _lblPhone = new() { AutoSize = true, Location = new Point(20, 15) };
    private readonly ComboBox _cmbYear = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _chkMonth = new() { Text = "Mese specifico", AutoSize = true };
    private readonly ComboBox _cmbMonth = new() { DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
    private readonly TextBox _txtPath = new() { ReadOnly = true };
    private readonly Button _btnBrowse = new() { Text = "Sfoglia..." };
    private readonly TextBox _txtName = new() { MaxLength = 100 };
    private readonly ProgressBar _bar = new() { Visible = false }; // visibile solo durante un'operazione
    private readonly Label _lblStatus = new() { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _btnDownload = new() { Text = "Scarica Foto" };
    private readonly Button _btnCancel = new() { Text = "Annulla", Visible = false };

    private string _lastDefaultName = "";

    public event EventHandler? DownloadClicked;
    public event EventHandler? CancelClicked;

    //Anno, oppure anno + mese se "Mese specifico" è selezionato
    public Period Period => new((int)_cmbYear.SelectedItem!,
                                _chkMonth.Checked ? _cmbMonth.SelectedIndex + 1 : null);
    public string ParentDir => _txtPath.Text;
    public string FolderName => _txtName.Text.Trim();

    public SelectionView()
    {
        int currentYear = DateTime.Now.Year;
        for (int y = currentYear; y >= 2007; y--) _cmbYear.Items.Add(y); // 2007 = primo iPhone
        _cmbYear.SelectedIndex = 0;

        _cmbMonth.Items.AddRange(Period.MonthNames);
        _cmbMonth.SelectedIndex = DateTime.Now.Month - 1;

        _txtPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        _lastDefaultName = Period.DefaultFolderName;
        _txtName.Text = _lastDefaultName;

        Controls.Add(_lblPhone);
        AddRow("Anno", _cmbYear, 50, 120);
        AddRow("Salva in", _txtPath, 105, 370);
        AddRow("Nome cartella", _txtName, 155, 300);

        _chkMonth.Left = 160;
        _cmbMonth.Left = 320;
        _cmbMonth.Width = 180;
        _bar.SetBounds(20, 235, 480, 18);
        _lblStatus.SetBounds(20, 255, 480, 24);
        _btnDownload.SetBounds(20, 290, 480, 38);
        _btnCancel.SetBounds(410, 290, 90, 38);
        Controls.AddRange(new Control[] { _chkMonth, _cmbMonth, _btnBrowse, _bar, _lblStatus, _btnDownload, _btnCancel });

        // Le altezze di TextBox/ComboBox dipendono dal font (e dal DPI): si riallineano a ogni cambio
        _txtPath.SizeChanged += (_, _) => AlignRows();
        _cmbYear.SizeChanged += (_, _) => AlignRows();
        AlignRows();

        _cmbYear.SelectedIndexChanged += (_, _) => OnPeriodChanged();
        _cmbMonth.SelectedIndexChanged += (_, _) => OnPeriodChanged();
        _chkMonth.CheckedChanged += (_, _) => OnPeriodChanged();
        _btnBrowse.Click += (_, _) => BrowseFolder();
        _btnDownload.Click += (_, e) => DownloadClicked?.Invoke(this, e);
        _btnCancel.Click += (_, e) => CancelClicked?.Invoke(this, e);
    }

    // ---------- Comandi dalla form ----------

    public void SetDeviceName(string name) => _lblPhone.Text = $"Connesso: {name}";

    public void SetStatus(string text) => _lblStatus.Text = text;

    public void SetBusy(bool busy)
    {
        _cmbYear.Enabled = _chkMonth.Enabled = _cmbMonth.Enabled = _txtName.Enabled =
            _btnBrowse.Enabled = _btnDownload.Enabled = !busy;
        _btnCancel.Visible = busy;
        _btnDownload.Width = busy ? 380 : 480;
        _bar.Visible = busy;
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

    /// <summary>Allinea Sfoglia alla barra del percorso e checkbox/mese alla riga dell'anno.</summary>
    private void AlignRows()
    {
        _btnBrowse.SetBounds(400, _txtPath.Top, 100, _txtPath.Height);
        _cmbMonth.Top = _cmbYear.Top;
        _chkMonth.Top = _cmbYear.Top + (_cmbYear.Height - _chkMonth.Height) / 2;
    }

    private void OnPeriodChanged()
    {
        _cmbMonth.Visible = _chkMonth.Checked;

        // Aggiorna il nome della cartella solo se l'utente non l'ha personalizzato
        string newDefault = Period.DefaultFolderName;
        if (_txtName.Text == _lastDefaultName) _txtName.Text = newDefault;
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
