using System.Text.Encodings.Web;
using System.Text.Json;

namespace FFScreenshot.Features.Common;

public static class JsonUtilities
{
    public static JsonSerializerOptions PrettyJsonOptions { get; } = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}