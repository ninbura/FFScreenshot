using FFScreenshot.Features.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FFScreenshot.Configuration;

public static class ServiceInitialization
{
    public static async Task InitializeServices(this IServiceProvider services)
    {
        var appService = services.GetRequiredService<AppService>();
        
        await appService.Invoke();
    }
}