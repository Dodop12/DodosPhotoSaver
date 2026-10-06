using DodosPhotoSaver.Models;
using DodosPhotoSaver.Sources;

namespace DodosPhotoSaver.Services;

public static class PhotoDownloader
{
    /// <summary>Copia le foto nella cartella di destinazione, sostituendo i file già esistenti.</summary>
    public static DownloadResult Download(IPhotoSession session, IReadOnlyList<PhotoItem> photos, string destDir,
                                          IProgress<int> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir);
        var namer = new SafeFileNamer(destDir);
        var errors = new List<string>();
        int copied = 0;

        for (int i = 0; i < photos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var photo = photos[i];
            string? tempPath = null;

            try
            {
                string target = namer.GetTargetPath(photo.Name);

                // Scarica in un file temporaneo, poi sostituisce: niente file a metà in caso di errore
                tempPath = target + ".tmp";
                session.CopyTo(photo, tempPath);
                File.Move(tempPath, target, overwrite: true);
                tempPath = null;
                copied++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add($"{photo.Name}: {ex.Message}");
            }
            finally
            {
                if (tempPath != null)
                {
                    try { File.Delete(tempPath); } catch { /* ignora */ }
                }
            }

            progress.Report(i + 1);
        }

        return new DownloadResult(copied, errors);
    }
}
