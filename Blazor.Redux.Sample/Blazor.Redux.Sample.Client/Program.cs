using System.Reflection;
using Blazor.Redux;
using Blazor.Redux.DevTools.Extensions;
using Blazor.Redux.Extensions;
using Blazor.Redux.Interfaces;
using Blazor.Redux.Sample.Client;
using Blazor.Redux.Sample.Client.Pages;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddSingleton<DispatchLogStore>();

var initialCounterSlice = new CounterSlice { Value = 10 };
builder.Services.AddBlazorRedux(new BlazorReduxOption()
{
    Slices = [initialCounterSlice, new EffectsSlice()],
    Assembly = Assembly.GetExecutingAssembly()
}.AddMiddleware<LoggingMiddleware>());

builder.Services.AddReduxDevTools();

await builder.Build().RunAsync();
