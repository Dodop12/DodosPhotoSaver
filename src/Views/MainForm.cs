using DodosPhotoSaver.Models;
using DodosPhotoSaver.Services;
using DodosPhotoSaver.Sources;

namespace DodosPhotoSaver.Views;

/// <summary>Finestra principale: alterna le due schermate e gestisce il flusso di download.</summary>
public class MainForm : Form
{
    private static readonly Size WindowSize = new(520, 390);

    private readonly IPhotoSource _source; // riconosce da solo iPhone e Android

    private readonly ConnectView _connectView = new() { Dock = DockStyle.Fill };
    private readonly SelectionView _selectionView = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 2000 };

    private DeviceInfo? _device;
    private bool _busy;
    private CancellationTokenSource? _cts;

    public MainForm(IPhotoSource source)
    {
        _source = source;

        Text = "Dodo's Photo Saver";
        ClientSize = WindowSize;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

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
        if (connected) _selectionView.SetDeviceName(_device!);
    }

    private async Task RunDownloadAsync()
    {
        if (_device == null) return;

        string? error = FolderNameValidator.Validate(
            _selectionView.ParentDir,
            _selectionView.FolderName,
            out string fullPath
        );
        if (error != null)
        {
            Dialogs.Warning(this, error);
            return;
        }

        var device = _device;
        var period = _selectionView.SelectedPeriod;
        var scanOptions = new ScanOptions(_selectionView.IncludeOtherMedia);
        string parentDir = _selectionView.ParentDir;
        string folderName = _selectionView.FolderName;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            _busy = true;
            _selectionView.SetBusy(true);

            // Ricerca foto
            _selectionView.SetStatus("Ricerca foto in corso...");
            _selectionView.ShowMarquee();
            var photos = await Task.Run(() =>
            {
                using var session = _source.Open(device, scanOptions);
                return session.Scan(period, ct);
            }, ct);
            ct.ThrowIfCancellationRequested();

            if (photos.Count == 0)
            {
                Dialogs.NoPhotos(this, period);
                return;
            }

            // Conferma nome, directory e numero di foto
            if (!Dialogs.ConfirmDownload(this, photos.Count, period, folderName, parentDir)) return;

            // Cartella già esistente
            if (Directory.Exists(fullPath) && !Dialogs.ConfirmOverwrite(this, folderName, parentDir)) return;

            // Spazio su disco insufficiente
            ulong totalBytes = 0;
            foreach (var p in photos)
            {
                totalBytes += p.Size;
            }
            
            try
            {
                if (!DiskSpaceChecker.HasEnoughSpace(parentDir, totalBytes))
                {
                    Dialogs.NotEnoughSpace(this);
                    return;
                }
            }
            catch (IOException)
            {
                Dialogs.SpaceCheckFailed(this);
                return;
            }

            // Copia foto
            int total = photos.Count;
            _selectionView.ShowProgress(0, total);
            var progress = new Progress<int>(v => _selectionView.ShowProgress(v, total));

            var result = await Task.Run(async () =>
            {
                using var session = _source.Open(device);
                return await PhotoDownloader.DownloadAsync(session, photos, fullPath, progress, ct);
            }, ct);
            ct.ThrowIfCancellationRequested();

            Dialogs.Completed(this, result, total, fullPath); // Riepilogo risultato
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