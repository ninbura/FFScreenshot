using FFScreenshot.Features.AudiovisualDevices;
using FFScreenshot.Features.Screenshot;
using Microsoft.Extensions.DependencyInjection;

namespace FFScreenshot.Features.Common;

public class AppService(CommandLineOptions options, IServiceProvider services)
{
    public async Task Invoke()
    {
        if (options.GetDevices)
        {
            await AudiovisualDeviceDetector.DetectAndSave();
            return;
        }

        var screenshotService = services.GetRequiredService<ScreenshotService>();
        
        await screenshotService.Invoke();
    }
}