using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using FFScreenshot.Features.Common;

namespace FFScreenshot.Features.AudiovisualDevices;

public static partial class AudiovisualDeviceDetector
{
    [GeneratedRegex(@"\[(\d+)\]\s+(.+)")]
    private static partial Regex AvfoundationDeviceRegex();
    
    public static async Task DetectAndSave()
    {
        var devices = await GetAllDevices();
        
        var json = JsonSerializer.Serialize(devices, JsonUtilities.PrettyJsonOptions);
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "devices.json");
        
        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"Audio and video devices saved to: {outputPath}");
    }

    private static async Task<List<AudiovisualDevice>> GetAllDevices()
    {
        var devices = new List<AudiovisualDevice>();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Get video devices using v4l2
            var videoOutput = await RunCommand("v4l2-ctl", "--list-devices", useStdOut: true);
            devices.AddRange(ParseV4L2Output(videoOutput));
            
            // Get audio devices using FFmpeg sources
            var audioOutput = await RunCommand("ffmpeg", "-sources", useStdOut: false);
            devices.AddRange(ParseFfmpegLinuxAudioSources(audioOutput));
        }
        else
        {
            var format = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dshow" : "avfoundation";
            var output = await RunCommand("ffmpeg", $"-f {format} -list_devices true -i dummy", useStdOut: false);
            devices.AddRange(ParseFfmpegOutput(output, format));
        }

        if (devices.Count != 0) return devices;
        
        throw new InvalidOperationException("No audio or video devices found on this system.");
    }

    private static async Task<string> RunCommand(string fileName, string arguments, bool useStdOut)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = 
            Process.Start(startInfo) ?? 
            throw new InvalidOperationException($"Error: Failed to start {fileName}, make sure it is installed and in your PATH.");
        
        var output = useStdOut 
            ? await process.StandardOutput.ReadToEndAsync()
            : await process.StandardError.ReadToEndAsync();
            
        await process.WaitForExitAsync();
        return output;
    }

    private static List<AudiovisualDevice> ParseFfmpegOutput(string output, string format)
    {
        return format == "dshow" ? ParseDirectShow(output) : ParseAvFoundation(output);
    }

    private static List<AudiovisualDevice> ParseDirectShow(string output)
    {
        var devices = new List<AudiovisualDevice>();
        var lines = output.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
        
            // Look for device lines that start with [dshow @ and contain quotes but NOT "Alternative name"
            if (!line.StartsWith("[dshow @") || !line.Contains('"') || line.Contains("Alternative name")) 
                continue;

            // Extract device name and type
            var deviceInfo = ExtractDeviceInfo(line);
            if (deviceInfo == null) 
                continue;

            // Look for an alternative name on the next line
            string? alternativeName = null;
            if (i + 1 < lines.Length)
            {
                var nextLine = lines[i + 1].Trim();
                if (nextLine.StartsWith("[dshow @") && nextLine.Contains("Alternative name"))
                {
                    alternativeName = ExtractAlternativeName(nextLine);
                }
            }

            devices.Add(new AudiovisualDevice
            {
                DeviceType = deviceInfo.Value.deviceType,
                Name = deviceInfo.Value.name,
                AlternativeName = alternativeName
            });
        }

        return devices;
    }

    private static (DeviceType deviceType, string name)? ExtractDeviceInfo(string line)
    {
        // Extract the quoted device name
        var firstQuote = line.IndexOf('"');
        var lastQuote = line.LastIndexOf('"');
        
        if (firstQuote == -1 || lastQuote == -1 || firstQuote == lastQuote)
            return null;
        
        var deviceName = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
        
        // Determine a device type from (video) or (audio) at the end
        var deviceType = line.Contains("(video)") ? DeviceType.Video : DeviceType.Audio;
        
        return (deviceType, deviceName);
    }

    private static string? ExtractAlternativeName(string line)
    {
        // Extract alternative name from lines like:
        // [dshow @ ...] Alternative name "@device_pnp_\\?\root#media#0001#{...}\vidsource0"
        var altStart = line.IndexOf("Alternative name \"", StringComparison.Ordinal);
        if (altStart == -1) return null;
        
        altStart += "Alternative name \"".Length;
        var altEnd = line.IndexOf('"', altStart);
        
        return altEnd > altStart ? line.Substring(altStart, altEnd - altStart) : null;
    }

    private static List<AudiovisualDevice> ParseAvFoundation(string output)
    {
        var devices = new List<AudiovisualDevice>();
        var currentDeviceType = DeviceType.Video;

        foreach (var line in output.Split('\n'))
        {
            if (line.Contains("AVFoundation video devices:"))
                currentDeviceType = DeviceType.Video;
            else if (line.Contains("AVFoundation audio devices:"))
                currentDeviceType = DeviceType.Audio;
            else
            {
                var match = AvfoundationDeviceRegex().Match(line);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var deviceId))
                    devices.Add(new AudiovisualDevice 
                    { 
                        DeviceType = currentDeviceType,
                        Id = deviceId, 
                        Name = match.Groups[2].Value.Trim() 
                    });
            }
        }

        return devices;
    }

    private static List<AudiovisualDevice> ParseV4L2Output(string output)
    {
        var devices = new Dictionary<string, List<string>>();
        string? currentName = null;

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('\t') && !trimmed.StartsWith(' ') && trimmed.EndsWith(':'))
            {
                currentName = trimmed.TrimEnd(':');
                if (!devices.ContainsKey(currentName))
                    devices[currentName] = [];
            }
            else if (trimmed.StartsWith("/dev/video") && currentName != null)
            {
                devices[currentName].Add(trimmed);
            }
        }

        return devices.Select(kvp => new AudiovisualDevice 
        { 
            DeviceType = DeviceType.Video,
            Name = kvp.Key, 
            DevicePaths = kvp.Value.ToArray() 
        }).ToList();
    }

    private static List<AudiovisualDevice> ParseFfmpegLinuxAudioSources(string output)
    {
        var devices = new List<AudiovisualDevice>();
        var lines = output.Split('\n');
        
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Look for audio backend sections (alsa, pulse, etc.)
            if (!line.StartsWith("Auto-detected sources for ") ||
                (!line.Contains("alsa") && !line.Contains("pulse") && !line.Contains("pipewire"))) continue;
            var backend = ExtractBackendName(line);
                
            // Parse devices for this backend
            for (var j = i + 1; j < lines.Length; j++)
            {
                var deviceLine = lines[j].Trim();
                    
                // Stop if we hit the next backend or empty section
                if (deviceLine.StartsWith("Auto-detected sources for") || 
                    deviceLine.StartsWith("Cannot list sources"))
                    break;
                    
                // Parse device lines like "  default [Default ALSA Output...] (none)"
                if (deviceLine.Length <= 0 || deviceLine.StartsWith("Cannot list")) continue;
                
                var device = ParseAudioDeviceLine(deviceLine, backend);
                if (device != null)
                    devices.Add(device);
            }
        }
        
        return devices;
    }

    private static string ExtractBackendName(string line)
    {
        // Extract the backend name from "Auto-detected sources for alsa":
        var start = line.IndexOf("for ", StringComparison.Ordinal) + 4;
        var end = line.IndexOf(':', start);
        return end > start ? line.Substring(start, end - start) : "unknown";
    }

    private static AudiovisualDevice? ParseAudioDeviceLine(string line, string backend)
    {
        // Handle lines like:
        // "* default [Default ALSA Output (currently PipeWire Media Server)] (none)"
        // " alsa_input.pci-0000_0b_00.3.analog-stereo [Family 17h...] (none)"
        
        var trimmed = line.TrimStart('*', ' ');
        if (string.IsNullOrEmpty(trimmed))
            return null;
        
        var bracketStart = trimmed.IndexOf('[');
        var bracketEnd = trimmed.LastIndexOf(']');

        if (bracketStart <= 0 || bracketEnd <= bracketStart) return null;
        
        var deviceId = trimmed[..bracketStart].Trim();
        var description = trimmed.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
            
        // Skip monitor devices (they're outputs, not inputs)
        if (description.Contains("monitor", StringComparison.CurrentCultureIgnoreCase) && 
            !description.Contains("input", StringComparison.CurrentCultureIgnoreCase))
            return null;
            
        return new AudiovisualDevice
        {
            DeviceType = DeviceType.Audio,
            Name = description,
            AlternativeName = $"{backend}:{deviceId}"
        };

    }
}