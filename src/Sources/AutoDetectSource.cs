using DodosPhotoSaver.Models;

namespace DodosPhotoSaver.Sources;

/// <summary>Cerca prima un iPhone, poi un telefono Android, e apre la sessione giusta.</summary>
public class AutoDetectSource : IPhotoSource
{
    private readonly IosSource _ios = new();
    private readonly AndroidSource _android = new();

    public DeviceInfo? FindDevice() => _ios.FindDevice() ?? _android.FindDevice();

    public IPhotoSession Open(DeviceInfo device, ScanOptions? options = null) => device.Platform switch
    {
        DevicePlatform.IOS => _ios.Open(device, options),
        DevicePlatform.Android => _android.Open(device, options),
        _ => throw new NotSupportedException("Tipo di telefono non supportato.")
    };
}