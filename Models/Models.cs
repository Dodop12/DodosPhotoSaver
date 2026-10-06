namespace DodosPhotoSaver.Models;

public record DeviceInfo(string Id, string Name);
public record DownloadResult(int Copied, List<string> Errors);
public record PhotoItem(string DevicePath, string Name, ulong Size);