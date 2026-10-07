using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Sources;

public class IosSource : IPhotoSource
{
    public DeviceInfo? FindDevice() =>
            MtpSession.FindFirst(MtpSession.IsApple, DevicePlatform.IOS, "iPhone");

    public IPhotoSession Open(DeviceInfo device, ScanOptions? options = null) => 
            MtpSession.Open(device.Id);
}