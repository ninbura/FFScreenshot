using FFScreenshot.Features.Common;
using FFScreenshot.Features.Screenshot;
using Microsoft.Extensions.DependencyInjection;

namespace FFScreenshot.Configuration;

public static class ServiceRegistration
{
    public static void RegisterServices(this IServiceCollection services, string[] args)
    {
        var options = CommandLineParser.Parse(args);
        
        services.AddSingleton(options);
        services.AddSingleton<AppService>();
        services.AddSingleton<ScreenshotService>();
    }
}