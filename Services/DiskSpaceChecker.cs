using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Services;

public static class DiskSpaceChecker
{
    public static bool HasEnoughSpace(string dir, IEnumerable<PhotoItem> photos)
    {
        try
        {
            ulong needed = 0;
            foreach (var p in photos) needed += p.Size;
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(dir))!);
            return (ulong)drive.AvailableFreeSpace > needed;
        }
        catch
        {
            return true; // se non riusciamo a controllare, proseguiamo (gli errori vengono gestiti per file)
        }
    }
}
