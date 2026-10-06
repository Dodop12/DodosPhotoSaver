using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Sources;

/// <summary>Un tipo di telefono (iOS oggi, Android in futuro).</summary>
public interface IPhotoSource
{
    /// <summary>Restituisce il telefono collegato, oppure null.</summary>
    DeviceInfo? FindDevice();

    /// <summary>Apre una connessione al telefono. Va chiusa con Dispose.</summary>
    IPhotoSession Open(DeviceInfo device);
}

/// <summary>Connessione aperta a un telefono.</summary>
public interface IPhotoSession : IDisposable
{
    /// <summary>Foto JPG/JPEG/PNG dell'anno indicato, in tutte le sottocartelle.</summary>
    List<PhotoItem> Scan(int year, CancellationToken ct);

    /// <summary>Copia una foto dal telefono al percorso indicato.</summary>
    void CopyTo(PhotoItem photo, string destFilePath);
}
