using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Sources;

public interface IPhotoSource
{
    DeviceInfo? FindDevice();
    IPhotoSession Open(DeviceInfo device); // Apre una connessione al telefono
}

/// <summary>Connessione aperta a un telefono.</summary>
public interface IPhotoSession : IDisposable
{
    List<PhotoItem> Scan(Period period, CancellationToken ct);
    void CopyTo(PhotoItem photo, string destFilePath);
}
