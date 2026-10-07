using DodosPhotoSaver.Models;
using DodosPhotoSaver.Sources;

namespace DodosPhotoSaver.Services;

public static class PhotoDownloader
{
    /// <summary>Copia le foto nella cartella di destinazione, sostituendo i file già esistenti.</summary>
    public static async Task<DownloadResult> DownloadAsync(IPhotoSession session, IReadOnlyList<PhotoItem> photos, string destDir,
            IProgress<int> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir); // Create the folder if it does not already exist
        var namer = new SafeFileNamer(destDir);
        var errors = new List<string>();
        int copied = 0;

        for (int i = 0; i < photos.Count; i++)
        {
            if (ct.IsCancellationRequested) break;
            var photo = photos[i];
            string? tempPath = null;

            try
            {
                string target = namer.GetTargetPath(photo.Name);

                // Usa un identificatore univoco per il file temporaneo per evitare collisioni
                tempPath = Path.Combine(destDir, $".dps_{Guid.NewGuid():N}.tmp");
                await session.CopyToAsync(photo, tempPath).ConfigureAwait(false);
                
                File.Move(tempPath, target, overwrite: true);
                tempPath = null;
                copied++;
            }
            catch (Exception ex)
            {
                errors.Add($"{photo.Name}: {ex.Message}");
            }
            finally
            {
                if (tempPath != null)
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }

            progress.Report(i + 1);
        }
        return new DownloadResult(copied, errors);
    }
}
