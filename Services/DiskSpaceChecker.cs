using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Services;

public static class DiskSpaceChecker
{
    private const ulong DefaultBufferBytes = 50 * 1024 * 1024; // Margine di sicurezza da mantenere libero su disco

    /// <summary>
    /// Verifica se c'è abbastanza spazio disponibile sul disco per salvare le foto specificate.
    /// </summary>
    /// <param name="dir">La cartella o percorso di destinazione.</param>
    /// <param name="photos">L'insieme delle foto da salvare.</param>
    /// <returns>True se c'è spazio sufficiente o se lo stato del disco non è verificabile; False se lo spazio è insufficiente.</returns>
    public static bool HasEnoughSpace(string dir, IEnumerable<PhotoItem> photos)
    {
        try
        {
            ulong needed = 0;
            foreach (var p in photos) needed += p.Size;
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(dir))!);
            return (ulong)drive.AvailableFreeSpace > needed + DefaultBufferBytes;
        }
        catch
        {
            return true;
        }
    }
}
