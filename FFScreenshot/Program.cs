using FFScreenshot.Configuration;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.RegisterServices(args);

var host = builder.Build();

await host.Services.InitializeServices();