using DodosPhotoSaver.Models;
using MediaDevices;

namespace DodosPhotoSaver.Sources;

public class IosSource : IPhotoSource
{
    public DeviceInfo? FindDevice()
    {
        var devices = new List<MediaDevice>();
        try
        {
            devices.AddRange(MediaDevice.GetDevices());
            foreach (var d in devices)
            {
                bool isApple =
                    (d.Manufacturer ?? "").Contains("Apple", StringComparison.OrdinalIgnoreCase) ||
                    (d.Description ?? "").Contains("iPhone", StringComparison.OrdinalIgnoreCase);

                if (isApple)
                {
                    string name = string.IsNullOrWhiteSpace(d.FriendlyName) ? "iPhone" : d.FriendlyName;
                    return new DeviceInfo(d.DeviceId, name);
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            foreach (var d in devices) d.Dispose();
        }
    }

    public IPhotoSession Open(DeviceInfo device)
    {
        var dev = MediaDevice.GetDevices().FirstOrDefault(d => d.DeviceId == device.Id)
                  ?? throw new InvalidOperationException("iPhone non trovato. Controlla il collegamento.");
        try
        {
            dev.Connect();
            return new IosSession(dev);
        }
        catch
        {
            dev.Dispose();
            throw;
        }
    }

    private sealed class IosSession : IPhotoSession
    {
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" }; // niente AAE, HEIC, MOV

        private const int MaxDepth = 12; // protezione da strutture di cartelle anomale

        private readonly MediaDevice _device;
        public IosSession(MediaDevice device) => _device = device;

        public List<PhotoItem> Scan(int year, CancellationToken ct)
        {
            var result = new List<PhotoItem>();
            Walk(_device.GetRootDirectory(), year, result, 0, ct);
            return result;
        }

        private static void Walk(MediaDirectoryInfo dir, int year, List<PhotoItem> result, int depth, CancellationToken ct)
        {
            if (depth > MaxDepth) return;
            ct.ThrowIfCancellationRequested();

            foreach (var file in dir.EnumerateFiles())
            {
                if (!AllowedExtensions.Contains(Path.GetExtension(file.Name))) continue;

                DateTime? date = file.CreationTime ?? file.LastWriteTime;
                if (date?.Year == year)
                    result.Add(new PhotoItem(file.FullName, file.Name, file.Length));
            }

            foreach (var sub in dir.EnumerateDirectories())
                Walk(sub, year, result, depth + 1, ct);
        }

        public void CopyTo(PhotoItem photo, string destFilePath)
            => _device.DownloadFile(photo.DevicePath, destFilePath);

        public void Dispose() => _device.Dispose();
    }
}
