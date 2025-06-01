using System.CommandLine;

namespace FFScreenshot.Features.Common;

public static class CommandLineParser
{
    public static CommandLineOptions Parse(string[] args)
    {
        var configOption = new Option<string?>(
            name: "--config",
            description: "Path to configuration file. Uses the configuration to take a screenshot.");

        var probeOption = new Option<bool>(
            name: "--get-devices",
            description: "Run FFmpeg devices command to detect available devices and output device info as JSON in the executable directory. Takes priority over --config if both are specified.");

        var rootCommand = new RootCommand("A tool for taking screenshots using FFmpeg");
        rootCommand.AddOption(configOption);
        rootCommand.AddOption(probeOption);

        CommandLineOptions? result = null;

        rootCommand.SetHandler((string? configPath, bool getDevices) =>
        {
            if (!getDevices && string.IsNullOrEmpty(configPath))
            {
                Console.Error.WriteLine("Error: Either --get-devices flag or --config <path> must be provided, use --help for more information.");
                Environment.Exit(1);
            }

            result = new CommandLineOptions
            {
                ConfigPath = configPath,
                GetDevices = getDevices
            };
        }, configOption, probeOption);

        rootCommand.Invoke(args);
        
        return result ?? throw new InvalidOperationException("Failed to parse command line arguments");
    }
}