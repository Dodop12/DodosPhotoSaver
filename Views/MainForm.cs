using DodosPhotoSaver.Services;
using DodosPhotoSaver.Sources;

namespace DodosPhotoSaver.Views;

public class MainForm : Form
{
    private readonly IPhotoSource _source = new IosSource(); // in futuro: scelta iOS / Android

    private readonly ConnectView _connectView = new() { Dock = DockStyle.Fill };
    private readonly SelectionView _selectionView = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 2000 };

    private Models.DeviceInfo? _device;
    private bool _busy;
    private CancellationTokenSource? _cts;

    public MainForm()
    {
        Text = "Dodo's Photo Saver";
        ClientSize = new Size(520, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);

        Controls.Add(_selectionView);
        Controls.Add(_connectView);

        _selectionView.DownloadClicked += async (_, _) => await RunDownloadAsync();
        _selectionView.CancelClicked += (_, _) => _cts?.Cancel();

        _timer.Tick += (_, _) => RefreshConnection();
        _timer.Start();
        RefreshConnection();
    }

    private void RefreshConnection()
    {
        if (_busy) return;

        _device = _source.FindDevice();
        bool connected = _device != null;

        _selectionView.Visible = connected;
        _connectView.Visible = !connected;
        if (connected) _selectionView.SetDeviceName(_device!.Name);
    }

    private async Task RunDownloadAsync()
    {
        if (_device == null) return;

        // 1. Validazione input
        string? error = FolderNameValidator.Validate(
            _selectionView.ParentDir, _selectionView.FolderName, out string fullPath);
        if (error != null)
        {
            Dialogs.Warning(this, error);
            return;
        }

        var device = _device;
        var period = _selectionView.Period;
        string parentDir = _selectionView.ParentDir;
        string folderName = _selectionView.FolderName;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            _busy = true;
            _selectionView.SetBusy(true);

            // 2. Ricerca foto
            _selectionView.SetStatus("Ricerca foto in corso...");
            _selectionView.ShowMarquee();
            var photos = await Task.Run(() =>
            {
                using var session = _source.Open(device);
                return session.Scan(period, ct);
            }, ct);

            if (photos.Count == 0)
            {
                Dialogs.NoPhotos(this, period);
                return;
            }

            // 3. Conferma nome, directory e numero di foto
            if (!Dialogs.ConfirmDownload(this, photos.Count, period, folderName, parentDir)) return;

            // 4. Cartella già esistente
            if (Directory.Exists(fullPath) && !Dialogs.ConfirmOverwrite(this, folderName, parentDir)) return;

            // 5. Spazio su disco
            if (!DiskSpaceChecker.HasEnoughSpace(parentDir, photos))
            {
                Dialogs.NotEnoughSpace(this);
                return;
            }

            // 6. Copia
            int total = photos.Count;
            _selectionView.ShowProgress(0, total);
            var progress = new Progress<int>(v => _selectionView.ShowProgress(v, total));

            var result = await Task.Run(() =>
            {
                using var session = _source.Open(device);
                return PhotoDownloader.Download(session, photos, fullPath, progress, ct);
            }, ct);

            Dialogs.Completed(this, result, total, fullPath);
        }
        catch (OperationCanceledException)
        {
            Dialogs.Cancelled(this);
        }
        catch (Exception ex)
        {
            Dialogs.Error(this, ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _busy = false;
            _selectionView.SetBusy(false);
            RefreshConnection();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cts?.Cancel();
        _timer.Stop();
        base.OnFormClosing(e);
    }
}
