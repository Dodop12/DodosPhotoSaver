using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Services;

public static class DiskSpaceChecker
{
    private const ulong DefaultBufferBytes = 50UL * 1024 * 1024; // Margine di sicurezza da mantenere libero su disco

    /// <summary>
    /// Verifica se lo spazio disponibile sul disco è sufficiente per salvare le foto specificate.
    /// </summary>
    /// <param name="dir">La cartella di destinazione.</param>
    /// <param name="neededBytes">Lo spazio necessario per salvare le foto, espresso in byte.</param>
    /// <returns>True se lo spazio disponibile supera quello richiesto, incluso il margine di sicurezza.</returns>
    /// <exception cref="IOException">Lo spazio disponibile sul disco non è verificabile.</exception>
    public static bool HasEnoughSpace(string dir, ulong neededBytes)
    {
        var freeBytes = GetFreeBytes(dir) ??
            throw new IOException("Impossibile verificare lo spazio disponibile sul disco.");
        return freeBytes > neededBytes + DefaultBufferBytes;
    }

    private static ulong? GetFreeBytes(string dir)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(dir))!);
            return (ulong)drive.AvailableFreeSpace;
        }
        catch
        {
            return null;
        }
    }
}
