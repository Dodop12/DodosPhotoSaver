using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Views;

/// <summary>Tutti i messaggi all'utente in un solo posto.</summary>
public static class Dialogs
{
    public static void Warning(IWin32Window owner, string text) =>
        MessageBox.Show(owner, text, "Controlla i dati", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Error(IWin32Window owner, string details) =>
        MessageBox.Show(owner,
            "Si è verificato un errore. Controlla che l'iPhone sia collegato e sbloccato.\n\n" + details,
            "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static void NoPhotos(IWin32Window owner, int year) =>
        MessageBox.Show(owner, $"Nessuna foto trovata per il {year}.", "Nessuna foto",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void NotEnoughSpace(IWin32Window owner) =>
        MessageBox.Show(owner, "Spazio insufficiente sul disco di destinazione.", "Spazio insufficiente",
            MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static void Cancelled(IWin32Window owner) =>
        MessageBox.Show(owner, "Operazione annullata.", "Annullato",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static bool ConfirmDownload(IWin32Window owner, int count, int year, string folderName, string parentDir) =>
        MessageBox.Show(owner,
            $"Trovate {count} foto del {year}.\n\n" +
            $"Nome cartella: {folderName}\nPosizione: {parentDir}\n\nVuoi procedere?",
            "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    public static bool ConfirmOverwrite(IWin32Window owner, string folderName, string parentDir) =>
        MessageBox.Show(owner,
            $"La cartella \"{folderName}\" esiste già in:\n{parentDir}\n\n" +
            "Le foto con lo stesso nome verranno SOSTITUITE.\nVuoi continuare?",
            "Cartella già esistente", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    public static void Completed(IWin32Window owner, DownloadResult result, int total, string fullPath)
    {
        string msg = $"Copiate {result.Copied} foto su {total}.\nCartella: {fullPath}";
        if (result.Errors.Count > 0)
            msg += $"\n\n{result.Errors.Count} errori, ad esempio:\n" + string.Join("\n", result.Errors.Take(5));

        MessageBox.Show(owner, msg, "Completato", MessageBoxButtons.OK,
            result.Errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }
}
