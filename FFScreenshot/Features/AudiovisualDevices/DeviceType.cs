using System.Text.Json.Serialization;

namespace FFScreenshot.Features.AudiovisualDevices;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceType
{
    Audio,
    Video
}