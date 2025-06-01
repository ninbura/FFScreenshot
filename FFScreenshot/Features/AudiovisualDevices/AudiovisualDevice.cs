using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace FFScreenshot.Features.AudiovisualDevices;

[PublicAPI]
public class AudiovisualDevice
{
    public DeviceType DeviceType { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Id { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AlternativeName { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? DevicePaths { get; set; }
}